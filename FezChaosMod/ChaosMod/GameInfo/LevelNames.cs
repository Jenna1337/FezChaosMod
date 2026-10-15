using FezGame.GameInfo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FezGame
{
    public class LevelNames
    {
        private static readonly List<LevelInfo> alllevels = new List<LevelInfo>();
        private static readonly List<LevelInfo> mainlevels = new List<LevelInfo>();
        private static readonly List<LevelInfo> introlevels = new List<LevelInfo>();
        private static readonly List<LevelInfo> endinglevels = new List<LevelInfo>();
        private static readonly List<LevelInfo> hublevels = new List<LevelInfo>();

        static LevelNames() {
            mainlevels = LevelInfo.GetAllLevelInfoByTravelingConnections("GOMEZ_HOUSE");
            mainlevels.Sort();

            hublevels = mainlevels.Where(l => l.IsHubLevel).ToList();
            hublevels.Sort();

            introlevels = LevelInfo.GetAllLevelInfoByTravelingConnections(Fez.ForcedLevelName);//starting level
            introlevels.Sort();

            endinglevels = LevelInfo.GetAllLevelInfoByTravelingConnections("GOMEZ_HOUSE_END_32");
            endinglevels.AddRange(LevelInfo.GetAllLevelInfoByTravelingConnections("GOMEZ_HOUSE_END_64"));
            endinglevels.AddRange(LevelInfo.GetAllLevelInfoByTravelingConnections("HEX_REBUILD"));
            endinglevels.AddRange(LevelInfo.GetAllLevelInfoByTravelingConnections("DRUM"));
            endinglevels.Sort();

            alllevels.AddRange(mainlevels);
            alllevels.AddRange(introlevels);
            alllevels.AddRange(endinglevels);
            //alllevels.Sort();
        }
        private LevelNames() { }

        public static IEnumerable<string> All => alllevels.Select(l => l.Name);
        public static IEnumerable<string> Main => mainlevels.Select(l=>l.Name);
        public static IEnumerable<string> Intro => introlevels.Select(l => l.Name);
        public static IEnumerable<string> Ending => endinglevels.Select(l => l.Name);
        public static IEnumerable<string> Hub => hublevels.Select(l => l.Name);

        internal static string[] GetLevelTypes(string name)
        {
            var ls = new List<string>();
            if (Main.Contains(name)) ls.Add("Main");
            if (Intro.Contains(name)) ls.Add("Intro");
            if (Ending.Contains(name)) ls.Add("Ending");
            if (Hub.Contains(name)) ls.Add("Hub");
            return ls.ToArray();
        }
    }
}