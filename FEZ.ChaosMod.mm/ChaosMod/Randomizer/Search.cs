using FezGame.GameInfo;
using System;
using System.Collections.Generic;
using static FezGame.GameInfo.WorldInfo;

namespace FezGame.Randomizer
{
    internal class Search
    {
        private readonly HashSet<string> VisitedRooms;
        private readonly Queue<LevelConnection> levelConnectionsToCheck;
        private readonly Loot inventory;

        private static readonly LevelConnectionList originalConnections = WorldInfo.GetConnections();

        //inspired by https://github.com/TestRunnerSRL/OoT-Randomizer/blob/b7cac173e17373fc56701f22663df089c4dc808f/Search.py

        internal Search(HashSet<string> VisitedRooms = null, Queue<LevelConnection> levelConnectionsToCheck = null, Loot loot = null)
        {
            this.VisitedRooms = VisitedRooms ?? new HashSet<string>(new string[]{ "GOMEZ_HOUSE", "VILLAGEVILLE_3D" });
            this.levelConnectionsToCheck = levelConnectionsToCheck ?? new Queue<LevelConnection>();
            this.inventory = loot ?? new Loot();
        }

        internal static Search max_explore(IEnumerable<LevelConnection> enumerable)
        {
            Search search = new Search(levelConnectionsToCheck: new Queue<LevelConnection>(enumerable));

            return search.deepSearchAll();
        }
        private Search deepSearchAll()
        {
            int checksSinceLastVisited = 0;
            while (levelConnectionsToCheck.Count > 0)
            {
                if (levelConnectionsToCheck.Count <10)
                    System.Diagnostics.Debugger.Break();
                if (checksSinceLastVisited > levelConnectionsToCheck.Count)
                    break; // could not visit any more levels

                checksSinceLastVisited++;
                LevelConnection connection = levelConnectionsToCheck.Dequeue();
                if (VisitedRooms.Contains(connection.FromLevel.LevelName))
                {
                    if (VisitedRooms.Contains(connection.ToLevel.LevelName))
                        continue;

                    if (connection.RequiredCubes <= inventory.TotalCubes)
                    {
                        if (connection.NeedsKey)
                        {
                            if(inventory.Keys.Count > 0)
                                inventory.Keys.RemoveAt(inventory.Keys.Count-1);//remove any
                            else
                            {
                                levelConnectionsToCheck.Enqueue(connection);
                                continue;
                            }
                                
                        }
                        inventory.Add(LevelInfo.GetLevelInfo(connection.ToLevel.LevelName));
                        bool a = VisitedRooms.Add(connection.ToLevel.LevelName);
                        if (!a) System.Diagnostics.Debugger.Break();
                        checksSinceLastVisited = 0;
                        continue;
                    }
                }
                levelConnectionsToCheck.Enqueue(connection);
            }
            return this;
        }
        internal bool can_beat_game()
        {
            if (can_reach_all())
                System.Diagnostics.Debugger.Break();
            if (can_reach("STARGATE") && can_reach("TEMPLE_OF_LOVE"))
                System.Diagnostics.Debugger.Break();
            //return can_reach("STARGATE") && can_reach("TEMPLE_OF_LOVE");
            return can_reach_all();
        }
        internal bool can_reach(string levelName)
        {
            if(VisitedRooms.Contains(levelName))
                return true;
            return false;//TODO
        }
        internal bool can_reach_all()
        {
            return levelConnectionsToCheck.Count == 0;
        }
        internal bool visited(string levelName)
        {
            return VisitedRooms.Contains(levelName);
        }
    }
}