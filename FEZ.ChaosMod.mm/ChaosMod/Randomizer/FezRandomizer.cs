using System;
using System.Collections.Generic;
using System.Linq;
using FezGame.ChaosMod;
using System.Threading;
using FezEngine.Structure;
using FezGame.GameInfo;
using static FezGame.GameInfo.LevelInfo;

namespace FezGame.Randomizer
{
    class FezRandomizer
    {
        public static readonly string Version = "0.7.2";

        public static bool RoomRandoMode;// = true;
        public static bool ItemRandoMode;

        public static bool Enabled { get => ItemRandoMode || RoomRandoMode; }

        private static readonly List<string> LevelNamesNotForRando = new List<string>()
        {
        LevelNames.Intro,
        LevelNames.Ending,

        "GOMEZ_HOUSE",//gives move doors
		"CABIN_INTERIOR_A",//has one-directional transitions
		"CABIN_INTERIOR_B",//has one-directional transitions
		"PIVOT_THREE_CAVE",//has two bidirectional doors leading to the same "level", one of which is far above the rest of the level and has a lesser warp panel
		"QUANTUM",//seems to often crash the game
        "PYRAMID",//final level
		};

        private static readonly BidirectionalLevelConnectionMap NewConnections = new BidirectionalLevelConnectionMap();

        public static int Seed { get; private set; }
        static FezRandomizer()
        {
            //UnshuffledConnections = WorldInfo.GetConnectionsForLevels(LevelNamesForRando).Unique();
            NewConnections = new BidirectionalLevelConnectionMap(WorldInfo.GetConnectionsWithoutLevels(LevelNamesNotForRando));
        }
        public static void Shuffle()
        {
            Shuffle(Environment.TickCount);
        }
        private static Thread shuffleThread = null;
        public static void Shuffle(int seed)
        {
            ChaosModWindow.LogLineDebug("Shuffling with seed " + seed);
            if (shuffleThread != null)
                shuffleThread.Abort();

            shuffleThread = new Thread(() =>
            {
                //TODO instead of reshuffling if failed, implement some logic like in https://github.com/TestRunnerSRL/OoT-Randomizer/blob/Dev/EntranceShuffle.py

                Loot loot = WorldInfo.GetLoot();

                long randomizationAttempts = 0;
                Random rng = new Random(Seed = seed);
                do
                {
                    randomizationAttempts++;
                    ChaosModWindow.LogLine("Attempting to randomize rooms... Attempts: " + randomizationAttempts);
                    NewConnections.Shuffle(rng);
                } while (!Search.max_explore(NewConnections).can_beat_game());
                ChaosModWindow.LogLine("Randomization complete. Total attempts: " + randomizationAttempts);
            });
            shuffleThread.Start();
            ChaosModWindow.LogLineDebug(shuffleThread.ThreadState.ToString());
        }

        public static bool HasConnection(string fromLevel, string toLevel, int? fromVolume, int? toVolume)
        {
            return NewConnections.HasConnection(fromLevel, toLevel, fromVolume, toVolume);
        }
        public static LevelTarget GetLevelTargetReplacement(string fromLevel, string toLevel, int? fromVolume, int? toVolume)
        {
            return NewConnections.GetLevelTargetReplacement(fromLevel, toLevel, fromVolume, toVolume);
        }

        internal static ActorType GetTreasureReplacement(string levelName, ActorType treasureActorType, bool fromChest)
        {
            throw new NotImplementedException();
            //return treasureActorType;//TODO
        }
        internal static void TryAbortShuffleThread()
        {
            if (shuffleThread != null && shuffleThread.IsAlive)
            {
                shuffleThread.Abort();
                FezRandomizer.RoomRandoMode = false;
                ChaosModWindow.LogLine("Shuffling aborted.");
            }
        }
    }

}