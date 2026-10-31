using System;
using UnityEngine;

namespace FieldCommand
{
    /// <summary>Sorts units into readable groups (ПВО, артиллерия, авианосцы, фрегаты...).</summary>
    internal static class Classifier
    {
        public struct Group
        {
            public int Order;
            public string NameEn;
            public string NameRu;
            public string Name => L.T(NameEn, NameRu);

            public Group(int order, string nameEn, string nameRu)
            {
                Order = order;
                NameEn = nameEn;
                NameRu = nameRu;
            }
        }

        // ---- ground ----
        public static readonly Group AirDefenseSam = new Group(10, "Air defence: SAM systems", "ПВО: зенитные ракетные комплексы");
        public static readonly Group AirDefenseIr = new Group(11, "Air defence: short range (IR)", "ПВО: ближнего действия (ИК)");
        public static readonly Group AirDefenseGun = new Group(12, "Air defence: AA guns", "ПВО: зенитная артиллерия");
        public static readonly Group Radar = new Group(13, "Radars", "Радары и РЛС");
        public static readonly Group Artillery = new Group(20, "Artillery and rocket systems", "Артиллерия и ракетные комплексы");
        public static readonly Group Tanks = new Group(30, "Assault: tanks", "Атака: танки");
        public static readonly Group Armor = new Group(31, "Assault: armour (APC/IFV)", "Атака: бронетехника (БТР/БМП)");
        public static readonly Group Light = new Group(40, "Light vehicles and recon", "Лёгкая техника и разведка");
        public static readonly Group Drones = new Group(41, "Unmanned vehicles", "Беспилотная техника");
        public static readonly Group Logistics = new Group(50, "Supply and logistics", "Снабжение и логистика");

        // ---- naval ----
        public static readonly Group Carrier = new Group(110, "Aircraft carriers", "Авианосцы");
        public static readonly Group Amphib = new Group(111, "Amphibious assault ships (LHA)", "Десантные вертолётоносцы (УДК)");
        public static readonly Group Dock = new Group(112, "Landing ship docks", "Десантные корабли-доки");
        public static readonly Group Cruiser = new Group(120, "Cruisers and battleships", "Крейсеры и линкоры");
        public static readonly Group Destroyer = new Group(121, "Destroyers", "Эсминцы");
        public static readonly Group Frigate = new Group(122, "Frigates", "Фрегаты");
        public static readonly Group Corvette = new Group(123, "Corvettes", "Корветы");
        public static readonly Group LandingCraft = new Group(130, "Landing craft", "Десантные катера");
        public static readonly Group Patrol = new Group(131, "Patrol boats", "Патрульные катера");
        public static readonly Group Support = new Group(140, "Supply ships", "Корабли снабжения");

        // ---- static positions & supplies ----
        public static readonly Group StaticSam = new Group(300, "Static air defence", "Стационарная ПВО");
        public static readonly Group StaticGuns = new Group(301, "Gun positions and fortifications", "Огневые точки и укрепления");
        public static readonly Group StaticRadar = new Group(302, "Static radars", "Стационарные радары");
        public static readonly Group Depots = new Group(310, "Depots and supplies", "Склады и снабжение");
        public static readonly Group ContainersAmmo = new Group(320, "Containers: ammo", "Контейнеры: боеприпасы");
        public static readonly Group ContainersFuel = new Group(321, "Containers: fuel", "Контейнеры: топливо");
        public static readonly Group ContainersRepair = new Group(322, "Containers: repair", "Контейнеры: ремонт");
        public static readonly Group ContainersOther = new Group(323, "Containers: other", "Контейнеры: прочее");
        public static readonly Group Factories = new Group(330, "Factories", "Заводы");
        public static readonly Group OtherBuildings = new Group(340, "Other buildings", "Прочие постройки");

        private static bool HasComp(GameObject prefab, string typeName)
        {
            try
            {
                foreach (var c in prefab.GetComponentsInChildren<Component>(true))
                {
                    if (c != null && c.GetType().Name == typeName) return true;
                }
            }
            catch
            {
            }
            return false;
        }

        private static bool HasAirDefenseWeapon(GameObject prefab)
        {
            try
            {
                foreach (var t in prefab.GetComponentsInChildren<Turret>(true))
                {
                    var ws = t.GetWeaponStations();
                    if (ws == null) continue;
                    foreach (var w in ws)
                    {
                        var info = w?.Weapons != null && w.Weapons.Count > 0 && w.Weapons[0] != null ? w.Weapons[0].info : w?.WeaponInfo;
                        if (info != null && (info.effectiveness.antiAir >= 0.4f || info.effectiveness.antiMissile >= 0.4f)) return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        /// <summary>null = do not offer (civilian / unknown), otherwise the group.</summary>
        public static Group? ClassifyStatic(UnitDefinition def)
        {
            var prefab = def.unitPrefab;
            if (prefab == null) return null;
            if (def is BuildingDefinition bd)
            {
                bool turrets = prefab.GetComponentInChildren<Turret>(true) != null;
                bool radar = HasComp(prefab, "Radar");
                switch (bd.buildingType)
                {
                    case BuildingType.DEF:
                        if (HasAirDefenseWeapon(prefab) || (radar && turrets)) return StaticSam;
                        return turrets ? StaticGuns : (Group?)OtherBuildings;
                    case BuildingType.RDR:
                        return StaticRadar;
                    case BuildingType.AMMO:
                    case BuildingType.DEP:
                        return Depots;
                    case BuildingType.FAC:
                        return Factories;
                    case BuildingType.HGR:
                        return OtherBuildings;
                    case BuildingType.CIV:
                        if (turrets) return HasAirDefenseWeapon(prefab) ? StaticSam : StaticGuns;
                        return Plugin.ShowCivilBuildings.Value ? (Group?)OtherBuildings : null;
                }
                return OtherBuildings;
            }
            if (prefab.GetComponent<Container>() != null)
            {
                if (def.code == "PILOT") return null;
                if (HasComp(prefab, "Rearmer")) return ContainersAmmo;
                if (HasComp(prefab, "Refueler")) return ContainersFuel;
                if (HasComp(prefab, "Repairer")) return ContainersRepair;
                string n = ((def.unitName ?? "") + " " + def.name).ToLowerInvariant();
                if (n.Contains("fuel") || n.Contains("топлив")) return ContainersFuel;
                if (n.Contains("ammo") || n.Contains("munition") || n.Contains("боеприп")) return ContainersAmmo;
                return ContainersOther;
            }
            return null;
        }

        // ---- playable (mods) ----
        public static readonly Group PlayGround = new Group(200, "Playable ground vehicles (drive yourself)", "Играбельная наземка (сесть за руль)");
        public static readonly Group PlayNaval = new Group(201, "Playable ships (take the helm)", "Играбельные корабли (встать за штурвал)");

        public static Group Classify(UnitDefinition def)
        {
            if (def is VehicleDefinition vd)
            {
                return ClassifyVehicle(vd);
            }
            if (def is ShipDefinition sd)
            {
                return ClassifyShip(sd);
            }
            return Logistics;
        }

        private static Group ClassifyVehicle(VehicleDefinition vd)
        {
            var r = vd.roleIdentity;
            switch (vd.vehicleType)
            {
                case VehicleType.R_SAM: return AirDefenseSam;
                case VehicleType.IR_SAM: return AirDefenseIr;
                case VehicleType.AAA: return AirDefenseGun;
                case VehicleType.RDR: return Radar;
                case VehicleType.ART: return Artillery;
                case VehicleType.MBT: return Tanks;
                case VehicleType.AFV: return Armor;
                case VehicleType.LCV: return Light;
                case VehicleType.UGV: return Drones;
            }
            // TRUCK (also the default value modders often leave untouched) — look at the role
            if (r.antiAir >= 0.4f && r.antiAir >= r.antiSurface) return AirDefenseSam;
            if (r.antiSurface >= 0.4f) return vd.armorTier >= 3f ? Tanks : Artillery;
            if (Has(vd.name, "radar") || Has(vd.unitName, "radar")) return Radar;
            return Logistics;
        }

        private static Group ClassifyShip(ShipDefinition sd)
        {
            bool hasAirbase = false;
            try
            {
                hasAirbase = sd.unitPrefab != null && sd.unitPrefab.GetComponentInChildren<Airbase>(true) != null;
            }
            catch
            {
            }
            switch (sd.shipType)
            {
                case ShipType.CV:
                    if (hasAirbase) return Carrier;
                    break; // CV is the enum default — mods often leave it; fall back to size
                case ShipType.LHA: return Amphib;
                case ShipType.LFD: return Dock;
                case ShipType.DDG: return sd.length >= 200f ? Cruiser : Destroyer;
                case ShipType.FFG: return Frigate;
                case ShipType.FFL: return Corvette;
                case ShipType.LC: return LandingCraft;
                case ShipType.PB: return Patrol;
            }
            string n = (sd.unitName ?? "") + " " + sd.name;
            if (Has(n, "supply") || Has(n, "tanker") || Has(n, "replenish") || Has(n, "cargo")) return Support;
            if (Has(n, "carrier")) return hasAirbase ? Carrier : Amphib;
            if (Has(n, "cruiser") || Has(n, "battle")) return Cruiser;
            if (Has(n, "destroyer")) return Destroyer;
            if (Has(n, "frigate")) return Frigate;
            if (Has(n, "corvette")) return Corvette;
            if (hasAirbase) return Carrier;
            float l = sd.length;
            if (l >= 200f) return Cruiser;
            if (l >= 140f) return Destroyer;
            if (l >= 95f) return Frigate;
            if (l >= 50f) return Corvette;
            return Patrol;
        }

        private static bool Has(string s, string what)
        {
            return !string.IsNullOrEmpty(s) && s.IndexOf(what, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ---- playable mod units (KAR = WheelEngine, BOTE = PlayerShipPropulsion) ----

        public enum PlayableKind
        {
            None,
            Ground,
            Naval
        }

        public static PlayableKind DetectPlayable(AircraftDefinition def)
        {
            if (def == null || def.unitPrefab == null)
            {
                return PlayableKind.None;
            }
            try
            {
                foreach (var mb in def.unitPrefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null) continue;
                    string n = mb.GetType().Name;
                    if (n == "PlayerShipPropulsion") return PlayableKind.Naval;
                    if (n == "WheelEngine") return PlayableKind.Ground;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Playable check failed for " + def.name + ": " + e.Message);
            }
            return PlayableKind.None;
        }
    }
}
