using System;
using System.Collections.Generic;
using System.Linq;
using FezGame.ChaosMod;
using System.Threading;
using FezEngine.Structure;
using FezGame.GameInfo;
using static FezGame.GameInfo.LevelInfo;
using FezGame.Services;
using FezEngine.Tools;
using MonoMod.RuntimeDetour;

namespace FezGame.Randomizer
{
    class FezRandomizer
    {
        public static readonly string Version = "0.7.2";

        public static bool RoomRandoMode;// = true;
        public static bool ItemRandoMode;

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }

        [ServiceDependency]
        public IGameCameraManager CameraManager { private get; set; }
        private IDetour ChangeLevelDetour;

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

        private static BidirectionalLevelConnectionMap NewConnections = new BidirectionalLevelConnectionMap();
        public FezRandomizer Instance;
        public static int Seed { get; private set; }
        public FezRandomizer()
        {
            ServiceHelper.InjectServices(this);
            //UnshuffledConnections = WorldInfo.GetConnectionsForLevels(LevelNamesForRando).Unique();
            NewConnections = new BidirectionalLevelConnectionMap(WorldInfo.GetConnectionsWithoutLevels(LevelNamesNotForRando));
            Instance = this;

            ChangeLevelDetour = new Hook(typeof(GameLevelManager).GetMethod("ChangeLevel"),
            (Action<Action<GameLevelManager, string>, GameLevelManager, string>)delegate (Action<GameLevelManager, string> original, GameLevelManager self, string levelName)
            {
                ChangeLevelHooked(original, self, levelName);
            });
        }

        private void ChangeLevelHooked(Action<GameLevelManager, string> original, GameLevelManager self, string levelName)//TODO have this use RuntimeDetour 
        {
            LevelTarget newLevelTarget;
            bool newLevelTargetDidInit = false;
            ChaosModWindow.LogLine("Changing Level from " + self.Name + " to " + levelName);
            if (FezRandomizer.RoomRandoMode)
            {
                if (FezRandomizer.HasConnection(self.Name, levelName, PlayerManager.DoorVolume, self.DestinationVolumeId))
                {
                    newLevelTarget = FezRandomizer.GetLevelTargetReplacement(self.Name, levelName, PlayerManager.DoorVolume, self.DestinationVolumeId);
                    levelName = newLevelTarget.TargetLevelName;
                    self.DestinationVolumeId = newLevelTarget.TargetVolumeId;
                    ChaosModWindow.LogLine("Hijacked Level load; now to " + newLevelTarget.TargetLevelName);
                    newLevelTargetDidInit = true;
                }
            }
            original(self, levelName);
            if (FezRandomizer.RoomRandoMode && newLevelTargetDidInit)
            {
                //TODO orient the camera in the correct direction?; changed some stuff here but have yet to test it

                CameraManager.RebuildView();
                CameraManager.ResetViewpoints();

                var doors = LevelInfo.GetLevelInfo(levelName).Entrances.Where(a => a.VolumeId == self.DestinationVolumeId);
                if (doors.Any())
                {
                    Entrance d;
                    CameraManager.ChangeViewpoint((d = doors.Single()).Exit.AsEntrance(d).FromOrientation.Value.AsViewpoint());
                }
                self.DestinationVolumeId = null;
            }
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