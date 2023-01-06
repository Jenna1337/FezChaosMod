using System;
using System.Collections.Generic;
using System.Linq;
using static FezGame.GameInfo.LevelInfo;

namespace FezGame.GameInfo
{
    class WorldInfo
    {
        public struct LevelConnection // to differentiate from FezEngine.Structure.MapNode.Connection
        {
            public Entrance FromLevel { get; }
            public Entrance ToLevel { get; }
            public bool NeedsKey { get => FromLevel.NeedsKey || ToLevel.NeedsKey; }
            public bool IsPipe { get => FromLevel.IsPipe || ToLevel.IsPipe; }
            public bool IsShortcut { get => FromLevel.IsShortcut || ToLevel.IsShortcut; }
            public int RequiredCubes { get => FromLevel.RequiredCubes + ToLevel.RequiredCubes; }

            public LevelConnection(Entrance fromLevel, Entrance toLevel)
            {
                FromLevel = fromLevel;
                ToLevel = toLevel;
            }
        }

        private static readonly LevelConnectionList Connections = new LevelConnectionList();

        private static void AddConnection(Entrance fromLevel, Entrance toLevel)
        {
            if (fromLevel.Exit.TargetLevelName == null || toLevel.Exit.TargetLevelName == null)
                System.Diagnostics.Debugger.Break();
            //ChaosModWindow.LogLineDebug(Connections.HasConnection(fromLevel, toLevel) + ": " + fromLevel.TargetLevelName + " to " + toLevel.TargetLevelName);
            if (!Connections.HasConnection(fromLevel, toLevel))
                Connections.Add(new LevelConnection(fromLevel, toLevel));
        }
        private static readonly List<LevelInfo> levelInfos = new List<LevelInfo>();
        static WorldInfo()
        {
            levelInfos = GetLevelInfo(LevelNames.All);
            foreach (var levelInfo in levelInfos)
            {
                foreach (Entrance toLevel in levelInfo.Entrances)
                {
                    AddConnection(toLevel.Exit.AsEntrance(), toLevel);
                }
            }
            ;
        }

        public static string GetAllLevelDataAsString()
        {
            return $"{typeof(WorldInfo).GetFormattedName()}(LevelInfos: {levelInfos.GetType().GetFormattedName()}(Count = {levelInfos.Count}, Values = {{{String.Join(", ", levelInfos)}}}))";
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
