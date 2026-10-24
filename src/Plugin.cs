using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace FieldCommand
{
    public enum FundsSource
    {
        Player,
        Faction
    }

    public enum UnknownUnitPolicy
    {
        AllowForAll,
        Hide
    }

    [BepInPlugin(Guid, "ORTIZ Ops - FieldCommand", Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "ortiz.fieldcommand";
        public const string OldGuid = "com.nikita.fieldcommand";
        public const string Version = "0.9.1";

        internal static ManualLogSource Log;

        // Controls
        internal static ConfigEntry<KeyboardShortcut> MenuKey;
        internal static ConfigEntry<KeyCode> RotateKey;
        internal static ConfigEntry<KeyCode> CameraPlaceKey;
        internal static ConfigEntry<KeyCode> CancelKey;
        internal static ConfigEntry<KeyCode> HoldKey;
        internal static ConfigEntry<KeyCode> AttackKey;
        internal static ConfigEntry<KeyboardShortcut> LeaveKey;
        internal static ConfigEntry<KeyCode> MissileDefenseKey;

        // Factions
        internal static ConfigEntry<bool> RestrictToFaction;
        internal static ConfigEntry<UnknownUnitPolicy> UnknownUnits;
        internal static ConfigEntry<float> FactionShareThreshold;
        internal static ConfigEntry<string> FactionOverrides;
        internal static ConfigEntry<bool> LogFactionTable;
        internal static ConfigEntry<bool> ShowOtherFaction;

        // Playable
        internal static ConfigEntry<bool> EnablePlayable;
        internal static ConfigEntry<bool> EnableStatic;
        internal static ConfigEntry<bool> ShowCivilBuildings;
        internal static ConfigEntry<bool> StaticCapturable;


        // Economy
        internal static ConfigEntry<FundsSource> Funds;
        internal static ConfigEntry<float> PriceMultiplier;
        internal static ConfigEntry<float> MinPrice;
        internal static ConfigEntry<int> MaxOwnUnits;
        internal static ConfigEntry<int> MaxFactionUnits;
        internal static ConfigEntry<float> ScrapRefundPercent;
        internal static ConfigEntry<KeyCode> ScrapKey;
        internal static ConfigEntry<float> KillRewardPercent;
        internal static ConfigEntry<bool> RewardFromFactionUnits;

        // Territory
        internal static ConfigEntry<bool> RestrictToTerritory;
        internal static ConfigEntry<float> AirbaseRadius;
        internal static ConfigEntry<float> NavalAirbaseRadius;
        internal static ConfigEntry<float> FriendlyUnitRadius;
        internal static ConfigEntry<float> MinEnemyDistance;
        internal static ConfigEntry<float> MinShipDepth;

        // Behaviour
        internal static ConfigEntry<bool> SpawnHoldingPosition;
        internal static ConfigEntry<float> GroundAttackStandoff;
        internal static ConfigEntry<float> NavalAttackStandoff;
        internal static ConfigEntry<float> RotateStep;

        private void Awake()
        {
            Log = Logger;
            MigrateOldConfig();

            MenuKey = Config.Bind("1. Управление", "Меню покупки", new KeyboardShortcut(KeyCode.F6), "Открыть/закрыть меню покупки юнитов");
            RotateKey = Config.Bind("1. Управление", "Повернуть", KeyCode.R, "Повернуть юнит при расстановке (Shift — в обратную сторону)");
            CameraPlaceKey = Config.Bind("1. Управление", "Поставить (камера)", KeyCode.G, "Поставить юнит в точку прицела в режиме камеры");
            CancelKey = Config.Bind("1. Управление", "Отмена", KeyCode.X, "Выйти из режима расстановки");
            HoldKey = Config.Bind("1. Управление", "Держать позицию", KeyCode.H, "На карте: переключить «держать позицию» для выделенных своих юнитов");
            AttackKey = Config.Bind("1. Управление", "Атаковать", KeyCode.K, "На карте: выделенные юниты открывают огонь по врагу/ракете под курсором с текущей позиции. Shift+клавиша — выдвинуться на дистанцию огня. По пустому месту — сбросить цель");
            MissileDefenseKey = Config.Bind("1. Управление", "Перехват ракет", KeyCode.L, "На карте: выделенные юниты сами выбирают и сбивают вражеские ракеты (стратегические и ядерные — первыми)");

            RestrictToFaction = Config.Bind("5. Фракции", "Только техника своей фракции", true, "BDF покупает только технику BDF, PALA — только PALA (общая техника доступна обеим)");
            UnknownUnits = Config.Bind("5. Фракции", "Техника без известной фракции", UnknownUnitPolicy.AllowForAll, "Что делать с юнитами, которые ни разу не встречались ни у одной фракции (обычно — из модов): AllowForAll — доступны обеим, Hide — скрыть");
            FactionShareThreshold = Config.Bind("5. Фракции", "Порог принадлежности", 0.75f, new ConfigDescription("Если юнит используется фракцией в ≥ этой доли случаев (миссии, конвои), он считается её техникой; иначе — общей", new AcceptableValueRange<float>(0.5f, 1f)));
            FactionOverrides = Config.Bind("5. Фракции", "Ручная привязка", "", "Через запятую: ключ:BDF, ключ:PALA, ключ:ALL. Ключи юнитов видны в LogOutput.log, если включить «Писать таблицу фракций в лог». Можно писать и имя юнита");
            LogFactionTable = Config.Bind("5. Фракции", "Писать таблицу фракций в лог", false, "Выводить в BepInEx\\LogOutput.log список: ключ | название | фракция");
            ShowOtherFaction = Config.Bind("5. Фракции", "Показывать чужую технику", false, "Показывать технику другой фракции в списке (серым, купить нельзя)");

            EnablePlayable = Config.Bind("6. Играбельная техника", "Включить", true, "Показывать технику из модов, за которую можно играть самому (KAR — наземка, BOTE — корабли). Выбираешь модули, покупаешь — и сразу садишься в неё");


            LeaveKey = Config.Bind("1. Управление", "Выйти из техники KAR/BOTE", new KeyboardShortcut(KeyCode.J), "Выйти из текущей техники KAR/BOTE — она останется стоять на месте без экипажа");

            EnableStatic = Config.Bind("8. Оборона и снабжение", "Включить", true, "Покупка стационарной ПВО, огневых точек, радаров, складов и контейнеров (боеприпасы, топливо, ремонт)");
            ShowCivilBuildings = Config.Bind("8. Оборона и снабжение", "Показывать гражданские постройки", false, "Добавить в список обычные здания без оружия");
            StaticCapturable = Config.Bind("8. Оборона и снабжение", "Постройки можно захватить", false, "Если включено, враг может захватить твои постройки, как обычные здания на базах");

            Funds = Config.Bind("2. Экономика", "Источник денег", FundsSource.Player, "Player — личный бюджет игрока (как при покупке самолётов), Faction — казна фракции");
            PriceMultiplier = Config.Bind("2. Экономика", "Множитель цены", 1f, new ConfigDescription("Цена = стоимость юнита в игре × множитель", new AcceptableValueRange<float>(0f, 10f)));
            MinPrice = Config.Bind("2. Экономика", "Минимальная цена", 0.5f, "Минимальная цена юнита (млн), если у юнита в игре стоимость 0");
            MaxOwnUnits = Config.Bind("2. Экономика", "Лимит своих юнитов", 60, "Сколько юнитов, купленных себе, может быть живо одновременно (0 — без лимита)");
            MaxFactionUnits = Config.Bind("2. Экономика", "Лимит юнитов для фракции", 60, "Сколько юнитов, купленных для фракции, может быть живо одновременно (0 — без лимита). Считается отдельно от своих");
            ScrapRefundPercent = Config.Bind("2. Экономика", "Возврат при списании, %", 50f, new ConfigDescription("Сколько процентов цены покупки возвращается, если списать (убрать) своего юнита", new AcceptableValueRange<float>(0f, 100f)));
            ScrapKey = Config.Bind("1. Управление", "Списать юнитов", KeyCode.Delete, "На карте: убрать выделенных купленных юнитов с возвратом части денег");

            KillRewardPercent = Config.Bind("2. Экономика", "Доля награды за уничтожения, %", 50f, new ConfigDescription("Когда твои купленные юниты уничтожают врага или ракету, ты получаешь этот % от того, что получил бы пилот-игрок за то же уничтожение (0 — выключить)", new AcceptableValueRange<float>(0f, 200f)));
            RewardFromFactionUnits = Config.Bind("2. Экономика", "Награда и за юнитов, купленных фракции", true, "Получать долю и за уничтожения юнитами, которых ты купил для фракции (ими управляет ИИ)");

            RestrictToTerritory = Config.Bind("3. Территория", "Только своя территория", true, "Разрешать расстановку только рядом со своими базами и юнитами");
            AirbaseRadius = Config.Bind("3. Территория", "Радиус от базы (наземка), м", 12000f, "Наземную технику можно ставить в этом радиусе от своей авиабазы");
            NavalAirbaseRadius = Config.Bind("3. Территория", "Радиус от базы (флот), м", 30000f, "Корабли можно ставить в этом радиусе от своей авиабазы — флот можно отстроить даже без живых авианосцев");
            FriendlyUnitRadius = Config.Bind("3. Территория", "Радиус от своих юнитов, м", 3000f, "Ставить можно рядом со своей наземной техникой, кораблями и зданиями");
            MinEnemyDistance = Config.Bind("3. Территория", "Мин. дистанция до врага, м", 2000f, "Нельзя ставить ближе к вражеским наземным юнитам/кораблям/зданиям (0 — без ограничения)");
            MinShipDepth = Config.Bind("3. Территория", "Мин. глубина для корабля, м", 8f, "Минимальная глубина под кораблём (проверяется по носу, корме и центру)");

            SpawnHoldingPosition = Config.Bind("4. Поведение", "Спавнить в режиме «держать позицию»", true, "Купленные юниты стоят на месте и не уезжают выполнять задачи ИИ, пока ты не дашь приказ");
            GroundAttackStandoff = Config.Bind("4. Поведение", "Дистанция атаки (наземка), м", 1500f, "На каком расстоянии от цели останавливается наземка при приказе «Атаковать»");
            NavalAttackStandoff = Config.Bind("4. Поведение", "Дистанция атаки (флот), м", 9000f, "На каком расстоянии от цели останавливаются корабли при приказе «Атаковать»");
            RotateStep = Config.Bind("4. Поведение", "Шаг поворота, град", 45f, "На сколько градусов поворачивать юнит клавишей поворота");

            try
            {
                new HarmonyLib.Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            }
            catch (System.Exception e)
            {
                Log.LogError("Harmony patching failed: " + e);
            }

            Stratagems.Bind(Config);

            var go = new GameObject("[FieldCommand]");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<FieldCommandController>();

            Log.LogInfo($"ORTIZ Ops - FieldCommand v{Version} loaded");
        }
    
        /// <summary>Settings of the old "com.nikita.fieldcommand" build are carried over once.</summary>
        private void MigrateOldConfig()
        {
            try
            {
                string oldPath = System.IO.Path.Combine(Paths.ConfigPath, OldGuid + ".cfg");
                string newPath = Config.ConfigFilePath;
                string marker = newPath + ".migrated";
                if (System.IO.File.Exists(oldPath) && !System.IO.File.Exists(marker))
                {
                    System.IO.File.Copy(oldPath, newPath, true);
                    System.IO.File.WriteAllText(marker, "settings copied from " + OldGuid);
                    Config.Reload();
                    Log.LogInfo("Settings migrated from " + OldGuid);
                }
            }
            catch (System.Exception e)
            {
                Log.LogWarning("Settings migration failed: " + e.Message);
            }
        }
}
}
