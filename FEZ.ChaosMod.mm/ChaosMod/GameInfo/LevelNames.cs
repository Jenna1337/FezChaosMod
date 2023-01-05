using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FezGame
{
    public class LevelNames
    {
        private enum LevelType
        {
            All = -1,
            Main = 1,
            Intro = 2,
            Ending = 4,
            Unused = 8,
            Hub = 16,

        }
        private struct LevelIdentifiers
        {
            public string Name { get; }
            public LevelType LevelType { get; }
            public LevelIdentifiers(string Name, LevelType levelType)
            {
                this.Name = Name;
                this.LevelType = levelType;
            }
        }

        //TODO dynamically generate this list instead of having hardcoded values?
        private static readonly ReadOnlyCollection<LevelIdentifiers> list = new ReadOnlyCollection<LevelIdentifiers>(new List<LevelIdentifiers>(){
            new LevelIdentifiers("ABANDONED_A", LevelType.Main),
            new LevelIdentifiers("ABANDONED_B", LevelType.Main),
            new LevelIdentifiers("ABANDONED_C", LevelType.Main),
            new LevelIdentifiers("ANCIENT_WALLS", LevelType.Main),
            new LevelIdentifiers("ARCH", LevelType.Main),
            new LevelIdentifiers("BELL_TOWER", LevelType.Main),
            new LevelIdentifiers("BIG_OWL", LevelType.Main),
            new LevelIdentifiers("BIG_TOWER", LevelType.Main),
            new LevelIdentifiers("BOILEROOM", LevelType.Main),
            new LevelIdentifiers("CABIN_INTERIOR_A", LevelType.Main),
            new LevelIdentifiers("CABIN_INTERIOR_B", LevelType.Main),
            new LevelIdentifiers("CLOCK", LevelType.Main),
            new LevelIdentifiers("CMY", LevelType.Main),
            new LevelIdentifiers("CMY_B", LevelType.Main),
            new LevelIdentifiers("CMY_FORK", LevelType.Main),
            new LevelIdentifiers("CODE_MACHINE", LevelType.Main),
            new LevelIdentifiers("CRYPT", LevelType.Main),
            new LevelIdentifiers("DRUM", LevelType.Ending),
            new LevelIdentifiers("ELDERS", LevelType.Intro),
            new LevelIdentifiers("EXTRACTOR_A", LevelType.Main),
            new LevelIdentifiers("FIVE_TOWERS", LevelType.Main),
            new LevelIdentifiers("FIVE_TOWERS_CAVE", LevelType.Main),
            new LevelIdentifiers("FOX", LevelType.Main),
            new LevelIdentifiers("FRACTAL", LevelType.Main),
            new LevelIdentifiers("GEEZER_HOUSE", LevelType.Main),
            new LevelIdentifiers("GEEZER_HOUSE_2D", LevelType.Intro),
            new LevelIdentifiers("GLOBE", LevelType.Main),
            new LevelIdentifiers("GLOBE_INT", LevelType.Main),
            new LevelIdentifiers("GOMEZ_HOUSE", LevelType.Main),
            new LevelIdentifiers("GOMEZ_HOUSE_2D", LevelType.Intro),
            new LevelIdentifiers("GOMEZ_HOUSE_END_32", LevelType.Ending),
            new LevelIdentifiers("GOMEZ_HOUSE_END_64", LevelType.Ending),
            new LevelIdentifiers("GRAVEYARD_A", LevelType.Main),
            new LevelIdentifiers("GRAVEYARD_GATE", LevelType.Main | LevelType.Hub),
            new LevelIdentifiers("GRAVE_CABIN", LevelType.Main),
            new LevelIdentifiers("GRAVE_GHOST", LevelType.Main),
            new LevelIdentifiers("GRAVE_LESSER_GATE", LevelType.Main),
            new LevelIdentifiers("GRAVE_TREASURE_A", LevelType.Main),
            new LevelIdentifiers("HEX_REBUILD", LevelType.Ending),
            new LevelIdentifiers("INDUSTRIAL_CITY", LevelType.Main),
            new LevelIdentifiers("INDUSTRIAL_HUB", LevelType.Main | LevelType.Hub),
            new LevelIdentifiers("INDUSTRIAL_SUPERSPIN", LevelType.Main),
            new LevelIdentifiers("INDUST_ABANDONED_A", LevelType.Main),
            new LevelIdentifiers("KITCHEN", LevelType.Main),
            new LevelIdentifiers("KITCHEN_2D", LevelType.Intro),
            new LevelIdentifiers("LAVA", LevelType.Main),
            new LevelIdentifiers("LAVA_FORK", LevelType.Main),
            new LevelIdentifiers("LAVA_SKULL", LevelType.Main),
            new LevelIdentifiers("LIBRARY_INTERIOR", LevelType.Main),
            new LevelIdentifiers("LIGHTHOUSE", LevelType.Main),
            new LevelIdentifiers("LIGHTHOUSE_HOUSE_A", LevelType.Main),
            new LevelIdentifiers("LIGHTHOUSE_SPIN", LevelType.Main),
            new LevelIdentifiers("MAUSOLEUM", LevelType.Main),
            new LevelIdentifiers("MEMORY_CORE", LevelType.Main),
            new LevelIdentifiers("MINE_A", LevelType.Main),
            new LevelIdentifiers("MINE_BOMB_PILLAR", LevelType.Main),
            new LevelIdentifiers("MINE_WRAP", LevelType.Main),
            new LevelIdentifiers("NATURE_HUB", LevelType.Main | LevelType.Hub),
            new LevelIdentifiers("NUZU_ABANDONED_A", LevelType.Main),
            new LevelIdentifiers("NUZU_ABANDONED_B", LevelType.Main),
            new LevelIdentifiers("NUZU_BOILERROOM", LevelType.Main),
            new LevelIdentifiers("NUZU_DORM", LevelType.Main),
            new LevelIdentifiers("NUZU_SCHOOL", LevelType.Main),
            new LevelIdentifiers("OBSERVATORY", LevelType.Main),
            new LevelIdentifiers("OCTOHEAHEDRON", LevelType.Unused),
            new LevelIdentifiers("OLDSCHOOL", LevelType.Main),
            new LevelIdentifiers("OLDSCHOOL_RUINS", LevelType.Main),
            new LevelIdentifiers("ORRERY", LevelType.Main),
            new LevelIdentifiers("ORRERY_B", LevelType.Main),
            new LevelIdentifiers("OWL", LevelType.Main),
            new LevelIdentifiers("PARLOR", LevelType.Main),
            new LevelIdentifiers("PARLOR_2D", LevelType.Intro),
            new LevelIdentifiers("PIVOT_ONE", LevelType.Main),
            new LevelIdentifiers("PIVOT_THREE", LevelType.Main),
            new LevelIdentifiers("PIVOT_THREE_CAVE", LevelType.Main),
            new LevelIdentifiers("PIVOT_TWO", LevelType.Main),
            new LevelIdentifiers("PIVOT_WATERTOWER", LevelType.Main),
            new LevelIdentifiers("PURPLE_LODGE", LevelType.Main),
            new LevelIdentifiers("PURPLE_LODGE_RUIN", LevelType.Main),
            new LevelIdentifiers("PYRAMID", LevelType.Ending),
            new LevelIdentifiers("QUANTUM", LevelType.Main),
            new LevelIdentifiers("RAILS", LevelType.Main),
            new LevelIdentifiers("RITUAL", LevelType.Main),
            new LevelIdentifiers("SCHOOL", LevelType.Main),
            new LevelIdentifiers("SCHOOL_2D", LevelType.Intro),
            new LevelIdentifiers("SEWER_FORK", LevelType.Main),
            new LevelIdentifiers("SEWER_GEYSER", LevelType.Main),
            new LevelIdentifiers("SEWER_HUB", LevelType.Main | LevelType.Hub),
            new LevelIdentifiers("SEWER_LESSER_GATE_B", LevelType.Main),
            new LevelIdentifiers("SEWER_PILLARS", LevelType.Main),
            new LevelIdentifiers("SEWER_PIVOT", LevelType.Main),
            new LevelIdentifiers("SEWER_QR", LevelType.Main),
            new LevelIdentifiers("SEWER_START", LevelType.Main),
            new LevelIdentifiers("SEWER_TO_LAVA", LevelType.Main),
            new LevelIdentifiers("SEWER_TREASURE_ONE", LevelType.Main),
            new LevelIdentifiers("SEWER_TREASURE_TWO", LevelType.Main),
            new LevelIdentifiers("SHOWERS", LevelType.Main),
            new LevelIdentifiers("SKULL", LevelType.Main),
            new LevelIdentifiers("SKULL_B", LevelType.Main),
            new LevelIdentifiers("SPINNING_PLATES", LevelType.Main),
            new LevelIdentifiers("STARGATE", LevelType.Main | LevelType.Ending),
            new LevelIdentifiers("STARGATE_RUINS", LevelType.Main),
            new LevelIdentifiers("SUPERSPIN_CAVE", LevelType.Main),
            new LevelIdentifiers("TELESCOPE", LevelType.Main),
            new LevelIdentifiers("TEMPLE_OF_LOVE", LevelType.Main),
            new LevelIdentifiers("THRONE", LevelType.Main),
            new LevelIdentifiers("TREE", LevelType.Main),
            new LevelIdentifiers("TREE_CRUMBLE", LevelType.Main),
            new LevelIdentifiers("TREE_OF_DEATH", LevelType.Main),
            new LevelIdentifiers("TREE_ROOTS", LevelType.Main),
            new LevelIdentifiers("TREE_SKY", LevelType.Main),
            new LevelIdentifiers("TRIPLE_PIVOT_CAVE", LevelType.Main),
            new LevelIdentifiers("TWO_WALLS", LevelType.Main),
            new LevelIdentifiers("VILLAGEVILLE_2D", LevelType.Intro),
            new LevelIdentifiers("VILLAGEVILLE_3D", LevelType.Main),
            new LevelIdentifiers("VILLAGEVILLE_3D_END_32", LevelType.Ending),
            new LevelIdentifiers("VILLAGEVILLE_3D_END_64", LevelType.Ending),
            new LevelIdentifiers("VISITOR", LevelType.Main),
            new LevelIdentifiers("WALL_HOLE", LevelType.Main),
            new LevelIdentifiers("WALL_INTERIOR_A", LevelType.Main),
            new LevelIdentifiers("WALL_INTERIOR_B", LevelType.Main),
            new LevelIdentifiers("WALL_INTERIOR_HOLE", LevelType.Main),
            new LevelIdentifiers("WALL_KITCHEN", LevelType.Main),
            new LevelIdentifiers("WALL_SCHOOL", LevelType.Main),
            new LevelIdentifiers("WALL_VILLAGE", LevelType.Main),
            new LevelIdentifiers("WATERFALL", LevelType.Main),
            new LevelIdentifiers("WATERFALL_ALT", LevelType.Unused),
            new LevelIdentifiers("WATERTOWER_SECRET", LevelType.Main),
            new LevelIdentifiers("WATER_PYRAMID", LevelType.Main),
            new LevelIdentifiers("WATER_TOWER", LevelType.Main),
            new LevelIdentifiers("WATER_WHEEL", LevelType.Main),
            new LevelIdentifiers("WATER_WHEEL_B", LevelType.Main),
            new LevelIdentifiers("WEIGHTSWITCH_TEMPLE", LevelType.Main),
            new LevelIdentifiers("WELL_2", LevelType.Main),
            new LevelIdentifiers("WINDMILL_CAVE", LevelType.Main),
            new LevelIdentifiers("WINDMILL_INT", LevelType.Main),
            new LevelIdentifiers("ZU_4_SIDE", LevelType.Main),
            new LevelIdentifiers("ZU_BRIDGE", LevelType.Main),
            new LevelIdentifiers("ZU_CITY", LevelType.Main),
            new LevelIdentifiers("ZU_CITY_RUINS", LevelType.Main | LevelType.Hub),
            new LevelIdentifiers("ZU_CODE_LOOP", LevelType.Main),
            new LevelIdentifiers("ZU_FORK", LevelType.Main),
            new LevelIdentifiers("ZU_HEADS", LevelType.Main),
            new LevelIdentifiers("ZU_HOUSE_EMPTY", LevelType.Main),
            new LevelIdentifiers("ZU_HOUSE_EMPTY_B", LevelType.Main),
            new LevelIdentifiers("ZU_HOUSE_QR", LevelType.Main),
            new LevelIdentifiers("ZU_HOUSE_RUIN_GATE", LevelType.Unused),
            new LevelIdentifiers("ZU_HOUSE_RUIN_VISITORS", LevelType.Main),
            new LevelIdentifiers("ZU_HOUSE_SCAFFOLDING", LevelType.Main),
            new LevelIdentifiers("ZU_LIBRARY", LevelType.Main),
            new LevelIdentifiers("ZU_SWITCH", LevelType.Main),
            new LevelIdentifiers("ZU_SWITCH_B", LevelType.Main),
            new LevelIdentifiers("ZU_TETRIS", LevelType.Main),
            new LevelIdentifiers("ZU_THRONE_RUINS", LevelType.Main),
            new LevelIdentifiers("ZU_UNFOLD", LevelType.Main),
            new LevelIdentifiers("ZU_ZUISH", LevelType.Main),
        });

        private LevelNames() { }

        private static readonly Dictionary<LevelType, IEnumerable<string>> cache = new Dictionary<LevelType, IEnumerable<string>>();

        private static IEnumerable<string> GetLevelNames(LevelType type)
        {
            if (cache.TryGetValue(type, out IEnumerable<string> cachedVal))
                return cachedVal;
            var stuff = list.Where(a => (a.LevelType & type) > 0).Select(a => a.Name);
            cache.Add(type, stuff);
            return stuff;
        }
        public static IEnumerable<string> All { get => GetLevelNames(LevelType.All); }
        public static IEnumerable<string> Main { get => GetLevelNames(LevelType.Main); }
        public static IEnumerable<string> Intro { get => GetLevelNames(LevelType.Intro); }
        public static IEnumerable<string> Ending { get => GetLevelNames(LevelType.Ending); }
        public static IEnumerable<string> Unused { get => GetLevelNames(LevelType.Unused); }
        public static IEnumerable<string> Hub { get => GetLevelNames(LevelType.Hub); }
        public static IEnumerable<string> Cutscene { get => GetLevelNames(LevelType.Intro | LevelType.Ending); }

        public static IEnumerable<string> GetLevelTypes(string levelName)
        {
            var t = list.Single(a => a.Name == levelName).LevelType;
            var ls = new List<string>();
            string[] allLevelTypeNames = System.Enum.GetNames(typeof(LevelType));
            var allLevelTypeValues = (LevelType[])System.Enum.GetValues(typeof(LevelType));
            for (var i = 0; i < allLevelTypeNames.Length; ++i)
                if (allLevelTypeValues[i] >= 0 && (t & allLevelTypeValues[i]) > 0)
                    ls.Add(allLevelTypeNames[i]);
            return ls;
        }
    }
}