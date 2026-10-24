using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Mirage;
using UnityEngine;

namespace FieldCommand
{
    internal enum StratagemKind
    {
        Artillery,
        Carpet,
        MissileStrike,
        TacticalNuke,
        StrategicNuke,
        Airdrop
    }

    /// <summary>One call-in support ability, World in Conflict style.</summary>
    internal class Stratagem
    {
        public StratagemKind Kind;
        public string Name;
        public string Description;
        public ConfigEntry<float> Price;
        public ConfigEntry<float> Cooldown;
        public ConfigEntry<string> Munition; // jsonKey / name override, empty = automatic
        public float ReadyAt;

        // area of effect
        public bool Point;          // true = pin-point, false = area (circle)
        public float Radius = 150f; // area radius, m

        public bool HasArea => Kind == StratagemKind.Artillery || Kind == StratagemKind.Carpet || Kind == StratagemKind.MissileStrike;

        /// <summary>Effective spread radius in metres.</summary>
        public float Spread => Point ? 12f : Mathf.Clamp(Radius, Stratagems.MinRadius, Stratagems.MaxRadius);

        // airdrop only
        public CatalogEntry DropUnit;
        public int DropCount = 2;

        public float CurrentPrice()
        {
            if (Kind == StratagemKind.Airdrop)
            {
                return DropUnit == null ? 0f : DropUnit.Price * DropCount * Mathf.Max(Stratagems.AirdropMarkup.Value, 1f);
            }
            return Price.Value;
        }

        public float CooldownLeft => Mathf.Max(0f, ReadyAt - Time.time);
    }

    internal static class Stratagems
    {
        public static readonly List<Stratagem> All = new List<Stratagem>();
        public static ConfigEntry<float> Delay;
        public static ConfigEntry<float> AirdropMarkup;
        public static ConfigEntry<float> AirdropHeight;
        public static ConfigEntry<float> SpawnHeight;
        public static ConfigEntry<float> MinAltitude;
        public static ConfigEntry<float> DropSpeed;
        public static ConfigEntry<float> RewardPercent;
        public static ConfigEntry<int> ArtilleryShells;
        public const float MinRadius = 30f;
        public const float MaxRadius = 1200f;

        private static int counter;

        public static void Bind(ConfigFile cfg)
        {
            const string sec = "7. Стратегемы";
            Delay = cfg.Bind(sec, "Задержка удара, сек", 5f, "Сколько секунд проходит от вызова до удара (время подлёта)");
            AirdropMarkup = cfg.Bind(sec, "Десант: наценка", 1.3f, "Цена десанта = цена юнитов × количество × наценка");
            AirdropHeight = cfg.Bind(sec, "Десант: высота сброса, м", 150f, "С какой высоты техника плавно опускается на землю (0 — появляется сразу на земле)");
            SpawnHeight = cfg.Bind(sec, "Высота появления снарядов над землёй, м", 800f, new ConfigDescription("На какой высоте над точкой удара появляются снаряды и бомбы. Меньше — точнее и быстрее долетают", new AcceptableValueRange<float>(100f, 8000f)));
            MinAltitude = cfg.Bind(sec, "Высота появления не ниже, м над морем", 3500f, new ConfigDescription("Снаряды никогда не появляются ниже этой высоты над уровнем моря — защита от появления внутри гор, если рельеф в точке удара не прогружен. Поставь выше самых высоких гор карты", new AcceptableValueRange<float>(0f, 10000f)));
            DropSpeed = cfg.Bind(sec, "Скорость падения снарядов, м/с", 300f, "С какой начальной скоростью снаряды летят вниз");
            RewardPercent = cfg.Bind(sec, "Награда за уничтожения стратегемой, %", 15f, new ConfigDescription("Какой % от награды пилота-игрока ты получаешь за врагов, уничтоженных твоими стратегемами (0 — выключить)", new AcceptableValueRange<float>(0f, 100f)));
            ArtilleryShells = cfg.Bind(sec, "Артудар: снарядов", 12, "Сколько снарядов в одном артударе");

            All.Clear();
            All.Add(Make(cfg, sec, StratagemKind.Artillery, "Артудар", "Серия снарядов: точечно или по выбранному кругу", 15f, 60f));
            All.Add(Make(cfg, sec, StratagemKind.Carpet, "Ковровая бомбардировка", "14 бомб полосой: точечно (~200 м) или длиной в диаметр круга, по направлению стрелки (R — повернуть)", 35f, 120f));
            All.Add(Make(cfg, sec, StratagemKind.MissileStrike, "Ракетный удар", "4 ракеты: точечно — все в ближайшего видимого врага; по области — по видимым врагам в круге", 40f, 90f));
            All.Add(Make(cfg, sec, StratagemKind.TacticalNuke, "Тактический ядерный удар", "1 тактический ядерный заряд", 250f, 300f));
            All.Add(Make(cfg, sec, StratagemKind.StrategicNuke, "Стратегический ядерный удар", "1 стратегический ядерный заряд", 800f, 900f));
            All.Add(Make(cfg, sec, StratagemKind.Airdrop, "Десант", "Сброс выбранной наземной техники с неба — куда угодно на сушу, даже в тыл врага", 0f, 90f));
        }

        private static Stratagem Make(ConfigFile cfg, string sec, StratagemKind k, string name, string desc, float price, float cooldown)
        {
            var s = new Stratagem { Kind = k, Name = name, Description = desc };
            s.Price = cfg.Bind(sec, name + ": цена, млн", price, "Цена вызова");
            s.Cooldown = cfg.Bind(sec, name + ": перезарядка, сек", cooldown, "Через сколько секунд можно вызвать снова");
            if (k != StratagemKind.Airdrop)
            {
                s.Munition = cfg.Bind(sec, name + ": боеприпас", "", "Ключ или название боеприпаса из игры (пусто — выбрать автоматически). Ключи пишутся в лог при входе в миссию");
            }
            return s;
        }

        public static void ResetCooldowns()
        {
            foreach (var s in All) s.ReadyAt = 0f;
        }

        // ---------------- munitions ----------------

        private static WeaponInfo Info(MissileDefinition d)
        {
            try
            {
                return d?.unitPrefab?.GetComponent<Missile>()?.GetWeaponInfo();
            }
            catch
            {
                return null;
            }
        }

        private static bool HasProxyFuse(MissileDefinition d)
        {
            try
            {
                var m = d.unitPrefab.GetComponent<Missile>();
                return m != null && HarmonyLib.Traverse.Create(m).Field("proxyFuse").GetValue() != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>All munitions that make sense for this stratagem (for the picker in the menu).</summary>
        public static List<MissileDefinition> Candidates(Stratagem s)
        {
            var c = AllMunitions().Where(x => !Info(x).troops && !Info(x).cargo && !Info(x).jammer && Info(x).blastDamage > 0f);
            switch (s.Kind)
            {
                case StratagemKind.TacticalNuke:
                case StratagemKind.StrategicNuke:
                    c = c.Where(x => Info(x).nuclear);
                    break;
                default:
                    c = c.Where(x => !Info(x).nuclear);
                    break;
            }
            return c.OrderBy(x => Info(x).blastDamage).ToList();
        }

        public static List<MissileDefinition> AllMunitions()
        {
            var set = new HashSet<MissileDefinition>();
            try
            {
                foreach (var m in Encyclopedia.i.missiles) if (m != null && m.unitPrefab != null) set.Add(m);
            }
            catch
            {
            }
            foreach (var u in Encyclopedia.Lookup.Values)
            {
                if (u is MissileDefinition m && m.unitPrefab != null) set.Add(m);
            }
            return set.Where(m => Info(m) != null).ToList();
        }

        private static bool loggedMunitions;

        public static void LogMunitions()
        {
            if (loggedMunitions) return;
            loggedMunitions = true;
            var sb = new System.Text.StringBuilder("[Stratagems] munitions (key | name | bomb/missile/nuclear/strategic | blast):\n");
            foreach (var m in AllMunitions())
            {
                var i = Info(m);
                sb.Append("  ").Append(FactionResolver.KeyOf(m)).Append(" | ").Append(m.unitName).Append(" | ")
                  .Append(i.bomb ? "bomb " : "").Append(i.missile ? "missile " : "").Append(i.nuclear ? "nuclear " : "").Append(i.strategic ? "strategic " : "")
                  .Append("| ").Append(i.blastDamage.ToString("0")).Append('\n');
            }
            Plugin.Log.LogInfo(sb.ToString());
        }

        public static MissileDefinition MunitionFor(Stratagem s)
        {
            var all = AllMunitions();
            string want = s.Munition?.Value;
            if (!string.IsNullOrWhiteSpace(want))
            {
                var m = all.FirstOrDefault(x => string.Equals(FactionResolver.KeyOf(x), want, StringComparison.OrdinalIgnoreCase)
                                                || string.Equals(x.unitName, want, StringComparison.OrdinalIgnoreCase)
                                                || string.Equals(x.name, want, StringComparison.OrdinalIgnoreCase));
                if (m != null) return m;
            }
            IEnumerable<MissileDefinition> c = all.Where(x => !Info(x).troops && !Info(x).cargo && !Info(x).jammer && Info(x).blastDamage > 0f);
            switch (s.Kind)
            {
                case StratagemKind.Artillery:
                    return c.Where(x => !Info(x).nuclear && Info(x).bomb).OrderBy(x => Info(x).blastDamage).FirstOrDefault()
                           ?? c.Where(x => !Info(x).nuclear).OrderBy(x => Info(x).blastDamage).FirstOrDefault();
                case StratagemKind.Carpet:
                    return c.Where(x => !Info(x).nuclear && Info(x).bomb && !Info(x).glideBomb && !Info(x).laserGuided).OrderByDescending(x => Info(x).blastDamage).FirstOrDefault()
                           ?? c.Where(x => !Info(x).nuclear && Info(x).bomb).OrderByDescending(x => Info(x).blastDamage).FirstOrDefault();
                case StratagemKind.MissileStrike:
                    return c.Where(x => !Info(x).nuclear && Info(x).missile && Info(x).effectiveness.antiSurface >= 0.3f && Info(x).effectiveness.antiAir < 0.3f && !HasProxyFuse(x)).OrderByDescending(x => Info(x).blastDamage).FirstOrDefault()
                           ?? c.Where(x => !Info(x).nuclear && Info(x).missile && Info(x).effectiveness.antiSurface >= 0.3f).OrderByDescending(x => Info(x).blastDamage).FirstOrDefault()
                           ?? c.Where(x => !Info(x).nuclear && Info(x).missile).OrderByDescending(x => Info(x).blastDamage).FirstOrDefault();
                case StratagemKind.TacticalNuke:
                    return c.Where(x => Info(x).nuclear && !Info(x).strategic).OrderBy(x => Info(x).bomb ? 0 : 1).ThenBy(x => Info(x).blastDamage).FirstOrDefault();
                case StratagemKind.StrategicNuke:
                    return c.Where(x => Info(x).strategic).OrderByDescending(x => Info(x).blastDamage).FirstOrDefault()
                           ?? c.Where(x => Info(x).nuclear).OrderByDescending(x => Info(x).blastDamage).FirstOrDefault();
            }
            return null;
        }

        // ---------------- validation ----------------

        public static GameApi.Placement Validate(Stratagem s, GlobalPosition cursor, float heading)
        {
            var p = new GameApi.Placement { Rotation = Quaternion.Euler(0f, heading, 0f) };
            if (!GameApi.IsServer()) { p.Reason = "Только одиночная игра / хост"; return p; }
            if (s.CooldownLeft > 0f) { p.Reason = $"Перезарядка {s.CooldownLeft:0} с"; return p; }
            if (GameApi.GetFunds() < s.CurrentPrice()) { p.Reason = "Недостаточно денег"; return p; }
            Vector3 local = cursor.ToLocalPosition();
            bool known = TryGroundY(local, out float gy);
            bool land = known && gy >= Datum.LocalSeaY + 0.3f;
            local.y = known ? gy : Datum.LocalSeaY;
            p.Position = local.ToGlobalPosition();
            if (s.Kind == StratagemKind.Airdrop)
            {
                if (s.DropUnit == null) { p.Reason = "Выбери технику для десанта"; return p; }
                if (!s.DropUnit.AllowedForMe) { p.Reason = "Техника другой фракции"; return p; }
                if (!known) { p.Reason = "Рельеф здесь не прогружен — подлети камерой ближе"; return p; }
                if (!land) { p.Reason = "Десант — только на сушу"; return p; }
                if (PathfindingAgent.RaycastTerrain(local, out var hit) && Vector3.Angle(hit.normal, Vector3.up) > 30f) { p.Reason = "Слишком крутой склон"; return p; }
            }
            else if (MunitionFor(s) == null)
            {
                p.Reason = "В игре не найден подходящий боеприпас";
                return p;
            }
            p.Ok = true;
            p.Reason = "Можно";
            return p;
        }

        // ---------------- launch ----------------

        public static Unit FindVisibleEnemy(GlobalPosition at, float radius)
        {
            if (!GameApi.TryGetLocal(out _, out var hq)) return null;
            Unit best = null;
            float bestD = radius;
            Vector3 p = at.AsVector3();
            foreach (var u in UnitRegistry.allUnits)
            {
                if (u == null || u.disabled || u.NetworkHQ == null || u.NetworkHQ == hq || u is Missile) continue;
                bool seen = false;
                try { seen = hq.IsTargetBeingTracked(u); } catch { }
                if (!seen) continue;
                Vector3 q = u.GlobalPosition().AsVector3();
                float d = new Vector2(q.x - p.x, q.z - p.z).magnitude;
                if (d < bestD) { bestD = d; best = u; }
            }
            return best;
        }

        /// <summary>Pays, starts the cooldown and runs the strike. Returns spawned units (airdrop) through the callback.</summary>
        public static void Launch(MonoBehaviour host, Stratagem s, GameApi.Placement p, Action<Unit, float> onUnitDropped, Action<string> toast)
        {
            float price = s.CurrentPrice();
            GameApi.Spend(price);
            s.ReadyAt = Time.time + s.Cooldown.Value;
            Plugin.Log.LogInfo($"[Stratagems] {s.Name} for {price:0.0}M at {p.Position}");
            if (s.Kind == StratagemKind.Airdrop)
            {
                host.StartCoroutine(AirdropRoutine(host, s, p, onUnitDropped, toast));
            }
            else
            {
                host.StartCoroutine(StrikeRoutine(s, p, toast));
            }
        }

        /// <summary>
        /// Ground height at a point. Terrain colliders exist only near the camera, so far away a
        /// raycast can miss (that is why shells used to appear inside hills). Then we use the
        /// height of units standing nearby, and if there are none the caller flies high instead.
        /// </summary>
        public static bool TryGroundY(Vector3 local, out float y)
        {
            if (PathfindingAgent.RaycastTerrain(local, out var hit))
            {
                y = Mathf.Max(hit.point.y, Datum.LocalSeaY);
                return true;
            }
            float nearest = 800f;
            bool found = false;
            y = Datum.LocalSeaY;
            foreach (var u in UnitRegistry.allUnits)
            {
                if (u == null || u.disabled || u is Missile || u is Aircraft) continue;
                Vector3 q = u.transform.position;
                float d = new Vector2(q.x - local.x, q.z - local.z).magnitude;
                if (d < nearest)
                {
                    nearest = d;
                    y = Mathf.Max(q.y, Datum.LocalSeaY);
                    found = true;
                }
            }
            return found;
        }

        /// <summary>Spawn height for a munition: some metres above the ground, or a safe altitude above every mountain.</summary>
        /// <summary>
        /// Spawn height: SpawnHeight above the ground, but never lower than MinAltitude above the sea —
        /// so a shell can not appear inside a hill even when the terrain there is not loaded.
        /// </summary>
        private static float DropY(Vector3 local)
        {
            float above = Mathf.Max(SpawnHeight.Value, 50f);
            float floor = Datum.LocalSeaY + Mathf.Max(MinAltitude.Value, 0f);
            return TryGroundY(local, out float y) ? Mathf.Max(y + above, floor) : Mathf.Max(Datum.LocalSeaY + above, floor);
        }

        /// <summary>Munition straight above its impact point, nose down, falling vertically — no drift, hits where aimed.</summary>
        private static void DropAbove(MissileDefinition def, Vector3 impact, float speed, FactionHQ hq, Unit target)
        {
            Vector3 g = new Vector3(impact.x, DropY(impact), impact.z);
            Drop(def, g, Vector3.down * speed, hq, target);
        }

        // ---------- rewards: remember where our strikes landed ----------

        private struct StrikeZone
        {
            public GlobalPosition Pos;
            public float Radius;
            public float Until;
        }

        private static readonly List<StrikeZone> zones = new List<StrikeZone>();

        private static void RecordStrike(GlobalPosition at, float radius, StratagemKind kind)
        {
            float extra = kind == StratagemKind.StrategicNuke ? 3000f : kind == StratagemKind.TacticalNuke ? 1200f : 120f;
            zones.Add(new StrikeZone { Pos = at, Radius = radius + extra, Until = Time.time + 60f });
        }

        /// <summary>Was this place hit by one of our stratagems recently?</summary>
        public static bool InRecentStrike(GlobalPosition at)
        {
            zones.RemoveAll(z => z.Until < Time.time);
            Vector3 p = at.AsVector3();
            foreach (var z in zones)
            {
                Vector3 q = z.Pos.AsVector3();
                if (new Vector2(q.x - p.x, q.z - p.z).magnitude <= z.Radius) return true;
            }
            return false;
        }

        private static Vector3 RandomInCircle(Vector3 c, float r, Func<float> r01)
        {
            float a = r01() * Mathf.PI * 2f, d = Mathf.Sqrt(r01()) * r;
            return c + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
        }

        public static List<Unit> VisibleEnemiesIn(GlobalPosition at, float radius)
        {
            var list = new List<Unit>();
            if (!GameApi.TryGetLocal(out _, out var hq)) return list;
            Vector3 p = at.AsVector3();
            foreach (var u in UnitRegistry.allUnits)
            {
                if (u == null || u.disabled || u.NetworkHQ == null || u.NetworkHQ == hq || u is Missile) continue;
                bool seen = false;
                try { seen = hq.IsTargetBeingTracked(u); } catch { }
                if (!seen) continue;
                Vector3 q = u.GlobalPosition().AsVector3();
                if (new Vector2(q.x - p.x, q.z - p.z).magnitude <= radius) list.Add(u);
            }
            list.Sort((a, b) => b.definition.value.CompareTo(a.definition.value)); // most valuable first
            return list;
        }


        /// <summary>
        /// Spawns one munition exactly at 'local'. The game's own SpawnSavedMissile instantiates the
        /// prefab at the local origin and moves only the Transform — the Rigidbody stays at the origin,
        /// which is always right next to the camera (floating origin). That is why every shell used to
        /// appear under the player's camera. Here the object is created at the right point from the start
        /// and the Rigidbody is placed there too.
        /// </summary>
        private static void Drop(MissileDefinition def, Vector3 local, Vector3 velocity, FactionHQ hq, Unit target)
        {
            var spawner = NetworkSceneSingleton<Spawner>.i;
            if (spawner == null || def?.unitPrefab == null) return;
            var rot = Quaternion.LookRotation(velocity.sqrMagnitude > 0.01f ? velocity.normalized : Vector3.down);
            GlobalPosition gpos = local.ToGlobalPosition();
            GameObject go = UnityEngine.Object.Instantiate(def.unitPrefab, local, rot);
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = local;
                rb.rotation = rot;
                rb.velocity = velocity;
            }
            var m = go.GetComponent<Missile>();
            if (m == null)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }
            m.NetworkUniqueName = "FCstrike" + (++counter);
            m.NetworkstartPosition = gpos;
            m.NetworkstartRotation = rot;
            m.NetworkstartingVelocity = velocity;
            m.NetworkunitName = m.definition.unitName;
            m.NetworkownerID = PersistentID.None;
            m.NetworkHQ = hq;
            if (target != null)
            {
                try
                {
                    m.SetTarget(target);
                }
                catch
                {
                }
            }
            Physics.SyncTransforms();
            spawner.ServerObjectManager.Spawn(go);
            // make sure nothing moved it back to the origin during start-up
            if (m != null && rb != null && (rb.position - local).sqrMagnitude > 1f)
            {
                Plugin.Log.LogWarning($"[Stratagems] munition was moved to {rb.position}, putting it back to {local}");
                rb.position = local;
                m.transform.position = local;
                rb.velocity = velocity;
            }
        }

        private static IEnumerator StrikeRoutine(Stratagem s, GameApi.Placement p, Action<string> toast)
        {
            var def = MunitionFor(s);
            GameApi.TryGetLocal(out _, out var hq);
            float delay = Mathf.Max(0f, Delay.Value);
            toast?.Invoke($"{s.Name}: вызван, удар через {delay:0} с");
            yield return new WaitForSeconds(delay);
            Vector3 c = p.Position.ToLocalPosition();
            Vector3 fwd = p.Rotation * Vector3.forward;
            var rnd = new System.Random();
            Func<float> r01 = () => (float)rnd.NextDouble();
            float r = s.Spread;
            Vector3 side = Vector3.Cross(Vector3.up, fwd);
            float speed = Mathf.Max(DropSpeed.Value, 50f);
            RecordStrike(p.Position, s.Kind == StratagemKind.Carpet ? (s.Point ? 100f : r) : r, s.Kind);
            switch (s.Kind)
            {
                case StratagemKind.Artillery:
                {
                    int n = Mathf.Clamp(ArtilleryShells.Value, 1, 60);
                    for (int i = 0; i < n; i++)
                    {
                        // every shell falls straight down onto its own point of the circle
                        DropAbove(def, RandomInCircle(c, r, r01), speed, hq, null);
                        yield return new WaitForSeconds(0.15f + r01() * 0.25f);
                    }
                    break;
                }
                case StratagemKind.Carpet:
                {
                    float len = s.Point ? 200f : r * 2f;
                    for (int i = 0; i < 14; i++)
                    {
                        Vector3 g = c + fwd * (-len * 0.5f + i * (len / 13f)) + side * ((r01() - 0.5f) * (s.Point ? 10f : 40f));
                        DropAbove(def, g, speed, hq, null);
                        yield return new WaitForSeconds(0.12f);
                    }
                    break;
                }
                case StratagemKind.MissileStrike:
                {
                    var targets = s.Point ? new List<Unit>() : VisibleEnemiesIn(p.Position, r);
                    if (s.Point)
                    {
                        var t = FindVisibleEnemy(p.Position, 150f);
                        if (t != null) targets.Add(t);
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        Unit target = targets.Count > 0 ? targets[i % targets.Count] : null;
                        Vector3 aim;
                        if (target != null && !target.disabled)
                        {
                            // no homing (the game's guidance makes them turn and air-burst) — just a straight
                            // dive onto the point where the target will be when the missile arrives
                            aim = target.transform.position;
                            float fall = Mathf.Max(DropY(aim) - aim.y, 50f) / Mathf.Max(speed, 50f);
                            if (target.rb != null) aim += new Vector3(target.rb.velocity.x, 0f, target.rb.velocity.z) * fall;
                        }
                        else
                        {
                            aim = s.Point ? c : RandomInCircle(c, r, r01);
                        }
                        // a few metres apart so the missiles never touch each other on the way down
                        float a = i * Mathf.PI * 0.5f;
                        aim += new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (s.Point ? 4f : 8f);
                        DropAbove(def, aim, speed, hq, null);
                        yield return new WaitForSeconds(0.5f);
                    }
                    break;
                }
                case StratagemKind.TacticalNuke:
                case StratagemKind.StrategicNuke:
                    DropAbove(def, c, speed, hq, null);
                    break;
            }
        }

        private static IEnumerator AirdropRoutine(MonoBehaviour host, Stratagem s, GameApi.Placement p, Action<Unit, float> onUnitDropped, Action<string> toast)
        {
            var entry = s.DropUnit;
            int n = s.DropCount;
            float each = s.CurrentPrice() / Mathf.Max(n, 1);
            float delay = Mathf.Max(0f, Delay.Value);
            toast?.Invoke($"Десант: {n} × {entry.Name}, высадка через {delay:0} с");
            yield return new WaitForSeconds(delay);
            Vector3 c = p.Position.ToLocalPosition();
            Vector3 right = p.Rotation * Vector3.right;
            float spacing = Mathf.Max(entry.Definition.width, 4f) + 12f;
            for (int i = 0; i < n; i++)
            {
                Vector3 g = c + right * ((i - (n - 1) * 0.5f) * spacing);
                if (!TryGroundY(g, out float gy) || gy < Datum.LocalSeaY + 0.3f) continue;
                Vector3 ground = new Vector3(g.x, gy + entry.Definition.spawnOffset.y + 0.5f, g.z);
                float h = Mathf.Max(AirdropHeight.Value, 0f);
                // spawn up in the air: the game itself opens a parachute for a vehicle that starts > 10 m above ground
                Vector3 start = h > 15f ? ground + Vector3.up * h : ground;
                var pl = new GameApi.Placement { Ok = true, Position = start.ToGlobalPosition(), Rotation = p.Rotation };
                GameApi.PendingForFaction = false;
                GameApi.PendingLoadout = null;
                GameApi.PendingExtraCost = 0f;
                Unit u = GameApi.SpawnNoCharge(entry, pl);
                if (u == null) continue;
                onUnitDropped?.Invoke(u, each);
                if (h > 15f) host.StartCoroutine(EnsureParachute(u, ground.ToGlobalPosition(), h));
                yield return new WaitForSeconds(0.3f);
            }
        }

        private static GameObject parachutePrefab;
        private static bool parachuteSearched;

        /// <summary>The game's cargo parachute (from any vehicle or container prefab that has one).</summary>
        private static GameObject ParachutePrefab()
        {
            if (parachuteSearched) return parachutePrefab;
            parachuteSearched = true;
            try
            {
                foreach (var d in Encyclopedia.Lookup.Values)
                {
                    if (d?.unitPrefab == null) continue;
                    Component c = d.unitPrefab.GetComponent<GroundVehicle>();
                    if (c == null) c = d.unitPrefab.GetComponent<Container>();
                    if (c == null) continue;
                    var go = HarmonyLib.Traverse.Create(c).Field("parachuteSystem").GetValue<GameObject>();
                    if (go != null && go.GetComponent<CargoDeploymentSystem>() != null)
                    {
                        parachutePrefab = go;
                        Plugin.Log.LogInfo("[Stratagems] parachute taken from " + d.name);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[Stratagems] parachute search: " + e.Message);
            }
            return parachutePrefab;
        }

        /// <summary>If the vehicle did not open its own parachute, give it the game's cargo parachute; failing that, lower it smoothly.</summary>
        private static IEnumerator EnsureParachute(Unit u, GlobalPosition groundG, float height)
        {
            yield return new WaitForSeconds(0.8f); // the game checks for its own airdrop after 0.5 s
            if (u == null || u.disabled) yield break;
            if (u.GetComponentInChildren<CargoDeploymentSystem>() != null) yield break;
            var prefab = ParachutePrefab();
            if (prefab != null)
            {
                try
                {
                    UnityEngine.Object.Instantiate(prefab, u.transform).GetComponent<CargoDeploymentSystem>().Initialize(u);
                    yield break;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("[Stratagems] parachute: " + e.Message);
                }
            }
            yield return Descend(u, groundG, Mathf.Max(u.transform.position.y - groundG.ToLocalPosition().y, 1f));
        }

        /// <summary>Fallback descent without a parachute model: kinematic body lowered smoothly, then released.</summary>
        private static IEnumerator Descend(Unit u, GlobalPosition groundG, float height)
        {
            var rb = u != null ? u.GetComponent<Rigidbody>() : null;
            if (rb == null) yield break;
            bool wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
            float t = 0f, dur = Mathf.Clamp(height / 18f, 3f, 12f);
            Quaternion rot = u.transform.rotation;
            while (t < dur && u != null && !u.disabled)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                Vector3 pos = groundG.ToLocalPosition() + Vector3.up * (height * (1f - k)); // global -> local every frame (floating origin)
                rb.position = pos;
                u.transform.SetPositionAndRotation(pos, rot);
                yield return null;
            }
            if (u != null && rb != null)
            {
                rb.position = groundG.ToLocalPosition();
                rb.isKinematic = wasKinematic;
                rb.velocity = Vector3.zero;
            }
        }
    }
}
