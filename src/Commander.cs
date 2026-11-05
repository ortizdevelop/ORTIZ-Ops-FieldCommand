using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Command mode: RTS-style control in the 3D view with the purchase menu closed.
    /// LMB — select one of your purchased units, drag — box select (Shift — add), RMB — move (in formation),
    /// Ctrl+RMB — add a waypoint. Order keys (K, Z, I, H, L, Y, Delete) work on this selection.
    /// </summary>
    internal partial class FieldCommandController
    {
        private bool commandMode;
        private readonly List<Unit> cmdSel = new List<Unit>();
        private bool boxDragging;
        private Vector2 boxStart;
        private Vector2 rmbDownPos;
        private bool rmbDown;

        private sealed class Route
        {
            public readonly List<GlobalPosition> Points = new List<GlobalPosition>();
            public float IssuedAt;
        }

        private readonly Dictionary<Unit, Route> routes = new Dictionary<Unit, Route>();

        private struct Ping
        {
            public GlobalPosition P;
            public float Until;
            public bool Waypoint;
        }

        private readonly List<Ping> pings = new List<Ping>();

        private bool CommandActive => commandMode && !menuOpen && mode == PlaceMode.None && !DynamicMap.mapMaximized && GameApi.InMission();

        private void SetCommandMode(bool on)
        {
            commandMode = on;
            boxDragging = false;
            rmbDown = false;
            if (on)
            {
                if (mode != PlaceMode.None) ExitPlacement();
                Toast(L.T($"Command mode: LMB / drag — select your units, RMB — move, Ctrl+RMB — waypoint. {Plugin.MenuKey.Value} — menu, {Plugin.CommandKey.Value} — exit",
                          $"Режим командира: ЛКМ / рамка — выделить свои юниты, ПКМ — двигаться, Ctrl+ПКМ — точка маршрута. {Plugin.MenuKey.Value} — меню, {Plugin.CommandKey.Value} — выход"), 6f);
            }
            else
            {
                Toast(L.T("Command mode off", "Режим командира выключен"), 2f);
            }
        }

        private void ResetCommander()
        {
            commandMode = false;
            cmdSel.Clear();
            routes.Clear();
            pings.Clear();
            boxDragging = false;
            rmbDown = false;
        }

        /// <summary>Units the order keys apply to: the map selection when the map is open, otherwise the command-mode selection.</summary>
        private List<Unit> OrderSelection()
        {
            if (DynamicMap.mapMaximized) return SelectedFriendlyCommandables();
            return cmdSel.Where(u => u != null && !u.disabled && u is ICommandable).ToList();
        }

        private static bool Selectable(Unit u) => u != null && !u.disabled && u is ICommandable && !(u is Aircraft);

        private void TickCommander()
        {
            if (Plugin.CommandKey.Value.IsDown())
            {
                if (commandMode && !menuOpen) SetCommandMode(false);
                else
                {
                    menuOpen = false;
                    SetCommandMode(true);
                }
            }
            cmdSel.RemoveAll(u => !Selectable(u) || !purchased.Contains(u));
            TickRoutes();
            pings.RemoveAll(p => Time.unscaledTime > p.Until);

            if (!CommandActive || areaTargeting)
            {
                boxDragging = false;
                rmbDown = false;
                return;
            }

            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            bool overGui = IsOverOwnGui(Input.mousePosition) || CommandHudRect.Contains(mouse) || GUIUtility.hotControl != 0;

            // ---- selection ----
            if (Input.GetMouseButtonDown(0) && !overGui)
            {
                boxDragging = true;
                boxStart = mouse;
            }
            if (boxDragging && Input.GetMouseButtonUp(0))
            {
                boxDragging = false;
                bool add = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                Rect r = RectFrom(boxStart, mouse);
                var cam = GetCamera();
                if (cam != null)
                {
                    if (r.width < 6f && r.height < 6f) ClickSelect(cam, mouse, add);
                    else BoxSelect(cam, r, add);
                }
            }

            // ---- move orders (on release, so dragging with RMB doesn't send orders) ----
            if (Input.GetMouseButtonDown(1) && !overGui)
            {
                rmbDown = true;
                rmbDownPos = mouse;
            }
            if (rmbDown && Input.GetMouseButtonUp(1))
            {
                rmbDown = false;
                if ((mouse - rmbDownPos).magnitude < 8f) MoveOrder(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
            }
        }

        private static Rect RectFrom(Vector2 a, Vector2 b)
        {
            return new Rect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private static bool ScreenOf(Camera cam, Unit u, out Vector2 gui)
        {
            gui = default;
            Vector3 sp = cam.WorldToScreenPoint(u.transform.position);
            if (sp.z <= 0f) return false;
            gui = new Vector2(sp.x, Screen.height - sp.y);
            return true;
        }

        private void ClickSelect(Camera cam, Vector2 mouse, bool add)
        {
            Unit best = null;
            float bestD = 36f;
            foreach (var u in purchased)
            {
                if (!Selectable(u) || !ScreenOf(cam, u, out var p)) continue;
                float d = (p - mouse).magnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = u;
                }
            }
            if (!add) cmdSel.Clear();
            if (best != null)
            {
                if (add && cmdSel.Contains(best)) cmdSel.Remove(best);
                else if (!cmdSel.Contains(best)) cmdSel.Add(best);
            }
        }

        private void BoxSelect(Camera cam, Rect r, bool add)
        {
            if (!add) cmdSel.Clear();
            foreach (var u in purchased)
            {
                if (!Selectable(u) || cmdSel.Contains(u) || !ScreenOf(cam, u, out var p)) continue;
                if (r.Contains(p)) cmdSel.Add(u);
            }
            if (cmdSel.Count > 0) Toast(L.T($"Selected: {cmdSel.Count}", $"Выделено: {cmdSel.Count}"), 1.5f);
        }

        // ---------- move orders and waypoints ----------

        private void MoveOrder(bool waypoint)
        {
            var sel = cmdSel.Where(Selectable).ToList();
            if (sel.Count == 0) return;
            var cam = GetCamera();
            if (cam == null || !RaycastWorld(cam.ScreenPointToRay(Input.mousePosition), out Vector3 point)) return;
            GlobalPosition target = point.ToGlobalPosition();

            // formation: a grid around the point, facing the direction of travel
            Vector3 centroid = Vector3.zero;
            foreach (var u in sel) centroid += u.transform.position;
            centroid /= sel.Count;
            Vector3 fwd = point - centroid;
            fwd.y = 0f;
            fwd = fwd.sqrMagnitude > 1f ? fwd.normalized : Vector3.forward;
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);
            int cols = Mathf.CeilToInt(Mathf.Sqrt(sel.Count));
            int rows = Mathf.CeilToInt(sel.Count / (float)cols);
            bool naval = sel.Any(u => u is Ship);
            float spacing = naval ? 250f : 35f;
            // keep the order stable: units closest to the front go to the front row
            var ordered = sel.OrderByDescending(u => Vector3.Dot(u.transform.position - centroid, fwd)).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                var u = ordered[i];
                int row = i / cols, col = i % cols;
                Vector3 off = right * ((col - (cols - 1) * 0.5f) * spacing) - fwd * ((row - (rows - 1) * 0.5f) * spacing);
                GlobalPosition dest = target + off;
                attackOrders.Remove(u);
                if (!routes.TryGetValue(u, out var rt) || !waypoint)
                {
                    rt = new Route();
                    routes[u] = rt;
                }
                rt.Points.Add(dest);
                if (rt.Points.Count == 1)
                {
                    GameApi.MoveTo(u, dest);
                    rt.IssuedAt = Time.unscaledTime;
                }
            }
            pings.Add(new Ping { P = target, Until = Time.unscaledTime + 1.5f, Waypoint = waypoint });
        }

        private static float ArriveRadius(Unit u) => u is Ship ? 300f : 40f;

        private static float Flat(GlobalPosition a, GlobalPosition b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private float nextRouteTick;

        private void TickRoutes()
        {
            if (routes.Count == 0 || Time.unscaledTime < nextRouteTick) return;
            nextRouteTick = Time.unscaledTime + 0.5f;
            foreach (var kv in routes.ToList())
            {
                var u = kv.Key;
                var rt = kv.Value;
                if (!Selectable(u) || rt.Points.Count == 0)
                {
                    routes.Remove(u);
                    continue;
                }
                var cur = rt.Points[0];
                // another order (map, AI, stop) replaced ours → drop the route
                try
                {
                    if (u is ICommandable c && c.UnitCommand != null && Time.unscaledTime - rt.IssuedAt > 1.5f)
                    {
                        var cmd = c.UnitCommand.GetCommandCached();
                        if (Flat(cmd.position, cur) > 5f)
                        {
                            routes.Remove(u);
                            continue;
                        }
                    }
                }
                catch
                {
                }
                if (Flat(u.GlobalPosition(), cur) < ArriveRadius(u))
                {
                    rt.Points.RemoveAt(0);
                    if (rt.Points.Count == 0)
                    {
                        routes.Remove(u);
                        continue;
                    }
                    GameApi.MoveTo(u, rt.Points[0]);
                    rt.IssuedAt = Time.unscaledTime;
                }
            }
        }

        private void ClearRoute(Unit u) => routes.Remove(u);

        // ---------- enemy under the mouse in the 3D view ----------

        private static Unit FindTargetScreen(FactionHQ hq, Vector3 mouseScreen)
        {
            var cam = GetCamera();
            if (cam == null) return null;
            Vector2 m = new Vector2(mouseScreen.x, mouseScreen.y);
            Unit best = null;
            float bestD = 40f;
            foreach (var u in UnitRegistry.allUnits)
            {
                if (u == null || u.disabled || u.NetworkHQ == null || u.NetworkHQ == hq) continue;
                if (!(u is GroundVehicle || u is Ship || u is Building || u is Missile || u is Aircraft)) continue;
                bool tracked = true;
                try { tracked = hq.IsTargetBeingTracked(u); } catch { }
                if (!tracked) continue;
                Vector3 sp = cam.WorldToScreenPoint(u.transform.position);
                if (sp.z <= 0f) continue;
                float d = (new Vector2(sp.x, sp.y) - m).magnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = u;
                }
            }
            return best;
        }

        // ---------- drawing ----------

        private static readonly Color SelColor = new Color(0.45f, 1f, 0.45f, 1f);
        private Rect CommandHudRect => new Rect(Screen.width * 0.5f - 330f, 10f, 660f, 56f);

        private void DrawCommander()
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (DynamicMap.mapMaximized) DrawRoutesOnMap();
                else if (commandMode || routes.Count > 0) DrawCommander3D();
            }
            if (CommandActive) DrawCommandHud();
        }

        private void DrawCommander3D()
        {
            var cam = GetCamera();
            if (cam == null) return;
            // routes of selected units (all routes while in command mode)
            foreach (var kv in routes)
            {
                var u = kv.Key;
                if (u == null || (!commandMode && !cmdSel.Contains(u))) continue;
                if (!ScreenOf(cam, u, out var prev)) continue;
                bool sel = cmdSel.Contains(u);
                Color c = sel ? SelColor : new Color(SelColor.r, SelColor.g, SelColor.b, 0.45f);
                foreach (var p in kv.Value.Points)
                {
                    Vector3 sp = cam.WorldToScreenPoint(p.ToLocalPosition() + Vector3.up * 2f);
                    if (sp.z <= 0f) break;
                    Vector2 g = new Vector2(sp.x, Screen.height - sp.y);
                    RangeOverlay.Dashed(prev, g, c, 2f);
                    RangeOverlay.Disc(g, 5f, c);
                    prev = g;
                }
            }
            if (!commandMode) return;
            // selection brackets
            foreach (var u in cmdSel)
            {
                if (u == null || !ScreenOf(cam, u, out var p)) continue;
                float dist = Vector3.Distance(cam.transform.position, u.transform.position);
                float h = Mathf.Clamp(6000f / Mathf.Max(dist, 1f), 14f, 46f);
                Brackets(new Rect(p.x - h, p.y - h, h * 2f, h * 2f), SelColor);
            }
            // order pings
            foreach (var pg in pings)
            {
                Vector3 sp = cam.WorldToScreenPoint(pg.P.ToLocalPosition() + Vector3.up * 2f);
                if (sp.z <= 0f) continue;
                float k = Mathf.Clamp01((pg.Until - Time.unscaledTime) / 1.5f);
                Vector2 g = new Vector2(sp.x, Screen.height - sp.y);
                Color c = pg.Waypoint ? new Color(0.4f, 0.85f, 1f, k) : new Color(SelColor.r, SelColor.g, SelColor.b, k);
                RangeOverlay.Circle(g, 10f + 20f * (1f - k), c, 2f, false);
            }
            // weapon reach of a small selection
            if (cmdSel.Count > 0 && cmdSel.Count <= 6 && RangeOverlay.Enabled.Value)
            {
                foreach (var u in cmdSel)
                {
                    var r = RangeOverlay.Of(u);
                    if (r.Surface > 0f) AreaOverlay.Ring(cam, u.GlobalPosition(), r.Surface, new Color(OwnedMarks.Normal.r, OwnedMarks.Normal.g, OwnedMarks.Normal.b, 0.55f), true);
                }
            }
            // selection box
            if (boxDragging)
            {
                Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                Rect r = RectFrom(boxStart, mouse);
                Color old = GUI.color;
                GUI.color = new Color(SelColor.r, SelColor.g, SelColor.b, 0.12f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = old;
                Frame(r, SelColor, 1f);
            }
        }

        private void DrawRoutesOnMap()
        {
            var map = SceneSingleton<DynamicMap>.i;
            if (map == null) return;
            foreach (var kv in routes)
            {
                if (kv.Key == null || !RangeOverlay.ToGui(map, kv.Key.GlobalPosition(), out var prev, out _)) continue;
                foreach (var p in kv.Value.Points)
                {
                    if (!RangeOverlay.ToGui(map, p, out var g, out _)) break;
                    RangeOverlay.Dashed(prev, g, SelColor, 1.5f);
                    RangeOverlay.Disc(g, 4f, SelColor);
                    prev = g;
                }
            }
        }

        private void DrawCommandHud()
        {
            var r = CommandHudRect;
            GUI.Box(r, "", Theme.Toast);
            GUI.DrawTexture(new Rect(r.x, r.y, 3f, r.height), Theme.Tex(Theme.AccentColor));
            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 6f, r.width - 20f, r.height - 10f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(L.T($"COMMAND MODE — selected: {cmdSel.Count}", $"РЕЖИМ КОМАНДИРА — выделено: {cmdSel.Count}"), Theme.Header);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(L.T("Menu", "Меню"), GUILayout.Width(80))) menuOpen = true;
            if (GUILayout.Button(L.T("Exit", "Выход"), GUILayout.Width(80))) SetCommandMode(false);
            GUILayout.EndHorizontal();
            GUILayout.Label(L.T($"LMB / drag — select · RMB — move · Ctrl+RMB — waypoint · {Plugin.AttackKey.Value} attack · {Plugin.AreaFireKey.Value} area · {Plugin.HoldFireKey.Value} fire on order · {Plugin.HoldKey.Value} hold",
                                $"ЛКМ / рамка — выделить · ПКМ — двигаться · Ctrl+ПКМ — маршрут · {Plugin.AttackKey.Value} атака · {Plugin.AreaFireKey.Value} площадь · {Plugin.HoldFireKey.Value} огонь по приказу · {Plugin.HoldKey.Value} держать"), Theme.Small);
            GUILayout.EndArea();
        }

        private static void Brackets(Rect r, Color c)
        {
            float l = Mathf.Max(r.width * 0.28f, 5f);
            RangeOverlay.Line(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin + l, r.yMin), c, 2f);
            RangeOverlay.Line(new Vector2(r.xMin, r.yMin), new Vector2(r.xMin, r.yMin + l), c, 2f);
            RangeOverlay.Line(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax - l, r.yMin), c, 2f);
            RangeOverlay.Line(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMin + l), c, 2f);
            RangeOverlay.Line(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin + l, r.yMax), c, 2f);
            RangeOverlay.Line(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMax - l), c, 2f);
            RangeOverlay.Line(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax - l, r.yMax), c, 2f);
            RangeOverlay.Line(new Vector2(r.xMax, r.yMax), new Vector2(r.xMax, r.yMax - l), c, 2f);
        }

        private static void Frame(Rect r, Color c, float w)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, w), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - w, r.width, w), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, w, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - w, r.y, w, r.height), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
