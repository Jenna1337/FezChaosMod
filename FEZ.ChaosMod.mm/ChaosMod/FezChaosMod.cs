using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using FezEngine.Components.Scripting;
using FezGame.GameInfo;
using FezEngine.Structure.Input;

namespace FezGame.ChaosMod
{
    /// <summary>
    /// The class where all the Chaos Mod logic is handled.
    /// </summary>
    public class FezChaosMod : DrawableGameComponent
    {
        public static readonly string Version = "0.9.4"
#if DEBUG
        + " (debug)"
#endif
        ;//TODO add a version checker to check for new versions? (accessing the internet might trigger antivirus); see System.Net.WebClient.DownloadStringAsync

        public static new bool Enabled = true;
        public static FezChaosMod Instance;
        public int MaxActiveEffectsToDisplay = 5;

        #region ServiceDependencies
        [ServiceDependency]
        public IContentManagerProvider CMProvider { private get; set; }

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public ITimeService TimeService { private get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { private get; set; }

        [ServiceDependency]
        public IGameCameraManager CameraManager { private get; set; }

        [ServiceDependency]
        public IGameService GameService { private get; set; }

        [ServiceDependency]
        public ISoundManager SM { private get; set; }

        [ServiceDependency]
        public IInputManager InputManager { private get; set; }

        [ServiceDependency]
        public ICollisionManager CollisionManager { private get; set; }

        [ServiceDependency]
        public IBlackHoleManager BlackHoles { private get; set; }

        [ServiceDependency]
        public ILevelService LevelService { private get; set; }

        [ServiceDependency]
        public ISpeechBubbleManager SpeechBubble { private get; set; }

        [ServiceDependency]
        public IDotManager DotHost { private get; set; }
        #endregion

        public class ChaosEffect
        {
            /// <summary>
            /// The function to test if the effect can be activated.
            /// </summary>
            private readonly Func<bool> Condition = null;
            public string Name { get; }
            /// <summary>
            /// Function to run every time the game is drawn.
            /// </summary>
            public Action Func { get; }
            private double _ratio;
            /// <summary>
            /// The number used for the weighted random.
            /// </summary>
            public double Ratio { get => Enabled ? _ratio : 0; set => _ratio = value; }
            /// <summary>
            /// Should be true iff the effect is enabled, there isn't another active effect with the same category, and the effect can be activated.
            /// </summary>
            public bool CanUse => Enabled && (Category == null || FezChaosMod.Instance.activeChaosEffects.FindIndex(a => !a.IsDone && Category.Equals(a.Category)) < 0) && (Condition == null || Condition());
            public bool Enabled = true;
            private double _duration;
            public double Duration { get => _duration <= 0 ? 0 : _duration * FezChaosMod.Instance.EffectsDurationMultiplier; set => _duration = value; }
            public Action OnDone { get; }
            /// <summary>
            /// The category of the effect. 
            /// Determines the heading this effect appears under in ChaosModWindow.
            /// Also used to prevent other effects with the same category from being active at the same time. 
            /// </summary>
            public string Category { get; }
            private readonly Func<bool> _pauseTimerTest;
            public bool ShouldPauseTimer => _pauseTimerTest != null && _pauseTimerTest();

            public ChaosEffect(string name, Action func, double ratio, Func<bool> test, double duration, Action onDone, string category, Func<bool> pauseTimerTest)
            {
                Name = name;
                Func = func;
                _ratio = ratio;
                Condition = test;
                Duration = duration;
                OnDone = onDone;
                Category = category;
                _pauseTimerTest = pauseTimerTest;
            }
            public override string ToString()
            {
                return Name;
            }
        }
        private void AddEffect(string name, Action func, double ratio, Func<bool> test = null, double duration = 0d, Action onDone = null, string category = null, Func<bool> pauseTimerTest = null)
        {
            if (!ChaosEffectsList.Exists(a => a.Name.Equals(name)))
            {
                ChaosEffectsList.Add(new ChaosEffect(name, func, ratio, test, duration, onDone, category, pauseTimerTest));
            }
            else
            {
                ChaosModWindow.LogLineDebug($"Effect with the name {name} already exists!");
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Constants and variables
        public List<string> LevelNamesForRandTele;

        private static Sky[] Skies;
        private static StarField Starfield;
        private static string[] SkiesNames;
        private static string[] HubLevelNames;
        private static string[] BGMusicNames;

        //NesGlitches
        private NesGlitchesWrapper Glitches;

        /// <summary>
        /// The delay that occurs between the activation of effects, in seconds.
        /// </summary>
        public double DelayBetweenEffects = 10f;
        /// <summary>
        /// The multiplier used to multiply the duration of all the effects.
        /// </summary>
        public double EffectsDurationMultiplier = 1f;
        /// <summary>
        /// How many effects are active but not displayed on screen.
        /// </summary>
        private int UnlistedActiveEffects = 0;
        /// <summary>
        /// The timer that keeps track of how long it's been since the previous effect has activated.
        /// </summary>
        private readonly Stopwatch Timer = new Stopwatch();
        private static readonly Random random = new Random();
        /// <summary>
        /// The list of all the chaos mod effects.
        /// </summary>
        public readonly List<ChaosEffect> ChaosEffectsList = new List<ChaosEffect>();
        private static bool DidInit = false;
        private static readonly Stopwatch SongStarterDelayTimer = new Stopwatch();
        private static readonly Stopwatch SkyChangerDelayTimer = new Stopwatch();
        private static readonly Stopwatch TimeInLevelTimer = new Stopwatch();
        private static ChaosModEffectText ChaosModEffectTextDrawer;
        /// <summary>
        /// The bar showing the time until the next effect activates.
        /// </summary>
        private static LinearProgressBar ChaosModNextEffectCountDownProgressBar;
        /// <summary>
        /// The list of effects that are currently active or being displayed on screen.
        /// </summary>
        private readonly List<ActiveChaosEffect> activeChaosEffects = new List<ActiveChaosEffect>();
        /// <summary>
        /// The thing used to draw text and textures to the game window.
        /// </summary>
        private DrawingTools drawingTools;

        #endregion


        private double EffectsRatioSum
        {
            get
            {
                double sum = 0f;
                foreach (ChaosEffect eff in ChaosEffectsList)
                {
                    if (eff.CanUse)
                        sum += eff.Ratio;
                }
                return sum;
            }
        }

        private bool ChaosTimerPaused => !Enabled || GameState.TimePaused || GameState.InCutscene || GameState.Loading || PlayerManager.Action == ActionType.GateWarp || PlayerManager.Action == ActionType.LesserWarp
          || /*LevelManager.Name.Equals("GOMEZ_HOUSE_2D") ||*/ !PlayerManager.CanControl
          || PlayerManager.Action == ActionType.EnteringPipe || PlayerManager.Action.IsEnteringDoor()
          || PlayerManager.Action == ActionType.ExitDoor || PlayerManager.Action == ActionType.ExitDoorCarry || PlayerManager.Action == ActionType.ExitDoorCarryHeavy
          //|| HurtingActions.Contains(PlayerManager.Action)
            /* || PlayerManager.Action != ActionType.Dying || PlayerManager.Action != ActionType.Suffering || PlayerManager.Action != ActionType.SuckedIn || PlayerManager.Action != ActionType.CollectingFez
			|| PlayerManager.Action != ActionType.FindingTreasure*/;
        private bool InIntro // i.e., before getting the FEZ
=> LevelNames.Intro.Contains(LevelManager.Name);/*LevelManager.Name.Contains("2D")
						|| LevelManager.Name.Equals("ELDERS");*/
        private bool InEnding // i.e., after ascending the pyramid
=> LevelNames.Ending.Contains(LevelManager.Name); /*LevelManager.Name.Contains("_END_")
						|| LevelManager.Name.Equals("HEX_REBUILD")
						|| LevelManager.Name.Equals("DRUM");*/
        private bool InCutsceneLevel // i.e., intro or ending
=> InIntro || InEnding;

        private static readonly HashSet<ActionType> HurtingActions = new HashSet<ActionType>()
        {
            ActionType.FreeFalling,//fell too far
            ActionType.Dying,//splatting into the ground
            ActionType.Sinking,//fell into deadly liquid, such as acid/lava
            ActionType.HurtSwim,
            ActionType.CrushHorizontal,//squashed horizontal
            ActionType.CrushVertical,//squashed vertical
            ActionType.SuckedIn,//black hole
            ActionType.Suffering,//exploded or touched hurt trile
        };
        private bool LastHurtValue = false;
        private bool IsHurting => HurtingActions.Contains(PlayerManager.Action);
        private Action OnHurt = () => { };
        public ulong HurtCount = 0;

        public FezChaosMod(Game game)
            : base(game)
        {
        }

        private bool disposing = false;
        protected override void Dispose(bool disposing)
        {
            if (this.disposing)
                return;
            this.disposing = true;
            drawingTools?.Dispose();
            ChaosModEffectTextDrawer?.Dispose();
            if (Glitches != null)
            {
                Glitches.ActiveGlitches = 0;
                Glitches.FreezeProbability = 0f;
                Glitches.Dispose();
                Glitches = null;
            }
            base.Dispose();
        }
        #region Effect-specific stuff
        private void EnsureGlitches()
        {
            if (Glitches == null)
            {
                Type NesGlitchesType = typeof(FezGame.Fez).Assembly.GetType("FezGame.Components.NesGlitches");
                var glitches = NesGlitchesType.GetConstructor(new[] { typeof(Game) }).Invoke(new[] { Game });
                ServiceHelper.AddComponent((DrawableGameComponent)glitches);
                Glitches = new NesGlitchesWrapper(glitches, NesGlitchesType);

                (NesGlitchesType.GetField("GlitchMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(glitches) as Mesh)
                    .Texture = CMProvider.Global.Load<Texture2D>("Other Textures/glitches/glitch_atlas");
                NesGlitchesType.GetField("sGlitches", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(glitches,
                    (from x in CMProvider.GetAllIn("Sounds/Intro\\Elders\\Glitches")
                     select CMProvider.Global.Load<SoundEffect>(x)).ToArray());
                NesGlitchesType.GetField("sTimestretches", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(glitches,
                    (from x in CMProvider.GetAllIn("Sounds/Intro\\Elders\\Timestretches")
                     select CMProvider.Global.Load<SoundEffect>(x)).ToArray());

            }
        }
        private void ResetGlitches()
        {
            Glitches.ActiveGlitches = 0;
            Glitches.FreezeProbability = 0;
            Glitches.DisappearProbability = 0;
        }

        private float NormalLevelZoom => CurrentLevelInfo.PixelsPerTrixel;
        private float NormalLevelGravity => CurrentLevelInfo.Gravity;

        private void ResetZoom()
        {
            if (NormalLevelZoom > 0)
                CameraManager.PixelsPerTrixel = NormalLevelZoom;
        }
        private void ResetGravity()
        {
            CollisionManager.GravityFactor = NormalLevelGravity;
        }
        private bool BlackHolesEnabled
        {
            get
            {
                var holestatelist = (System.Collections.IList)typeof(BlackHolesHost).GetField("holes", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(BlackHoles);
                return holestatelist.Count > 0 && (bool)typeof(BlackHolesHost).GetNestedType("HoleState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetProperty("Enabled", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetValue(holestatelist[0], null);
            }
        }
        IWaiter HoleWaiter = null;
        private bool PlayerInBlackHole => PlayerManager.CurrentVolumes.Any(currentVolume => currentVolume.ActorSettings != null && currentVolume.ActorSettings.IsBlackHole)
                                        || LevelManager.Volumes.Values.Where((Volume x) => x.ActorSettings != null && x.ActorSettings.IsBlackHole).Any(vol =>
                                        {
                                            return vol.IsContainedInCurrentViewpoint(PlayerManager.LeaveGroundPosition)
                                            || vol.IsContainedInCurrentViewpoint(PlayerManager.RespawnPosition);
                                        });
        private void SetBlackHoles(bool? enabled)
        {
            if (enabled == BlackHolesEnabled)
                return;
            var BlackHoleVolumes = LevelManager.Volumes.Values.Where((Volume x) => x.ActorSettings != null && x.ActorSettings.IsBlackHole);

            HoleWaiter?.Cancel();
            //wait until the player is not in a blackhole volume to change the blackholes
            HoleWaiter = Waiters.Wait(() => !PlayerInBlackHole, () =>
             {
                 if (enabled.HasValue)
                 {
                     if (enabled.Value)
                     {
                         BlackHoles.EnableAll();
                     }
                     else
                     {
                         BlackHoles.DisableAll();
                     }
                 }
                 else
                     BlackHoles.Randomize();
             });
        }
        #endregion

        private LevelInfo CurrentLevelInfo { get => LevelInfo.GetLevelInfo(LevelManager.Name); }

        public static Color EffectTextColorBlink = Color.Yellow;
        public static Color EffectTextColorDone = Color.Gray;
        public static Color EffectTextColorActive = Color.White;

        private static readonly Color DefaultUnsaturatedTextBlinkColor = Color.Yellow;
        public static void SetColors(Color ProgressColor, Color TextColor)
        {
            SetColors(ProgressColor, TextColor, TextColor.Darken(0.5f), TextColor.GetSaturation() == 0 ? DefaultUnsaturatedTextBlinkColor : TextColor.HueRotate(180));
        }
        public static void SetColors(Color ProgressColor, Color TextColor, Color EffectTextColorDone, Color EffectTextColorBlink)
        {
            SetColors(ProgressColor, TextColor, TextColor, EffectTextColorDone, EffectTextColorBlink);
        }
        public static void SetColors(Color ProgressColor, Color ProgressTextColor, Color EffectTextColorActive, Color EffectTextColorDone, Color EffectTextColorBlink)
        {
            FezChaosMod.EffectTextColorBlink = EffectTextColorBlink;
            FezChaosMod.EffectTextColorDone = EffectTextColorDone;
            FezChaosMod.EffectTextColorActive = EffectTextColorActive;
            ProgressBar.SetColors(ProgressColor, ProgressTextColor);
        }

        private ChaosModWindow ChaosModWindow;
        private static readonly Color InitializingChaosModSettingsWindowWaitingTextColor = Color.SlateGray;
        public override void Initialize()
        {
            if (Instance != null
                || (Instance == null && typeof(Fez).Assembly != typeof(FezChaosMod).Assembly && typeof(Fez).Assembly.GetType("FezGame.ChaosMod.FezChaosMod") != null))//Injected via MonoMod and as HAT
            {
                //TODO keep ONLY whatever instance of FezChaosMod has the highest Version
                Common.Logger.Log("ChaosMod", "Warning: Attempted to initialize another instance of FezChaosMod");
                ServiceHelper.RemoveComponent(this);
                return;
            }

            Instance = this;
            this.DrawOrder = int.MaxValue;

            _ = Waiters.Wait(() => MemoryContentManager.AssetExists("Skies/DEFAULT"), () => { Waiters.Wait(1, Initialize0); });//wait until the assets are loaded
        }
        private void Initialize0()
        {
            //Game.IsFixedTimeStep = true;
            //Game.TargetElapsedTime = TimeSpan.FromTicks(1);
            if (!DidInit)
            {
                //DidInit = true;

                ChaosModNextEffectCountDownProgressBar = new LinearProgressBar();
                ChaosModEffectTextDrawer = new ChaosModEffectText();
                SetColors(Color.Blue, Color.White);//TODO make this customizable

                LevelNamesForRandTele = LevelNames.Main.Where(a => a != "QUANTUM").ToList();

                SkiesNames = WorldInfo.GetSkiesNames(); //Note: does not include "DEFAULT", "STARLINE", or "ZUSKY"

                HubLevelNames = LevelNames.Hub.ToArray();

                BGMusicNames = WorldInfo.GetBGMusicNames(); //Note: does not include "Grave_Rain"

                Skies = SkiesNames.Select(skyname => CMProvider.Global.Load<Sky>("Skies/" + skyname)).ToArray();
                ServiceHelper.AddComponent(Starfield = new StarField(base.Game)
                {
                    Opacity = 0f,
                    FollowCamera = true
                });

                OnHurt += () => ++HurtCount;

                #region AddEffect calls

                AddEffect("BlackHolesEnable", () => SetBlackHoles(true), 1, duration: 10d, onDone: () => SetBlackHoles(null), category: "BlackHoles", pauseTimerTest: () => PlayerInBlackHole);
                AddEffect("BlackHolesDisable", () => SetBlackHoles(false), 1, duration: 10d, onDone: () => SetBlackHoles(null), category: "BlackHoles", pauseTimerTest: () => PlayerInBlackHole);
                //AddEffect("BlackHolesNormal", () => SetBlackHoles(null), 1, duration: 10d, category: "BlackHoles");

                AddEffect("SetZoom1", () => { CameraManager.PixelsPerTrixel = 1f; }, 1f, () => { return NormalLevelZoom != 1f; }, 10d, ResetZoom, category: "Zoom");
                AddEffect("SetZoom2", () => { CameraManager.PixelsPerTrixel = 2f; }, 1f, () => { return NormalLevelZoom != 2f; }, 10d, ResetZoom, category: "Zoom");
                AddEffect("SetZoom3", () => { CameraManager.PixelsPerTrixel = 3f; }, 1f, () => { return NormalLevelZoom != 3f; }, 10d, ResetZoom, category: "Zoom");
                AddEffect("SetZoom4", () => { CameraManager.PixelsPerTrixel = 4f; }, 1f, () => { return NormalLevelZoom != 4f; }, 10d, ResetZoom, category: "Zoom");

                AddEffect("CubeAddGold", () =>
                {
                    GameState.SaveData.CubeShards++;
                    GameState.SaveData.ScoreDirty = true;
                    GameState.OnHudElementChanged();
                    ServiceHelper.Get<IGomezService>().OnCollectedShard();
                }, 1f, category: "Inventory.GoldCube");
                AddEffect("CubeSubGold", () =>
                {
                    GameState.SaveData.CubeShards--;
                    GameState.SaveData.ScoreDirty = true;
                    GameState.OnHudElementChanged();
                }, 1f, category: "Inventory.GoldCube");
                AddEffect("CubeAddAnti", () =>
                {
                    GameState.SaveData.SecretCubes++;
                    GameState.SaveData.ScoreDirty = true;
                    GameState.OnHudElementChanged();
                    ServiceHelper.Get<IGomezService>().OnCollectedAnti();
                }, 1f, category: "Inventory.AntiCube");
                AddEffect("CubeSubAnti", () =>
                {
                    GameState.SaveData.SecretCubes--;
                    GameState.SaveData.ScoreDirty = true;
                    GameState.OnHudElementChanged();
                }, 1f, category: "Inventory.AntiCube");
                AddEffect("AddKey", () =>
                {
                    GameState.SaveData.Keys++;
                    GameState.OnHudElementChanged();
                }, 1f, category: "Inventory.Key");
                AddEffect("SubKey", () =>
                {
                    GameState.SaveData.Keys--;
                    GameState.OnHudElementChanged();
                }, 1f, category: "Inventory.Key");

                //AddEffect("Jetpack", () => { GameState.JetpackMode = true; }, 1f);
                //AddEffect("DebugModeEnable", () => { GameState.DebugMode = true; }, 1f);
                //AddEffect("DebugModeDisable", () => { GameState.DebugMode = false; }, 1f);

                AddEffect("SetGravityInverted", () => {
                    if(!CurrentLevelInfo.HasSinkBlocks)
                        CollisionManager.GravityFactor = PlayerManager.Action.IsEnteringDoor() ? 1f : -1.0f;//tertiary operator so the door puts Gomez on the top trile surface instead of the bottom
                }, 1f, ()=> !CurrentLevelInfo.HasSinkBlocks, duration: 3d, onDone: ResetGravity, category: "Gravity", pauseTimerTest: () => CurrentLevelInfo.HasSinkBlocks);
                //AddEffect("SetGravity1.0", () => { CollisionManager.GravityFactor = 1.0f; }, 1f, duration: 10d, category: "Gravity");
                AddEffect("SetGravityMoon", () => { CollisionManager.GravityFactor = 0.165f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity");//The gravity of Earth's moon, assuming 1f is equivalent to Earth's gravity
                AddEffect("SetGravityFezMoon", () => { CollisionManager.GravityFactor = 0.25f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity");//The gravity of the game's moon level, "PYRAMID"
                AddEffect("SetGravity0.5", () => { CollisionManager.GravityFactor = 0.5f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity");
                AddEffect("SetGravity1.5", () => { CollisionManager.GravityFactor = 1.5f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity");
                AddEffect("SetGravity1.7", () => { CollisionManager.GravityFactor = 1.7f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity");
                AddEffect("SetGravity1.9", () => { CollisionManager.GravityFactor = 1.9f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity");
                //Note: setting the gravity to 2 or greater causes weird things to occur

                //AddEffect("SoundEffect_ResolvePuzzle", () => { LevelService.ResolvePuzzleSoundOnly(); }, 1f);

                AddEffect("SetSkyRandom", () =>
                {
                    Sky sky = Skies[random.Next(0, Skies.Length)];
                    ChaosModWindow.LogLine("Changed Sky: " + sky.Name);//TODO Changing from certain skies to certain other skies can cause the foreground to go black for some reason
                    LevelManager.ChangeSky(sky);
                    SkyChangerDelayTimer.Restart();
                }, 3f, () => SkyChangerDelayTimer.Elapsed.TotalSeconds > 2f, category: "Sky");
                SkyChangerDelayTimer.Start();
                /*AddEffect("Blind", () =>
                {
                    Sky sky = BlackSky;
                    LevelManager.ChangeSky(sky);//Only seems to work when changing from certain skies
                }, 1f, ()=>CurrentLevelInfo.Sky.Name!="BLACK", duration: 5, onDone: ()=>LevelManager.ChangeSky(CurrentLevelInfo.Sky), category: "Sky");*/
                AddEffect("Starfield", () =>
                {
                    Starfield.Opacity = 1;
                    Starfield.Draw();
                }, 5f, duration: 60, onDone: () => { Starfield.Opacity = 0; Starfield.Draw(); }, category: "Starfield");
                AddEffect("PlayRandomMusic", () =>
                {
                    string songname = BGMusicNames[random.Next(0, BGMusicNames.Length)];
                    ChaosModWindow.LogLine("Changed Music: " + songname);
                    //TODO don't stop playing song beacause of level transitions; might have to forgo SoundManager
                    SM.PlayNewSong(songname);
                    SongStarterDelayTimer.Restart();
                }, 3f, () => SongStarterDelayTimer.Elapsed.TotalSeconds > 2f, category: "Music");
                SongStarterDelayTimer.Start();

                //AddEffect("GoToIntro", () => { LevelManager.ChangeLevel("GOMEZ_HOUSE_2D"); }, 0.1f);
                /*AddEffect("ReloadLevel", ()=>
                {
                    if(LevelManager.LastDestinationVolumeId.HasValue)
                        LevelService.ChangeLevelToVolume(LevelManager.Name, (int)LevelManager.LastDestinationVolumeId, false, false, false);//can crash the game
                    else
                        LevelManager.ChangeLevel(LevelManager.Name);
                }, 1f, () => !(InCutsceneLevel) && TimeInLevelTimer.Elapsed.TotalSeconds > 5f, category: "Teleport");*/
                AddEffect("GoToRandomLevel", () =>
                {
                    if (LevelNamesForRandTele.Count <= 0)
                    {
                        ChaosModWindow.LogLine("No levels found in the random teleport level list");
                        return;
                    }
                    string levelname = LevelNamesForRandTele[random.Next(0, LevelNamesForRandTele.Count)];
                    LevelManager.ChangeLevel(levelname);
                    //GC.Collect();
                }, 0.01f, () => !(InCutsceneLevel) && TimeInLevelTimer.Elapsed.TotalSeconds > 5f, category: "Teleport");
                AddEffect("GoToRandomHubLevel", () =>
                {
                    string levelname = HubLevelNames[random.Next(0, HubLevelNames.Length)];
                    LevelManager.ChangeLevel(levelname);
                }, 0.1f, () => !(InCutsceneLevel) && TimeInLevelTimer.Elapsed.TotalSeconds > 5f, category: "Teleport");
                LevelManager.LevelChanged += TimeInLevelTimer.Restart;
                TimeInLevelTimer.Start();


                AddEffect("Glitches5", () =>
                {
                    EnsureGlitches();
                    Glitches.ActiveGlitches = 5;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.ActiveGlitches");
                AddEffect("Glitches25", () =>
                {
                    EnsureGlitches();
                    Glitches.ActiveGlitches = 25;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.ActiveGlitches");
                AddEffect("Glitches50", () =>
                {
                    EnsureGlitches();
                    Glitches.ActiveGlitches = 50;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.ActiveGlitches");
                AddEffect("Glitches500", () =>
                {
                    EnsureGlitches();
                    Glitches.ActiveGlitches = 500;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.ActiveGlitches");
                AddEffect("FreezeChance0.1", () =>
                {
                    EnsureGlitches();
                    Glitches.FreezeProbability = 0.1f;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.FreezeProbability");
                AddEffect("FreezeChance0.2", () =>
                {
                    EnsureGlitches();
                    Glitches.FreezeProbability = 0.2f;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.FreezeProbability");
                AddEffect("FreezeChance0.5", () =>
                {
                    EnsureGlitches();
                    Glitches.FreezeProbability = 0.5f;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.FreezeProbability");
                AddEffect("FreezeChance1.0", () =>
                {
                    EnsureGlitches();
                    Glitches.FreezeProbability = 1f;
                }, 1f, duration: 3, onDone: ResetGlitches, category: "Glitches.FreezeProbability");


                /*AddEffect("SpawnVase", () =>
                {
                    var trile = new TrileInstance(PlayerManager.Ground.First.Position, LevelManager.ActorTriles(ActorType.Vase).FirstOrDefault().Id);
                    if (trile.PhysicsState == null)
                    {
                        trile.PhysicsState = new InstancePhysicsState(trile);
                    }
                    trile.PhysicsState.Ground = PlayerManager.Ground;//physics don't work
                    trile.Position = PlayerManager.Center;
                    ServiceHelper.AddComponent(new GlitchyRespawner(base.Game, trile)
                    {
                        DontCullIn = true
                    });
                }, 5f, () => PlayerManager.Ground.First != null);*/

                #endregion

                Thread thread = new Thread(() => (this.ChaosModWindow = new ChaosModWindow(this)).ShowDialog());
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                DidInit = true;
                Timer.Start();
                _ = FezEngine.Components.Waiters.Wait(() => ChaosModWindow != null && ChaosModWindow.Created && !ChaosModWindow.IsDisposed && ChaosModWindow.Visible, () =>
                {
                    ChaosModWindow.LogLineDebug(WorldInfo.GetAllLevelDataAsString());

                    ChaosModWindow.LogLineDebug($"Skies: {{{String.Join(", ", SkiesNames)}}}");
                    ChaosModWindow.LogLineDebug($"Songs: {{{String.Join(", ", BGMusicNames)}}}");
                    ChaosModWindow.LogLineDebug($"Hubs: {{{String.Join(", ", HubLevelNames)}}}");
                });
            }
        }
        public class ActiveChaosEffect // Needs to be a  class because if it's a struct then HasDoneOnDone will never be set to true
        {
            private ChaosEffect Effect { get; }
            private Stopwatch ActiveTimer { get; set; }
            private Stopwatch BlinkerTimer { get; set; }
            public bool IsDone => ActiveTimer == null || ActiveTimer.Elapsed.TotalSeconds >= Effect.Duration;
            private bool HasDoneOnDone;
            public double Progress => ActiveTimer != null ? ActiveTimer.Elapsed.TotalSeconds / Effect.Duration : 1;
            public bool Hidden;
            public ActiveChaosEffect(ChaosEffect effect)
            {
                Effect = effect;
                HasDoneOnDone = false;
                ActiveTimer = Stopwatch.StartNew();
                BlinkerTimer = Stopwatch.StartNew();
                Hidden = false;
            }
            public void Draw(int index)
            {
                if (!Hidden)
                {
                    double blinkTime = BlinkerTimer.Elapsed.TotalSeconds;
                    bool ShouldBlink = blinkTime < 3 && blinkTime % 1 < .5;
                    double timeLeft = Effect.Duration - (ActiveTimer != null ? ActiveTimer.Elapsed.TotalSeconds : 0);
                    string Text = timeLeft < 60 ? Math.Ceiling(timeLeft).ToString() : null;
                    Color color = Hidden ? Color.Transparent : (ShouldBlink ? EffectTextColorBlink : (Progress >= 1 ? EffectTextColorDone : EffectTextColorActive));
                    ChaosModEffectTextDrawer.Draw(Effect.Name, Progress, index, Text, color);
                }
            }
            public void Func()
            {
                if (!HasDoneOnDone)
                {
                    if (Effect.ShouldPauseTimer)
                        Pause();
                    else
                        Resume();
                    Effect.Func();
                }
            }
            public string Category => Effect.Category;
            public bool ShouldPauseTimer => Effect.ShouldPauseTimer;
            public void OnDone()
            {
                if (!HasDoneOnDone)
                {
                    HasDoneOnDone = true;
                    Effect.OnDone?.Invoke();
                    ActiveTimer = null;
                    //ActiveTimer.Stop();
                }
            }

            public void Pause() { ActiveTimer?.Stop(); }
            public void Resume() { ActiveTimer?.Start(); }
        }
        private DotHost.BehaviourType LastDotBehave;
        private bool SpiralInterrupted = false;
        public bool AllowRotateAnywhere = false;
        public bool AllowFirstPersonAnywhere = false;
        public bool ZuSpeakEnglish = false;
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            //Initialize0();
            _updatesDone++;
            if (!SpiralInterrupted && LastDotBehave == Components.DotHost.BehaviourType.SpiralAroundWithCamera && DotHost.Behaviour == Components.DotHost.BehaviourType.ReadyToTalk)
            {
                SpiralInterrupted = true;
            }
            if (Enabled)
            {
                if (SpiralInterrupted
                    && !GameState.ScheduleLoadEnd
                    && !GameState.InCutscene
                    && !PlayerManager.CanControl
                    && DotHost.Behaviour != Components.DotHost.BehaviourType.ReadyToTalk
                    && DotHost.Behaviour != Components.DotHost.BehaviourType.SpiralAroundWithCamera)
                {
                    //fixes softlocks that occur when Dot inturrupts the camera spinning around thing when entering a level for the first time
                    PlayerManager.CanControl = true;
                    SpiralInterrupted = false;
                }
            }
            LastDotBehave = DotHost.Behaviour;
            if (AllowRotateAnywhere)
            {
                PlayerManager.CanRotate = true;
                LevelManager.Flat = false;
            }
            if (AllowFirstPersonAnywhere)
            {
                GameState.SaveData.HasFPView = true;
                LevelManager.Flat = false;
            }
            if (ZuSpeakEnglish)
            {
                SpeechBubble.Font = SpeechFont.Pixel;
            }

            bool hurt = IsHurting;
            if (hurt != LastHurtValue)
            {
                if (LastHurtValue = hurt)
                {
                    OnHurt();
                }
            }

        }

        private ulong _updatesDone = 0, _framesRendered = 0, _ups = 0, _fps = 0;
        private DateTime _lastTime = DateTime.Now;
        public bool ShowDebugInfo =
#if DEBUG
true //if debug build
#else
false
#endif
        || System.Diagnostics.Debugger.IsAttached;

        public override void Draw(GameTime gameTime)
        {
            float scale = (float)Math.Floor(Game.GraphicsDevice.GetViewScale());
            Viewport viewport = Game.GraphicsDevice.Viewport;
            bool ChaosTimerPaused = this.ChaosTimerPaused;

            if (drawingTools == null)
            {
                drawingTools = DrawingTools.Instance;
            }

            _framesRendered++;
            if ((DateTime.Now - _lastTime).TotalSeconds >= 1)
            {
                // one second has elapsed 

                _fps = _framesRendered;
                _framesRendered = 0;
                _ups = _updatesDone;
                _updatesDone = 0;
                _lastTime = DateTime.Now;
            }

            if (ChaosTimerPaused)
                Timer.Stop();
            else if (!Timer.IsRunning)
                Timer.Start();

            if (!Enabled)
            {
                //forcily end all active effects
                ActiveChaosEffect activeEffect;
                while (activeChaosEffects.Count > 0)
                {
                    int index = activeChaosEffects.Count - 1;
                    activeEffect = activeChaosEffects[index];
                    activeEffect.OnDone();
                    activeChaosEffects.RemoveAt(index);
                }

            }

            foreach (var activeEffect in activeChaosEffects)
            {
                if (ChaosTimerPaused || activeEffect.ShouldPauseTimer)
                {
                    activeEffect.Pause();
                }
                else
                {
                    activeEffect.Resume();
                }
            }

            var elapsedtime = Timer.Elapsed.TotalSeconds;
            if (elapsedtime >= DelayBetweenEffects && !ChaosTimerPaused)
            {
                //use weighted random to find the next effect to activate
                double numericValue = random.NextDouble() * EffectsRatioSum;

                foreach (var effect in ChaosEffectsList)
                {
                    if (effect.CanUse)
                    {
                        numericValue -= effect.Ratio;

                        if (numericValue > 0)
                            continue;

                        //activate the effect
                        activeChaosEffects.Add(new ActiveChaosEffect(effect));
                        ChaosModWindow.LogLine(effect.Name);
                        effect.Func();
                        Timer.Restart();
                        break;
                    }
                }
            }

            if (!DidInit)
            {
                var vp = ServiceHelper.Get<IGraphicsDeviceService>().GraphicsDevice.Viewport;
                drawingTools.DrawShadowedText("Initializing Chaos Mod...", InitializingChaosModSettingsWindowWaitingTextColor, new Vector2(vp.Width * .01f, vp.Height * .9f), scale);
            }

            if (Enabled && DidInit)
            {
                if (ChaosModWindow != null && ChaosModWindow.Initializing)
                {
                    var vp = ServiceHelper.Get<IGraphicsDeviceService>().GraphicsDevice.Viewport;
                    drawingTools.DrawShadowedText("Initializing Chaos Mod settings window...", InitializingChaosModSettingsWindowWaitingTextColor, new Vector2(vp.Width * .01f, vp.Height * .9f), scale);
                }

                //draw the big countdown timer bar thing
                double timeLeft = DelayBetweenEffects - elapsedtime;
                if (timeLeft < 0)
                    timeLeft = 0;
                string Text = Math.Ceiling(timeLeft).ToString();
                ChaosModNextEffectCountDownProgressBar.DrawProgressBar(
                    elapsedtime / DelayBetweenEffects,
                    Text,
                    new Rectangle(0, 0, viewport.Width, (int)Math.Ceiling(DrawingTools.Instance.MeasureString("0").Y * scale) + 4),
                    scale);

                int index;

                //process effects
                if (!ChaosTimerPaused)
                {
                    foreach (var activeEffect in activeChaosEffects)
                    {
                        //process finished and active effects
                        if (activeEffect.IsDone)
                            activeEffect.OnDone();
                        else
                            activeEffect.Func();
                    }
                    index = 0;
                    //try to clear finished active effects to make room to display new active effects
                    while (activeChaosEffects.Count > MaxActiveEffectsToDisplay)
                    {
                        if (index >= activeChaosEffects.Count)
                            break;
                        var item = activeChaosEffects[index];
                        if (item.IsDone)
                        {
                            activeChaosEffects.Remove(item);
                            index = 0;
                        }
                        else if (++index >= activeChaosEffects.Count)
                            break;
                    }
                }

                //draw the active effects info to the screen
                if (activeChaosEffects.Count > MaxActiveEffectsToDisplay)
                {
                    //there are too many active effects to display
                    UnlistedActiveEffects = activeChaosEffects.Count - MaxActiveEffectsToDisplay;
                }
                else
                {
                    UnlistedActiveEffects = 0;
                }

                if (UnlistedActiveEffects > 0)
                {
                    string unlisted = $"+ {UnlistedActiveEffects} more...";
                    var pos = new Vector2(15, viewport.Height * .75f - CircularProgressBar.Size * MaxActiveEffectsToDisplay * scale);
                    drawingTools.DrawShadowedText(unlisted, EffectTextColorActive, pos, scale * .75f);
                }

                index = MaxActiveEffectsToDisplay - 1;
                for (int i = 0; i < activeChaosEffects.Count; ++i)
                {
                    var activeEffect = activeChaosEffects[i];
                    activeEffect.Hidden = i < UnlistedActiveEffects;
                    activeEffect.Draw(index);
                    if (!activeEffect.Hidden)
                        index--;
                }
            }

            string debugText = $"FezChaosMod Version: {FezChaosMod.Version}\n" +
                   $"Updates/second: {_ups}\n" +
                   $"Frames/second: {_fps}\n" +
                   $"Level Name: {LevelManager.Name}\n" +
                   $"Sky Name: {(LevelManager.Sky != null ? LevelManager.Sky.Name : "")}\n" +
                   $"Water Type: {Enum.GetName(typeof(LiquidType), LevelManager.WaterType)}\n" +
                   $"Water Height: {LevelManager.WaterHeight}\n" +
                   $"Time in Level: {TimeInLevelTimer.Elapsed.TotalSeconds}\n" +
                   $"Gomez Action: {PlayerManager.Action}\n" +
                   $"Gomez Position: {PlayerManager.Position.ToString().Replace("{", "").Replace("}", "")}\n" +
                   $"Camera Orientation: {CameraManager.VisibleOrientation}\n" +
                   $"Camera Rotation: {CameraManager.Rotation.ToString().Replace("{", "").Replace("}", "")}\n" +
                   $"PixelsPerTrixel: {CameraManager.PixelsPerTrixel}\n" +
                   $"HurtCount: {HurtCount}\n" +
                   $"Inputs: {GetCurrentButtonInputsAsString()}\n" +
                   $"Movement: {InputManager.Movement}\n" +
                   $"FreeLook: {InputManager.FreeLook}";

            drawingTools.DrawShadowedText(debugText, ShowDebugInfo ? Color.White : Color.Transparent, Vector2.Zero, scale);

        }

        private string GetCurrentButtonInputsAsString()
        {
            string s = "";
            s += InputManager.Up != 0 ? "u " : "  ";
            s += InputManager.Down != 0 ? "d " : "  ";
            s += InputManager.Left != 0 ? "l " : "  ";
            s += InputManager.Right != 0 ? "r " : "  ";

            s += InputManager.Jump != 0 ? "a " : "  ";
            s += InputManager.GrabThrow != 0 ? "x " : "  ";
            s += InputManager.CancelTalk != 0 ? "b " : "  ";
            s += InputManager.OpenInventory != 0 ? "y " : "  ";

            s += InputManager.RotateLeft != 0 ? "lt " : "   ";
            s += InputManager.RotateRight != 0 ? "rt " : "   ";

            s += InputManager.MapZoomIn != 0 ? "rb " : "   ";
            s += InputManager.MapZoomOut != 0 ? "lb " : "   ";

            s += InputManager.Back != 0 ? "m " : "  ";
            s += InputManager.Start != 0 ? "s " : "  ";
            s += InputManager.ClampLook != 0 ? "rs " : "   ";
            s += InputManager.FpsToggle != 0 ? "ls " : "   ";

            //s += InputManager.ExactUp       != 0 ? "u "  : "  ";

            return s;
        }
    }
}