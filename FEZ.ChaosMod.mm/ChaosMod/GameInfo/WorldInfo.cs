using System;
using System.Collections.Generic;
using System.Linq;
using static FezGame.GameInfo.LevelInfo;

namespace FezGame.GameInfo
{
    /// <summary>
    /// Aggregates <see cref="LevelInfo"/>
    /// </summary>
    class WorldInfo
    {
        public struct LevelConnection // to differentiate from FezEngine.Structure.MapNode.Connection
        {
            public Entrance FromLevel { get; }
            public Entrance ToLevel { get; }
            public bool NeedsKey => FromLevel.NeedsKey || ToLevel.NeedsKey;
            public bool IsPipe => FromLevel.IsPipe || ToLevel.IsPipe;
            public bool IsShortcut => FromLevel.IsShortcut || ToLevel.IsShortcut;
            public int RequiredCubes => FromLevel.RequiredCubes + ToLevel.RequiredCubes;

            public LevelConnection(Entrance fromLevel, Entrance toLevel)
            {
                FromLevel = fromLevel;
                ToLevel = toLevel;
            }
        }

        private static readonly List<LevelInfo> levelInfos = new List<LevelInfo>();
        static WorldInfo()
        {
            levelInfos = GetLevelInfo(LevelNames.All);
        }
        public static Loot GetLoot()
        {
            Loot loot = new Loot();
            foreach (LevelInfo levelInfo in levelInfos)
            {
                loot.Add(levelInfo);
            }
            //Note: Sewer QR code is used in both ZU_THRONE_RUINS and ZU_HOUSE_EMPTY
            /**
			 * Note: GameWideCodes has two codes, and GameWideCodes.MapCode is also in WATERTOWER_SECRET
			 * GameWideCodes.AchievementCode is not used in any level files, and therefore will not be included as a part of WorldInfo
			**/

            return loot;
        }

        public static string GetAllLevelDataAsString()
        {
            return $"{{\"Type\": \"{typeof(WorldInfo).GetFormattedName()}\", \"LevelInfos\": {{\"Type\": \"{levelInfos.GetType().GetFormattedName()}\", \"Count\": {levelInfos.Count}, \"Values\": [{String.Join(", ", levelInfos)}]}}}}";
        }

        //Note: this method is only ever meant to be called once.
        internal static string[] GetSkiesNames()
        {
            return levelInfos.Select(a => a.Sky?.Name).Distinct(StringComparer.OrdinalIgnoreCase).Where(a => a != null).OrderBy(a => a).ToArray();
        }

        //Note: this method is only ever meant to be called once.
        internal static string[] GetBGMusicNames()
        {
            return levelInfos.Select(a => a.Song?.Name).Distinct(StringComparer.OrdinalIgnoreCase).Where(a => a != null).OrderBy(a => a).ToArray();
        }
    }
}
