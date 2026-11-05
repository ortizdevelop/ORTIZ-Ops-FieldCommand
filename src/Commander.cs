using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>
    /// Command mode: RTS-style control in the 3D view with the purchase menu closed.
    /// LMB — select one of your purchased units, drag — box select (Shift — add), RMB — move (in formation),
    /// Shift+RMB — add a waypoint, RMB drag — facing, RMB on an enemy — attack. Order keys (K, Z, I, H, L, Y, Delete) work on this selection.
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
            /// <summary>Optional point slightly ahead of the last one: the unit rolls onto it to end up facing that way.</summary>
            public GlobalPosition? Face;
            /// <summary>Which way the unit should face at the end (for the arrow on the route).</summary>
            public Vector3? FacingDir;
        }

        private bool rmbHasPoint;
        private Vector3 rmbWorld;

        private readonly Dictionary<Unit, Route> routes = new Dictionary<Unit, Route>();

        private struct Ping
        {
            public GlobalPosition P;
            public float Until;
            public bool Waypoint;
        }

        private readonly List<Ping> pings = new List<Ping>();

        /// <summary>Selecting and ordering with the mouse in the 3D view (also with a panel open — outside it).</summary>
        private bool CommandActive => commandMode && mode == PlaceMode.None && !DynamicMap.mapMaximized && GameApi.InMission();

        private void SetCommandMode(bool on)
        {
            commandMode = on;
            boxDragging = false;
            rmbDown = false;
            if (!on)
            {
                menuOpen = false;
                areaTargeting = false;
            }
            else if (mode != PlaceMode.None)
            {
                ExitPlacement();
            }
        }

        /// <summary>Top menu sections: 0 — purchase / supply, 1 — units, 2 — stratagems.</summary>
        private static int SectionOf(int tabIndex) => tabIndex <= 3 ? 0 : tabIndex == 4 ? 1 : 2;

        private void OpenSection(int sec)
        {
            if (menuOpen && SectionOf(tab) == sec)
            {
                menuOpen = false; // second click collapses the panel
                return;
            }
            if (sec == 0 && SectionOf(tab) != 0) tab = 0;
            if (sec == 1) tab = 4;
            if (sec == 2) tab = 5;
            menuOpen = true;
            if (catalog == null || Time.time - catalogBuiltAt > 30f) RebuildCatalog();
        }

        private void ResetCommander()
        {
            commandMode = false;
            cmdSel.Clear();
            routes.Clear();
            pings.Clear();
            boxDragging = false;
            rmbDown = false;
            targetLists.Clear();
            ResetGroups();
        }

        /// <summary>Units the order keys apply to: the map selection when the map is open, otherwise the command-mode selection.</summary>
        private List<Unit> OrderSelection()
        {
            if (DynamicMap.mapMaximized)
            {
                var mapSel = SelectedFriendlyCommandables();
                if (mapSel.Count > 0) return mapSel;
            }
            return cmdSel.Where(u => u != null && !u.disabled && u is ICommandable).ToList();
        }

        private bool mapWasOpen;
        private float nextMapSelSync;

        /// <summary>
        /// One selection for the 3D view and the map: opening the map selects the command-mode units there
        /// (so the game's own map orders and ours go to them), and what you select on the map comes back.
        /// </summary>
        private void SyncMapSelection()
        {
            bool open = DynamicMap.mapMaximized;
            var map = SceneSingleton<DynamicMap>.i;
            if (map == null || !commandMode)
            {
                mapWasOpen = open;
                return;
            }
            try
            {
                if (open && !mapWasOpen && cmdSel.Count > 0)
                {
                    map.DeselectAllIcons();
                    foreach (var u in cmdSel) if (Selectable(u)) map.SelectIcon(u);
                }
                else if (open && Time.unscaledTime >= nextMapSelSync)
                {
                    nextMapSelSync = Time.unscaledTime + 0.2f;
                    var mapSel = SelectedFriendlyCommandables().Where(u => purchased.Contains(u) && Selectable(u)).ToList();
                    if (mapSel.Count > 0)
                    {
                        cmdSel.Clear();
                        cmdSel.AddRange(mapSel);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Commander] map selection: " + e.Message);
            }
            mapWasOpen = open;
        }

        private static bool Selectable(Unit u) => u != null && !u.disabled && u is ICommandable && !(u is Aircraft);

        private void TickCommander()
        {
            FreeLook_Patch.CommandMode = CommandActive && Plugin.MiddleMouseLook.Value;
            SyncMapSelection();
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

            overGui |= CommandBarRect.Contains(mouse);
            // ---- "Attack" button: the next LMB on an enemy ----
            if (attackPick)
            {
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(Plugin.StopKey.Value) || Input.GetKeyDown(KeyCode.Escape)) { attackPick = false; rmbDown = false; return; }
                if (Input.GetMouseButtonDown(0) && !overGui)
                {
                    attackPick = false;
                    Unit enemy = null;
                    if (GameApi.TryGetLocal(out _, out var hqA)) enemy = FindTargetScreen(hqA, Input.mousePosition);
                    if (enemy != null) AttackFromCommand(enemy);
                    else Toast(L.T("No enemy under the cursor", "Под курсором нет врага"), 2f);
                    return;
                }
            }
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
            // RMB: click — move (on an enemy — attack), hold and drag — move and face the arrow; Shift — add a waypoint
            if (Input.GetMouseButtonDown(1) && !overGui)
            {
                rmbDown = true;
                rmbDownPos = mouse;
                var cam0 = GetCamera();
                rmbHasPoint = cam0 != null && RaycastWorld(cam0.ScreenPointToRay(Input.mousePosition), out rmbWorld);
            }
            if (rmbDown && Input.GetMouseButtonUp(1))
            {
                rmbDown = false;
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if ((mouse - rmbDownPos).magnitude < 12f)
                {
                    Unit enemy = null;
                    if (GameApi.TryGetLocal(out _, out var hq0)) enemy = FindTargetScreen(hq0, Input.mousePosition);
                    if (enemy != null && Ctrl) QueueTarget(cmdSel.Where(Selectable).ToList(), enemy);
                    else if (Ctrl) Toast(L.T("Ctrl+RMB: point at an enemy", "Ctrl+ПКМ: наведи на врага"), 2f);
                    else if (enemy != null) AttackFromCommand(enemy);
                    else if (rmbHasPoint) MoveOrder(rmbWorld, null, shift);
                }
                else if (rmbHasPoint && TryFacing(out Vector3 face))
                {
                    MoveOrder(rmbWorld, face, shift);
                }
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

        /// <summary>Direction of the RMB drag on the ground (from the press point to the cursor).</summary>
        private bool TryFacing(out Vector3 dir)
        {
            dir = Vector3.forward;
            var cam = GetCamera();
            if (cam == null || !RaycastWorld(cam.ScreenPointToRay(Input.mousePosition), out var cur)) return false;
            dir = cur - rmbWorld;
            dir.y = 0f;
            if (dir.sqrMagnitude < 4f) return false;
            dir.Normalize();
            return true;
        }

        private void AttackFromCommand(Unit enemy)
        {
            var sel = cmdSel.Where(Selectable).ToList();
            if (sel.Count == 0) return;
            foreach (var u in sel) routes.Remove(u);
            OrderAttack(sel, enemy, true);
        }

        private void MoveOrder(Vector3 point, Vector3? facing, bool waypoint)
        {
            var sel = cmdSel.Where(Selectable).ToList();
            if (sel.Count == 0) return;
            GlobalPosition target = point.ToGlobalPosition();

            // formation: a grid around the point, facing the direction of travel
            Vector3 centroid = Vector3.zero;
            foreach (var u in sel) centroid += u.transform.position;
            centroid /= sel.Count;
            Vector3 fwd = facing ?? (point - centroid);
            fwd.y = 0f;
            fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
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
                FocusFire.Clear(u);
                if (!routes.TryGetValue(u, out var rt) || !waypoint)
                {
                    rt = new Route();
                    routes[u] = rt;
                }
                if (facing.HasValue)
                {
                    // come in from behind the point along the arrow, so the unit stops already facing that way
                    float lead = u is Ship ? 900f : 130f;
                    rt.Points.Add(dest - facing.Value * lead);
                }
                rt.Points.Add(dest);
                rt.Face = null;
                rt.FacingDir = facing;
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
                        if (rt.Face.HasValue) GameApi.MoveTo(u, rt.Face.Value); // roll forward a little to face the chosen way
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
        private Rect CommandHudRect
        {
            get
            {
                float w = Mathf.Min(Screen.width - 20f, 1160f);
                return new Rect(Screen.width * 0.5f - w * 0.5f, 8f, w, 52f);
            }
        }

        private void DrawCommander()
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (DynamicMap.mapMaximized) DrawRoutesOnMap();
                else if (commandMode || routes.Count > 0) DrawCommander3D();
            }
            if (commandMode && mode == PlaceMode.None && !DynamicMap.mapMaximized && GameApi.InMission()) DrawCommandHud();
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
                Vector2 before = prev;
                bool ok = true;
                GlobalPosition last = default;
                foreach (var p in kv.Value.Points)
                {
                    Vector3 sp = cam.WorldToScreenPoint(p.ToLocalPosition() + Vector3.up * 2f);
                    if (sp.z <= 0f) { ok = false; break; }
                    Vector2 g = new Vector2(sp.x, Screen.height - sp.y);
                    RangeOverlay.Dashed(prev, g, c, 2f);
                    RangeOverlay.Disc(g, 5f, c);
                    before = prev;
                    prev = g;
                    last = p;
                }
                // arrow at the end: which way the unit will face
                if (ok && kv.Value.Points.Count > 0)
                {
                    Vector2 dir;
                    if (kv.Value.FacingDir.HasValue)
                    {
                        Vector3 tip3 = cam.WorldToScreenPoint(last.ToLocalPosition() + kv.Value.FacingDir.Value * 40f + Vector3.up * 2f);
                        dir = tip3.z > 0f ? new Vector2(tip3.x, Screen.height - tip3.y) - prev : prev - before;
                    }
                    else dir = prev - before;
                    if (dir.sqrMagnitude > 1f)
                    {
                        Vector2 n = dir.normalized, perp = new Vector2(-n.y, n.x);
                        Vector2 tip = prev + n * 22f;
                        RangeOverlay.Line(prev, tip, c, 3f);
                        RangeOverlay.Line(tip, tip - n * 10f + perp * 7f, c, 3f);
                        RangeOverlay.Line(tip, tip - n * 10f - perp * 7f, c, 3f);
                    }
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
            // group numbers
            foreach (var kv in groups)
            {
                foreach (var u in kv.Value)
                {
                    if (u == null || u.disabled || !ScreenOf(cam, u, out var p)) continue;
                    float dist = Vector3.Distance(cam.transform.position, u.transform.position);
                    float h = Mathf.Clamp(6000f / Mathf.Max(dist, 1f), 14f, 46f);
                    RangeOverlay.Label(new Vector2(p.x - h - 2f, p.y - h - 18f), GroupName(kv.Key), cmdSel.Contains(u) ? SelColor : new Color(1f, 1f, 1f, 0.75f));
                }
            }
            // target list (Ctrl+RMB)
            if (targetLists.Count > 0) DrawTargetLists3D(cam);
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
            // RMB held and dragged: the direction the units will face
            if (rmbDown && rmbHasPoint)
            {
                Vector2 mouseNow = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                Vector3 a3 = cam.WorldToScreenPoint(rmbWorld + Vector3.up * 2f);
                if (a3.z > 0f && (mouseNow - rmbDownPos).magnitude >= 12f)
                {
                    Vector2 a = new Vector2(a3.x, Screen.height - a3.y);
                    Vector2 dir = (mouseNow - a);
                    if (dir.sqrMagnitude > 1f)
                    {
                        Vector2 n = dir.normalized, perp = new Vector2(-n.y, n.x);
                        Color ac = Theme.AccentColor;
                        RangeOverlay.Disc(a, 6f, ac);
                        RangeOverlay.Line(a, mouseNow, ac, 3f);
                        RangeOverlay.Line(mouseNow, mouseNow - n * 16f + perp * 10f, ac, 3f);
                        RangeOverlay.Line(mouseNow, mouseNow - n * 16f - perp * 10f, ac, 3f);
                    }
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
            if (targetLists.Count > 0) DrawTargetListsOnMap(map);
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
            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 8f, r.width - 20f, r.height - 10f));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"ORTIZ <color=#{Theme.AccentHex}>OPS</color>", Theme.Logo, GUILayout.Width(120));
            int sec = menuOpen ? SectionOf(tab) : -1;
            if (GUILayout.Button(L.T("Purchase / supply", "Покупка / снабжение"), sec == 0 ? Theme.SegOn : Theme.Seg, GUILayout.Width(190))) OpenSection(0);
            if (GUILayout.Button(L.T($"Units ({purchased.Count})", $"Юниты ({purchased.Count})"), sec == 1 ? Theme.SegOn : Theme.Seg, GUILayout.Width(130))) OpenSection(1);
            if (GUILayout.Button(L.T("Stratagems", "Стратегемы"), sec == 2 ? Theme.SegOn : Theme.Seg, GUILayout.Width(130))) OpenSection(2);
            GUILayout.Space(12);
            GUILayout.Label(L.T($"Selected: {cmdSel.Count}", $"Выделено: {cmdSel.Count}") + "   " + GameApi.Money(GameApi.GetFunds()), Theme.RowLabel);
            DrawGroupChips();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(L.T("Exit ", "Выход ") + Plugin.MenuKey.Value, GUILayout.Width(110))) SetCommandMode(false);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            DrawCommandBar();
        }

        private Rect CommandBarRect
        {
            get
            {
                float w = Mathf.Min(Screen.width - 20f, 1180f);
                return new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height - 104f, w, 94f);
            }
        }

        private bool attackPick;

        /// <summary>Bottom bar: order buttons (same as the hot keys) and the mouse / camera help.</summary>
        private void DrawCommandBar()
        {
            var r = CommandBarRect;
            GUI.Box(r, "", Theme.Toast);
            GUI.DrawTexture(new Rect(r.x, r.y, 3f, r.height), Theme.Tex(Theme.AccentColor));
            GUILayout.BeginArea(new Rect(r.x + 12f, r.y + 8f, r.width - 20f, r.height - 10f));
            var sel = cmdSel.Where(Selectable).ToList();
            bool any = sel.Count > 0;
            GUILayout.BeginHorizontal();
            GUI.enabled = any;
            if (GUILayout.Button((attackPick ? "► " : "") + L.T("Attack", "Атака") + $"  [{Plugin.AttackKey.Value}]", attackPick ? Theme.Primary : GUI.skin.button, GUILayout.Width(118)))
            {
                attackPick = !attackPick;
                if (attackPick) Toast(L.T("Click an enemy (LMB). RMB / X — cancel", "Кликни по врагу (ЛКМ). ПКМ / X — отмена"), 4f);
            }
            if (GUILayout.Button(L.T("Strike area", "Площадь") + $"  [{Plugin.AreaFireKey.Value}]", GUILayout.Width(118))) BeginAreaTargeting(sel, false);
            if (GUILayout.Button(L.T("Stop!", "Стоп!") + $"  [{Plugin.StopKey.Value}]", GUILayout.Width(104))) CmdStop(sel);
            if (GUILayout.Button(L.T("Cease fire", "Прекратить огонь") + $"  [{Plugin.CeaseFireKey.Value}]", GUILayout.Width(176))) CmdCeaseFire(sel);
            bool hf = any && sel.All(u => AreaFire.HoldFire.Contains(u));
            if (GUILayout.Button((hf ? L.T("Fire: on order", "Огонь: по приказу") : L.T("Fire: at will", "Огонь: свободный")) + $"  [{Plugin.HoldFireKey.Value}]", hf ? Theme.Primary : GUI.skin.button, GUILayout.Width(190)))
            {
                foreach (var u in sel) AreaFire.SetHold(u, !hf);
            }
            bool hold = any && sel.All(GameApi.GetHold);
            if (GUILayout.Button(L.T("Hold", "Держать") + $"  [{Plugin.HoldKey.Value}]", hold ? Theme.Primary : GUI.skin.button, GUILayout.Width(112)))
            {
                foreach (var u in sel)
                {
                    attackOrders.Remove(u);
                    GameApi.SetHold(u, !hold);
                    if (!hold) { routes.Remove(u); GameApi.Stop(u); }
                }
            }
            bool abm = any && sel.All(u => FocusFire.MissileDefense.Contains(u));
            if (GUILayout.Button(L.T("ABM", "ПРО") + $"  [{Plugin.MissileDefenseKey.Value}]", abm ? Theme.Primary : GUI.skin.button, GUILayout.Width(84)))
            {
                foreach (var u in sel) { if (abm) FocusFire.MissileDefense.Remove(u); else FocusFire.MissileDefense.Add(u); }
            }
            if (GUILayout.Button(L.T("Rearm", "Пополнить") + $"  [{Plugin.PaidRearmKey.Value}]", GUILayout.Width(126))) RearmUnits(sel);
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
            GUILayout.Label(L.T("LMB / drag — select · RMB — move (hold and drag — facing) · Shift+RMB — waypoint · Alt — use roads · RMB on an enemy — attack · Ctrl+RMB on enemies — one salvo at each in turn · Ctrl+1…0 — make a group, 1…0 — select (twice — camera to it) · Ctrl+C — buy one more of the selected · mouse wheel held — turn the camera · WASD — move the camera",
                                "ЛКМ / рамка — выделить · ПКМ — двигаться (зажать и потянуть — разворот) · Shift+ПКМ — маршрут · Alt — по дорогам · ПКМ по врагу — атака · Ctrl+ПКМ по врагам — по залпу по каждому по очереди · Ctrl+1…0 — группа, 1…0 — выделить (дважды — камера к ней) · Ctrl+C — купить ещё такой же · зажатое колесо — поворот камеры · WASD — движение камеры"), Theme.Small);
            GUILayout.EndArea();
        }

        private void CmdStop(List<Unit> sel)
        {
            foreach (var u in sel)
            {
                routes.Remove(u);
                attackOrders.Remove(u);
                GameApi.Stop(u);
            }
            Toast(L.T($"Stop: {sel.Count}", $"Стоп: {sel.Count}"), 2f);
        }

        private void CmdCeaseFire(List<Unit> sel)
        {
            foreach (var u in sel)
            {
                FocusFire.Clear(u);
                FocusFire.MissileDefense.Remove(u);
                AreaFire.Cancel(u);
                attackOrders.Remove(u);
                AreaFire.SetHold(u, true);
            }
            Toast(L.T($"Cease fire: {sel.Count} — fire only on order ({Plugin.HoldFireKey.Value} — fire at will again)", $"Прекратить огонь: {sel.Count} — огонь только по приказу ({Plugin.HoldFireKey.Value} — снова свободный огонь)"), 4f);
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
