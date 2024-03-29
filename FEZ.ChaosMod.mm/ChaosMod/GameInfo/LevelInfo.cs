using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.ChaosMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace FezGame.GameInfo
{
    /// <summary>
    /// Contains static level data
    /// </summary>
    public class LevelInfo : Loot, IComparable
    {
        [ServiceDependency]
        public IContentManagerProvider CMProvider { get; set; }

        public struct Entrance
        {
            public string LevelName { get; }
            public int? VolumeId { get; }
            private int Requirement { get; }
            public bool IsShortcut => Requirement == -2;  // GameLevelManager.WentThroughSecretPassage
            public bool IsPipe => Requirement == -3;
            public bool NeedsAllOwls => Requirement == -4;
            public bool NeedsKey => Requirement == -1;
            public bool IsWarpGate => Requirement == -5;
            public int RequiredCubes => Requirement > 0 ? Requirement : 0;
            public FaceOrientation? FromOrientation { get; }
            public LevelTarget Exit { get; }
            public Entrance(string LevelName, int? myVolume, FaceOrientation? fromOrientation, int requirement, LevelTarget target)
            {
                this.LevelName = LevelName;
                VolumeId = myVolume;
                FromOrientation = fromOrientation;
                Requirement = requirement;
                Exit = target;
            }
            public Entrance(string LevelName, int? myVolume, string toLevel, int? volumeId, FaceOrientation? fromOrientation, int requirement)
                    : this(LevelName, myVolume, fromOrientation, requirement, new LevelTarget(toLevel, volumeId)) { }

            public override string ToString()
            {
                var str = $"{{\"Type\": \"{this.GetType().GetFormattedName()}\", \"LevelName\": \"{LevelName}\", \"VolumeId\": {(VolumeId.HasValue ? VolumeId.Value.ToString() : "null")}";
                if(IsShortcut)
                    str += $", \"IsShortcut\": {(IsShortcut ? "true" : "false")}";
                if (IsPipe)
                    str += $", \"IsPipe\": {(IsPipe ? "true" : "false")}";
                if (IsWarpGate)
                    str += $", \"IsWarpGate\": {(IsWarpGate ? "true" : "false")}";
                if (NeedsAllOwls)
                    str += $", \"NeedsAllOwls\": {(NeedsAllOwls ? "true" : "false")}";
                if (NeedsKey)
                    str += $", \"NeedsKey\": {(NeedsKey ? "true" : "false")}";
                if (RequiredCubes>0)
                    str += $", \"RequiredCubes\": {RequiredCubes}";
                str += $", \"FromOrientation\": \"{FromOrientation}\", \"Exit\": {Exit}}}";

                return str;
            }
            public override bool Equals(object obj)
            {
                if (obj is Entrance ot)
                {
                    return
                        Requirement.Equals(ot.Requirement) &&
                        FromOrientation.Equals(ot.FromOrientation);
                }
                return false;
            }
            public override int GetHashCode()
            {
                return
                    Requirement.GetHashCode() ^
                    FromOrientation.GetHashCode();
            }
        }
        public struct LevelTarget
        {
            public string TargetLevelName { get; }
            public int? TargetVolumeId { get; }

            public LevelTarget(string toLevel, int? volumeId)
            {
                this.TargetLevelName = toLevel;
                this.TargetVolumeId = volumeId;
            }
            public override string ToString()
            {
                var str = $"{{\"Type\": \"{this.GetType().GetFormattedName()}\", \"TargetLevelName\": \"{TargetLevelName}\"";
                if(TargetVolumeId.HasValue)
                    str += $", \"TargetVolumeId\": {TargetVolumeId}";
                str += $"}}";
                return str;
            }
            public override bool Equals(object obj)
            {
                if (obj is LevelTarget ot)
                {
                    return TargetLevelName.Equals(ot.TargetLevelName) &&
                        TargetVolumeId.Equals(ot.TargetVolumeId);
                }
                return false;
            }
            public override int GetHashCode()
            {
                return TargetLevelName.GetHashCode() ^
                    TargetVolumeId.GetHashCode();
            }
            public Entrance AsEntrance(Entrance FromWhere)
            {
                int? tvid = TargetVolumeId;
                string targlvlname = TargetLevelName;
                var ents = GetLevelInfo(targlvlname).Entrances;
                var en = ents.Find(te => te.VolumeId == tvid);
                if (!FromWhere.IsWarpGate && (en.LevelName == null || en.LevelName == ""))
                {
                    var m = System.Reflection.MethodBase.GetCurrentMethod();
                    ChaosModWindow.LogLineDebug($"{m.DeclaringType.GetFormattedName()}.{m.Name}: Warning: Target level volume ({TargetVolumeId}) does not exist as an entrance in the target level ({TargetLevelName}). Attempting to locate the first entrance in the target level that leads back to this level.");
                    
                    en = ents.Find(te => te.LevelName == FromWhere.LevelName);
                    if (en.LevelName == null || en.LevelName == "")
                    {
                        ChaosModWindow.LogLineDebug($"{m.DeclaringType.GetFormattedName()}.{m.Name}: Failed to find an appropriate entrance");
                        System.Diagnostics.Debugger.Break();
                    }
                }
                return en;
            }
        }

        public List<Entrance> Entrances = new List<Entrance>();

        public float? LowestLiquidHeight { get; } // Should be set to (the Y position of the lowestmost door)-1f

        private readonly Level levelData;

        public string Name => levelData?.Name;
        public float PixelsPerTrixel { get; }
        public float Gravity { get; }
        public Sky Sky => levelData?.Sky;
        public TrileSet TrileSet => levelData?.TrileSet;
        public TrackedSong Song => levelData?.Song;
        public LiquidType? WaterType => levelData?.WaterType;
        public IEnumerable<NpcInstance> NonPlayerCharacters => levelData?.NonPlayerCharacters.Values;
        public List<Volume> BlackHoles => levelData?.Volumes?.Values?.Where(v => v != null && v.ActorSettings != null && v.ActorSettings.IsBlackHole).ToList();

        private readonly Dictionary<ActorType, int> TrileCountByActorType = new Dictionary<ActorType, int>();

        public bool HasSinkBlocks { get; private set; } = false;

        public bool Quantum => levelData.Quantum;
        public bool Rainy => levelData.Rainy;
        public bool Flat => levelData.Flat;
        public Vector3 Size => levelData.Size;


        public readonly bool IsHubLevel;

        private static readonly HashSet<string> LevelNamesBeingLoaded = new HashSet<string>();

        private LevelInfo(string levelName)
        {
            if (levelName == null || levelName.Length <= 0)
                return;

            ServiceHelper.InjectServices(this);

            LevelNamesBeingLoaded.Add(levelName);
            levelData = LoadLevelData(levelName);

            Gravity = 1f;

            //See HolesParticlesHost.TryInitialize()
            if (Name.StartsWith("HOLE"))
                PixelsPerTrixel = 3f;

            var triggerVolumes = new List<string>();
            float tentativePixelsPerTrixel = 0f;
            foreach (var scriptpair in levelData.Scripts)
            {
                foreach (var trigger in scriptpair.Value.Triggers)
                {
                    foreach (var action in scriptpair.Value.Actions)
                    {
                        if (trigger.Event == "Start" && trigger.Object.Type == "Level")
                        {
                            switch (action.Operation)
                            {
                            case "SetPixelsPerTrixel":
                            case "FocusWithPan":
                            case "FocusCamera":
                                PixelsPerTrixel = Int32.Parse(action.Arguments[0]);
                                break;
                            case "SetGravity":
                                Gravity = float.Parse(action.Arguments[1]);
                                break;
                            }
                        }
                        if (action.Operation.Equals("FocusCamera"))
                        {
                            tentativePixelsPerTrixel = Int32.Parse(action.Arguments[0]);
                        }
                        if (trigger.Object.Type == "Volume" && trigger.Object.Identifier != null && levelData.Volumes.ContainsKey((int)trigger.Object.Identifier))
                        {
                            var triggervolume = levelData.Volumes[(int)trigger.Object.Identifier];
                            var ores = triggervolume.Orientations;

                            FaceOrientation? orientation = null;
                            foreach (var i in ores)
                            {
                                orientation = i;
                                break;
                            }

                            switch (action.Operation)
                            {
                            case "ChangeLevelToVolume":
                            case "ChangeToFarAwayLevel":
                                {
                                    int requirements = 0;
                                    if (triggervolume.ActorSettings != null && triggervolume.ActorSettings.IsSecretPassage)
                                        requirements = -2;
                                    else
                                    {
                                        foreach (var scriptpair2 in levelData.Scripts)
                                        {
                                            foreach (var trigger2 in scriptpair2.Value.Triggers)
                                            {
                                                if (trigger2.Object.Type == "BitDoor" && trigger2.Object.Identifier != null && levelData.ArtObjects.ContainsKey((int)trigger2.Object.Identifier))
                                                {
                                                    foreach (var action2 in scriptpair2.Value.Actions)
                                                    {
                                                        if (action2.Operation.Equals("SetEnabled") && action2.Object.Identifier == trigger.Object.Identifier)
                                                        {
                                                            // requirements =  Int32.Parse(Regex.Match(levelData.ArtObjects[(int)trigger2.Object.Identifier].ArtObjectName, @"\d+").Value);
                                                            requirements = levelData.ArtObjects[(int)trigger2.Object.Identifier].ArtObject.ActorType.GetBitCount();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    //checks if requires key
                                    if (CheckVolumeOverlapsWithDoorTrile(triggervolume, levelData.Triles.Values))
                                    {
                                        requirements = -1;
                                    }
                                    if (levelName == "OWL" && action.Arguments[0] == "BIG_OWL")//handles owl door
                                    {
                                        requirements = -4;
                                    }

                                    if (levelData.WaterType.IsWater() && (!LowestLiquidHeight.HasValue || (triggervolume.From.Y < LowestLiquidHeight || triggervolume.To.Y < LowestLiquidHeight)))
                                        LowestLiquidHeight = Math.Min(triggervolume.From.Y, triggervolume.To.Y) - 1f;

                                    Entrances.Add(new Entrance(levelName, trigger.Object.Identifier, action.Arguments[0], Int32.Parse(action.Arguments[1]), orientation, requirements));
                                }
                                break;
                            case "AllowPipeChangeLevel":
                                {
                                    int? targetvolumeid = LoadLevelData(action.Arguments[0]).Scripts.Values.First(s => s.Actions.Any(a => a.Operation == "AllowPipeChangeLevel")).Triggers[0].Object.Identifier;
                                    Entrances.Add(new Entrance(levelName, trigger.Object.Identifier, action.Arguments[0], targetvolumeid, orientation, -3));
                                }
                                break;
                            case "ChangeLevel":
                                {
                                    LevelInfo targetLevel = GetLevelInfo(action.Arguments[0]);
                                    int? targetvolumeid = null;
                                    foreach (var sc in targetLevel.levelData.Scripts)
                                    {
                                        if (sc.Value.Triggers.FindIndex(tr => tr.Object.Type.Equals("Volume")) >= 0 && sc.Value.Actions.FindIndex(ac => Regex.IsMatch(ac.Operation, @"Change\w*Level\w*")) >= 0)
                                        {
                                            if (sc.Value.Triggers.FindIndex(tr => tr.Object.Type.Equals("Volume")) >= 0)
                                            {
                                                targetvolumeid = sc.Value.Triggers.Find(tr => tr.Object.Type.Equals("Volume")).Object.Identifier;
                                                break;
                                            }
                                        }
                                    }
                                    Entrances.Add(new Entrance(levelName, trigger.Object.Identifier, action.Arguments[0], targetvolumeid, orientation, 0));
                                }
                                break;
                            case "LoadHexahedronAt":
                                {
                                    ChaosModWindow.LogLineDebug(String.Join(", ", action.Object.Identifier));
                                    Entrances.Add(new Entrance(levelName, trigger.Object.Identifier, action.Arguments[0], action.Object.Identifier, null, 0));
                                }
                                break;
                            default:
                                {
                                    //the anticubes and pieces of hearts and other things spawned by scripts
                                    string trileToSpawn;
                                    if (action.Operation.Equals("SpawnTrileAt"))
                                    {
                                        trileToSpawn = action.Arguments[0];
                                    }
                                    else if (action.Operation.Equals("GlitchOut"))
                                    {
                                        trileToSpawn = action.Arguments[1];
                                    }
                                    else
                                        continue;
                                    if (trileToSpawn.Equals("SecretCube"))
                                        AntiCubes.AddCode(action.Object, ActorType.SecretCube, levelData, triggervolume.ActorSettings.CodePattern, scriptpair.Value.Conditions);
                                    else if (trileToSpawn.Equals("PieceOfHeart"))
                                        PieceOfHeart.AddCode(action.Object, ActorType.PieceOfHeart, levelData, triggervolume.ActorSettings.CodePattern, scriptpair.Value.Conditions);
                                }
                                break;
                            }
                        }
                    }
                }
            }

            ProcessTriles(levelData.Triles.Values);

            foreach (var pair in levelData.ArtObjects)
            {
                var ao = pair.Value;

                //Handles all treasure chests
                switch (ao.ActorSettings.ContainedTrile)
                {
                case ActorType.CubeShard:
                    GoldCubes.AddContained(ao, ao.ActorSettings.ContainedTrile, Name, ao.ActorSettings.CodePattern);
                    break;
                case ActorType.SecretCube:
                    AntiCubes.AddContained(ao, ao.ActorSettings.ContainedTrile, Name, ao.ActorSettings.CodePattern);
                    break;
                case ActorType.PieceOfHeart:
                    PieceOfHeart.AddContained(ao, ao.ActorSettings.ContainedTrile, Name, ao.ActorSettings.CodePattern);
                    break;

                case ActorType.SkeletonKey:
                    Keys.AddContained(ao, ao.ActorSettings.ContainedTrile, Name, ao.ActorSettings.CodePattern);
                    break;

                case ActorType.NumberCube:
                case ActorType.TriSkull:
                case ActorType.LetterCube:
                case ActorType.Tome:
                    Artifacts.AddContained(ao, ao.ActorSettings.ContainedTrile, Name, ao.ActorSettings.CodePattern);
                    break;
                }

                //handles all maps
                if (!String.IsNullOrEmpty(ao.ActorSettings.TreasureMapName))
                    Maps.AddMap(ao, levelName, ao.ActorSettings.TreasureMapName);

                //handles tuning forks
                if (ao.ArtObjectName.IndexOf("fork", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    AntiCubes.AddFork(ao, Name);

                //handles big hub warp gate
                if (ao.ArtObject.ActorType == ActorType.WarpGate)
                    IsHubLevel = true;

                //handles the smaller warp panel things
                if (ao.ArtObject.ActorType == ActorType.LesserGate)
                {
                    if (ao.ActorSettings.DestinationLevel != null && ao.ActorSettings.DestinationLevel.Length > 0)
                    {
                        Entrances.Add(new Entrance(levelName, null, ao.ActorSettings.DestinationLevel, null, null, -5));
                    }
                }
            }

            //handles hardcoded stuff
            if (levelName == "ZU_ZUISH")
                PieceOfHeart.AddHardcoded(ActorType.PieceOfHeart, new Vector3(13.5f, 57.5f, 14.5f) + Vector3.UnitY * 2f + Vector3.UnitZ, "Hardcoded--PuzzleBased", levelName);
            if (levelName == "CLOCK")
            {
                var RedGroup = levelData.Groups[23];
                var BlueGroup = levelData.Groups[24];
                var GreenGroup = levelData.Groups[25];
                var WhiteGroup = levelData.Groups[26];
                var RedTopMost = RedGroup.Triles.First((TrileInstance x) => x.Emplacement.Y == 58);
                var BlueTopMost = BlueGroup.Triles.First((TrileInstance x) => x.Emplacement.Y == 58);
                var GreenTopMost = GreenGroup.Triles.First((TrileInstance x) => x.Emplacement.Y == 58);
                var WhiteTopMost = WhiteGroup.Triles.First((TrileInstance x) => x.Emplacement.Y == 58);

                AntiCubes.AddHardcoded(ActorType.SecretCube, RedTopMost.Position + Vector3.Up * 1.5f, "Hardcoded--TimeBased", levelName);
                AntiCubes.AddHardcoded(ActorType.SecretCube, BlueTopMost.Position + Vector3.Up * 1.5f, "Hardcoded--TimeBased", levelName);
                AntiCubes.AddHardcoded(ActorType.SecretCube, GreenTopMost.Position + Vector3.Up * 1.5f, "Hardcoded--TimeBased", levelName);
                AntiCubes.AddHardcoded(ActorType.SecretCube, WhiteTopMost.Position + Vector3.Up * 1.5f, "Hardcoded--TimeBased", levelName);
            }
            if (levelName == "ZU_UNFOLD")
                AntiCubes.AddHardcoded(ActorType.SecretCube, new Vector3(9f, 57.5f, 11f) + Vector3.UnitY * 2f + Vector3.UnitZ, "Hardcoded--PuzzleBased", levelName);
            if (levelName == "BELL_TOWER")
                AntiCubes.AddHardcoded(ActorType.SecretCube, levelData.ArtObjects.Values.FirstOrDefault((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.Bell).Position, "Hardcoded--PuzzleBased", levelName);
            if (levelName == "ZU_TETRIS")
                AntiCubes.AddHardcoded(ActorType.SecretCube, new Vector3(14.5f, 19.5f, 13.5f) + Vector3.UnitY, "Hardcoded--PuzzleBased", levelName);

            //handles if the level didn't explicitly specify the PixelsPerTrixel
            if (PixelsPerTrixel <= 0)
            {
                if (tentativePixelsPerTrixel > 0)
                    PixelsPerTrixel = tentativePixelsPerTrixel;
                else
                {
                    //Sets PixelsPerTrixel to the most common PixelsPerTrixel of the connected Levels
                    var entrances = Entrances.Where(Entrance0 => !LevelNamesBeingLoaded.Contains(Entrance0.Exit.TargetLevelName))
                                             .Select(Entrance1 => GetLevelInfo(Entrance1.Exit.TargetLevelName).PixelsPerTrixel);
                    if (entrances.Any())
                        PixelsPerTrixel = entrances.GroupBy(a => a).OrderByDescending(a => a.Count()).First().Key;
                    else
                        ChaosModWindow.LogLineDebug("No zoom info found for " + levelName);
                }

            }

            //handles owl npcs
            if (levelName != "OWL")
                foreach (NpcInstance npc in levelData.NonPlayerCharacters.Values)
                {
                    if (npc.ActorType == ActorType.Owl)
                        Owls.AddOwl(npc, levelName);
                }

            LevelInfoDict.Add(levelName, this);

            LevelNamesBeingLoaded.Remove(levelName);
            //end of initializer LevelInfo(string levelName)
        }

        private bool CheckVolumeOverlapsWithDoorTrile(Volume triggervolume, IEnumerable<TrileInstance> triles)
        {
            foreach (var trile in triles)
            {
                if (trile.OverlappedTriles != null && trile.OverlappedTriles.Count > 0)
                {
                    if (CheckVolumeOverlapsWithDoorTrile(triggervolume, trile.OverlappedTriles))
                        return true;
                }
                levelData.TrileSet.Triles.TryGetValue(trile.TrileId, out Trile trile2);
                if (trile2 == null)
                    continue;
                if (trile2.ActorSettings.Type == ActorType.Door)
                {
                    if (triggervolume.Orientations.Any(o => triggervolume.IsContainedInViewpoint(trile.Emplacement.AsVector, o.AsViewpoint(), levelData)))
                        return true;
                }
            }
            return false;
        }

        private void ProcessTriles(IEnumerable<TrileInstance> triles, TrileInstance ContainingTrile = null)
        {
            foreach (var trile in triles)
            {
                if (trile.OverlappedTriles != null && trile.OverlappedTriles.Count > 0)
                {
                    ProcessTriles(trile.OverlappedTriles, trile);
                }
                if (levelData.TrileSet.Triles.TryGetValue(trile.TrileId, out Trile trile2))
                {
                    if (trile2 == null)
                        continue;

                    if (!TrileCountByActorType.ContainsKey(trile2.ActorSettings.Type))
                        TrileCountByActorType.Add(trile2.ActorSettings.Type, 1);
                    else
                        TrileCountByActorType[trile2.ActorSettings.Type] += 1;

                    switch (trile2.ActorSettings.Type)
                    {
                    case ActorType.CubeShard:
                        GoldCubes.AddTrile(trile.Emplacement, trile2.ActorSettings.Type, Name, ContainingTrile);
                        break;
                    case ActorType.GoldenCube:
                        CubeBits.AddTrile(trile.Emplacement, trile2.ActorSettings.Type, Name, ContainingTrile);
                        break;
                    case ActorType.SecretCube:
                        AntiCubes.AddTrile(trile.Emplacement, trile2.ActorSettings.Type, Name, ContainingTrile);
                        break;
                    case ActorType.PieceOfHeart:
                        PieceOfHeart.AddTrile(trile.Emplacement, trile2.ActorSettings.Type, Name, ContainingTrile);
                        break;
                    case ActorType.SinkPickup:
                        HasSinkBlocks = true;
                        break;
                    }
                }
            }
        }

        private static readonly Dictionary<string, LevelInfo> LevelInfoDict = new Dictionary<string, LevelInfo>();
        public static LevelInfo GetLevelInfo(string LevelName)
        {
            return LevelName != null ? (LevelInfoDict.TryGetValue(LevelName, out LevelInfo levelInfo) ? levelInfo : new LevelInfo(LevelName)) : null;
        }
        public static List<LevelInfo> GetLevelInfo(IEnumerable<string> LevelNames)
        {
            var collection = new List<LevelInfo>();
            foreach (string LevelName in LevelNames)
                collection.Add(GetLevelInfo(LevelName));
            return collection;
        }

        //copied from GameLevelManager.Load(string levelName)
        private Level LoadLevelData(string levelName)
        {
            if (levelName == null || levelName == "")
                return null;
            levelName = levelName.Replace('\\', '/');
            string text = levelName;
            Level level;
                if (!string.IsNullOrEmpty(levelName))
                {
                    levelName = levelName.Substring(0, levelName.LastIndexOf("/") + 1) + levelName.Substring(levelName.LastIndexOf("/") + 1);
                }
                if (!MemoryContentManager.AssetExists("Levels\\" + levelName.Replace('/', '\\')))
                {
                    levelName = text;
                }
                try
                {
                    level = CMProvider.Global.Load<Level>("Levels/" + levelName);
                }
                catch (Exception e)
                {
                    ChaosModWindow.LogLineDebug(e.ToString());
                    System.Diagnostics.Debugger.Break();
                    throw e;
                }
            
            level.Name = levelName;
            ContentManager forLevel = ServiceHelper.Get<IContentManagerProvider>().GetForLevel(levelName);
            foreach (ArtObjectInstance value in level.ArtObjects.Values)
            {
                value.ArtObject = forLevel.Load<ArtObject>(string.Format("{0}/{1}", "Art Objects", value.ArtObjectName));
            }
            if (level.Sky == null)
            {
                level.Sky = forLevel.Load<Sky>("Skies/" + level.SkyName);
            }
            if (level.TrileSetName != null)
            {
                level.TrileSet = forLevel.Load<TrileSet>("Trile Sets/" + level.TrileSetName);
            }
            if (level.SongName != null)
            {
                level.Song = forLevel.Load<TrackedSong>("Music/" + level.SongName);
                level.Song.Initialize();
            }

            return level;
        }

        public override string ToString()
        {
            string str = $"{{\"Type\": \"{this.GetType().GetFormattedName()}\", \"Name\": \"{Name}\"";
            str += $", \"Size\": {Size.ToJsonString()}";
            str += $", \"LevelTypes\": [{String.Join(", ", LevelNames.GetLevelTypes(Name).Select(a=>$"\"{a}\""))}]";
            str += $", \"Quantum\": {(Quantum ? "true" : "false")}";
            str += $", \"Rainy\": {(Rainy ? "true" : "false")}";
            str += $", \"Flat\": {(Flat ? "true" : "false")}";
            str += $", \"HasSinkBlocks\": {(HasSinkBlocks ? "true" : "false")}";
            str += $", \"TrileCountByActorType\": {TrileCountByActorType.ToJsonString()}";
            if (Entrances.Count > 0)
                str += $", \"Entrances\": {{\"Type\": \"{Entrances.GetType().GetFormattedName()}\", \"Count\": {Entrances.Count}, \"Values\": [{String.Join(", ", Entrances)}]}}";
            if (LowestLiquidHeight.HasValue)
                str += $", \"LowestLiquidHeight\": {LowestLiquidHeight}";
            //if (HasLoot())
            {
                if (Keys.Count > 0)
                    str += $", \"Keys\": {Keys}";
                if (GoldCubes.Count > 0)
                    str += $", \"GoldCubes\": {GoldCubes}";
                if (AntiCubes.Count > 0)
                    str += $", \"AntiCubes\": {AntiCubes}";
                if (CubeBits.Count > 0)
                    str += $", \"CubeBits\": {CubeBits}";
                if (PieceOfHeart.Count > 0)
                    str += $", \"PieceOfHeart\": {PieceOfHeart}";
                if (Artifacts.Count > 0)
                    str += $", \"Artifacts\": {Artifacts}";
                if (Maps.Count > 0)
                    str += $", \"Maps\": {Maps}";
                if (Owls.Count > 0)
                    str += $", \"Owls\": {Owls}";
            }
            if(PixelsPerTrixel != 0)
                str += $", \"PixelsPerTrixel\": {PixelsPerTrixel}";
            if (Sky != null)
                str += $", \"Sky\": \"{Sky.Name}\"";
            if (TrileSet != null)
                str += $", \"TrileSet\": \"{TrileSet.Name}\"";
            if (Song != null)
                str += $", \"Song\": \"{Song.Name}\"";
            if (NonPlayerCharacters.Any())
                str += $", \"NonPlayerCharacters\": {{{String.Join(", ", NonPlayerCharacters.Select(npc => $"\"{npc.Id}\": {{\"Name\": \"{npc.Name}\", \"Position\": {npc.Position.ToJsonString()}, \"WalkSpeed\": {npc.WalkSpeed}, \"AvoidsGomez\": {(npc.AvoidsGomez ? "true" : "false")}}}"))}}}";
            if (BlackHoles.Any())
                str += $", \"BlackHoles\": {{\"Type\": \"{BlackHoles.GetType().GetFormattedName()}\", \"Count\": {BlackHoles.Count}, \"Values\": {{{String.Join(", ", BlackHoles.Select(bh => $"\"{bh.Id}\": {{\"From\": {bh.From.ToJsonString()}, \"To\": {bh.To.ToJsonString()}}}"))}}}}}";
            str += "}";
            return str;
        }

        internal static Trile GetTrile(string levelName, TrileInstance trile)
        {
            return GetLevelInfo(levelName).levelData.TrileSet.Triles.TryGetValue(trile.TrileId, out Trile trile2) ? trile2 : null;
        }

        public static List<LevelInfo> GetAllLevelInfoByTravelingConnections(string initialLevelName)
        {
            return GetAllLevelInfoByTravelingConnections(GetLevelInfo(initialLevelName));
        }
        private static List<LevelInfo> GetAllLevelInfoByTravelingConnections(LevelInfo initialLevelInfo)
        {
            List<LevelInfo> levelInfos = new List<LevelInfo>() { initialLevelInfo };
            HashSet<string> VisitedRooms = new HashSet<string>() { initialLevelInfo.Name };

            Queue<Entrance> doorsToCheck = new Queue<Entrance>();
            initialLevelInfo.Entrances.ForEach(doorsToCheck.Enqueue);


            while (doorsToCheck.Count > 0)
            {
                string otherlevelname = doorsToCheck.Dequeue().Exit.TargetLevelName;
                if (!VisitedRooms.Contains(otherlevelname))
                {
                    LevelInfo newinfo = LevelInfo.GetLevelInfo(otherlevelname);
                    levelInfos.Add(newinfo);
                    newinfo.Entrances.ForEach(doorsToCheck.Enqueue);
                    VisitedRooms.Add(otherlevelname);
                }
            }
            return levelInfos;
        }

        public int CompareTo(object obj)
        {
            return obj != null && obj is LevelInfo info ? Name.CompareTo(info.Name) : Name.CompareTo(obj);
        }
    }
}
