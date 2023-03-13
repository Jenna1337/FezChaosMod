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
        /// <summary>
        /// A string representing the current version of this class.
        /// </summary>
        public static readonly string Version = "0.9.4"
#if DEBUG
        + " (debug)"
#endif
        ;//TODO add a version checker to check for new versions? (accessing the internet might trigger antivirus); see System.Net.WebClient.DownloadStringAsync

        /// <summary>
        /// Determines if this <see cref="FezChaosMod"/> is enabled. 
        /// </summary>
        public static new bool Enabled = true;
        /// <summary>
        /// The current singleton instance of <see cref="FezChaosMod"/>. 
        /// </summary>
        public static FezChaosMod Instance { get; private set; }
        /// <summary>
        /// The maximum number of effects that should be displayed on the screen at any point in time. <br/>
        /// <br/>
        /// Should be greater than or equal to <see cref="LatestEffectsToDisplay"/>
        /// </summary>
        public int MaxActiveEffectsToDisplay = 5;
        /// <summary>
        /// The number of effects that always display the latest effects even if the effects are done. <br/>
        /// <br/>
        /// Should be less than or equal to <see cref="MaxActiveEffectsToDisplay"/>
        /// </summary>
        public int LatestEffectsToDisplay = 2;

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
            /// <summary>
            /// The name of this effect.
            /// </summary>
            public string Name { get; }
            /// <summary>
            /// Function to run every time the game is drawn.
            /// </summary>
            public Action Func { get; }
            private double _ratio;
            /// <summary>
            /// The number used for the weighted random. If not <see cref="Enabled">enabled</see>, returns 0.
            /// </summary>
            public double Ratio { get => Enabled ? _ratio : 0; set => _ratio = value; }
            /// <summary>
            /// Should be true iff the effect is enabled, there isn't another active effect with the same <see cref="Category">category</see>, and the effect can be activated.
            /// </summary>
            /// <remarks>Related:<br/>
            /// <seealso cref="Enabled"/><br/>
            /// <seealso cref="Condition"/></remarks>
            public bool CanUse => Enabled && (Category == null || FezChaosMod.Instance.activeChaosEffects.FindIndex(a => !a.IsDone && Category.Equals(a.Category)) < 0) && (Condition == null || Condition());
            /// <summary>
            /// Flag to determine if the effect should 
            /// </summary>
            public bool Enabled = true;
            private double _duration;
            /// <summary>
            /// The duration for which this effect should last, in seconds, multiplied by the <see cref="EffectsDurationMultiplier"/>. <br/>
            /// Note: <see cref="EffectsDurationMultiplier"/> is not used when setting this value.
            /// </summary>
            public double Duration { get => _duration <= 0 ? 0 : _duration * FezChaosMod.Instance.EffectsDurationMultiplier; set => _duration = value; }
            /// <summary>
            /// The action that gets called when the effect is done. 
            /// </summary>
            public Action OnDone { get; }
            /// <summary>
            /// The category of the effect. 
            /// Determines the heading this effect appears under in ChaosModWindow.
            /// Also used to prevent other effects with the same category from being active at the same time. 
            /// </summary>
            public string Category { get; }
            private readonly Func<bool> _pauseTimerTest;
            /// <summary>
            /// Determines if the timer for this effect should be paused. 
            /// </summary>
            public bool ShouldPauseTimer => _pauseTimerTest != null && _pauseTimerTest();

            /// <summary>
            /// Initializes a new <see cref="ChaosEffect"/> with the given parameters.
            /// </summary>
            /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
            /// <param name="func"><inheritdoc cref="Func" path="/summary"/></param>
            /// <param name="ratio"><inheritdoc cref="Ratio" path="/summary"/></param>
            /// <param name="test"><inheritdoc cref="Condition" path="/summary"/></param>
            /// <param name="duration"><inheritdoc cref="Duration" path="/summary"/></param>
            /// <param name="onDone"><inheritdoc cref="OnDone" path="/summary"/></param>
            /// <param name="category"><inheritdoc cref="Category" path="/summary"/></param>
            /// <param name="pauseTimerTest"><inheritdoc cref="ShouldPauseTimer" path="/summary"/></param>
            public ChaosEffect(string name, Action func, double ratio, Func<bool> test = null, double duration = 0d, Action onDone = null, string category = null, Func<bool> pauseTimerTest = null)
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
            /// <summary>
            /// Activates a new instance of this effect even if there is already an active instance. 
            /// </summary>
            public void Activate()
            {
                ActiveChaosEffect activeChaosEffect = new ActiveChaosEffect(this);
                FezChaosMod.Instance.activeChaosEffects.Add(activeChaosEffect);
                ChaosModWindow.LogLine(this.Name);
                FezChaosMod.Instance.ChaosEffectActivated(activeChaosEffect);
                this.Func();
                FezChaosMod.Instance.Timer.Restart();
            }
            /// <summary>
            /// Forcibly ends all active instances of this effect.
            /// </summary>
            public void Terminate()
            {
                int index;
                while ((index = FezChaosMod.Instance.activeChaosEffects.FindIndex(eff => eff.Name == this.Name)) != -1)
                {
                    ActiveChaosEffect activeEffect = FezChaosMod.Instance.activeChaosEffects[index];
                    activeEffect.OnDone();
                    FezChaosMod.Instance.activeChaosEffects.RemoveAt(index);
                }
            }
        }
        /// <summary>
        /// A subscribable event for when a new <see cref="ChaosEffect"/> is added to this instance of <see cref="FezChaosMod"/>
        /// </summary>
        public event Action<ChaosEffect> ChaosEffectAdded;
        /// <summary>
        /// A subscribable event for when a new <see cref="ActiveChaosEffect"/> is activated in this instance of <see cref="FezChaosMod"/>
        /// </summary>
        public event Action<ActiveChaosEffect> ChaosEffectActivated;
        /// <summary>
        /// A subscribable event for when an <see cref="ActiveChaosEffect"/> is done executing in this instance of <see cref="FezChaosMod"/>
        /// </summary>
        public event Action<ActiveChaosEffect> ChaosEffectEnded;
        private class ChaosEffectsListClass : List<ChaosEffect>
        {
            private readonly FezChaosMod myChaosMod;
            internal ChaosEffectsListClass(FezChaosMod chaosMod)
            {
                myChaosMod = chaosMod;
            }

            /// <summary>
            /// Add the supplied effect if there is n
            /// </summary>
            /// <param name="effect"></param>
            public new void Add(ChaosEffect effect)
            {
                if (!this.Exists(a => a.Name.Equals(effect.Name)))
                {
                    base.Add(effect);
                    myChaosMod.ChaosEffectAdded(effect);
                }
                else
                {
                    ChaosModWindow.LogLineDebug($"Effect with the name {effect.Name} already exists!");
                    System.Diagnostics.Debugger.Break();
                }
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
        private GlobalGlitchesManager Glitches;

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
        public readonly List<ChaosEffect> ChaosEffectsList;
        private static bool DidInit = false;
        private string LoadingText = "";
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
            ActionType.HurtSwim,//unused
            ActionType.CrushHorizontal,//squashed horizontal
            ActionType.CrushVertical,//squashed vertical
            ActionType.SuckedIn,//black hole
            ActionType.Suffering,//exploded or touched hurt trile
        };
        private bool LastHurtValue = false;
        private bool IsHurting => HurtingActions.Contains(PlayerManager.Action);
        public event Action OnHurt;
        public ulong HurtCount = 0;

        public FezChaosMod(Game game)
            : base(game)
        {
            ChaosEffectsList = new ChaosEffectsListClass(this);
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
                ServiceHelper.RemoveComponent(Glitches);
                Glitches = null;
            }
            base.Dispose();
        }
        #region Effect-specific stuff
        private void EnsureGlitches()
        {
            if (Glitches == null)
            {
                ServiceHelper.AddComponent(Glitches = new GlobalGlitchesManager(Game));
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

        /// <summary>
        /// The <see cref="LevelInfo"/> for the currently loaded level.
        /// </summary>
        private LevelInfo CurrentLevelInfo { get => LevelInfo.GetLevelInfo(LevelManager.Name); }

        public static Color EffectTextColorBlink = Color.Yellow;
        public static Color EffectTextColorDone = Color.Gray;
        public static Color EffectTextColorActive = Color.White;

        private static readonly Color DefaultUnsaturatedTextBlinkColor = Color.Yellow;
        public static void SetColors(Color ProgressColor, Color TextColor)
        {
            SetColors(ProgressColor, ProgressColor.Desaturate(0.5f).Darken(0.5f), TextColor, TextColor.Darken(0.5f), TextColor.GetSaturation() == 0 ? DefaultUnsaturatedTextBlinkColor : TextColor.HueRotate(180));
        }
        public static void SetColors(Color ProgressColor, Color PausedColor, Color TextColor, Color EffectTextColorDone, Color EffectTextColorBlink)
        {
            SetColors(ProgressColor, PausedColor, TextColor, TextColor, EffectTextColorDone, EffectTextColorBlink);
        }
        public static void SetColors(Color ProgressColor, Color PausedColor, Color ProgressTextColor, Color EffectTextColorActive, Color EffectTextColorDone, Color EffectTextColorBlink)
        {
            FezChaosMod.EffectTextColorBlink = EffectTextColorBlink;
            FezChaosMod.EffectTextColorDone = EffectTextColorDone;
            FezChaosMod.EffectTextColorActive = EffectTextColorActive;
            ProgressBar.SetColors(ProgressColor, ProgressTextColor, PausedColor);
        }

        private ChaosModWindow ChaosModWindow;
        private static readonly Color InitializingChaosModSettingsWindowWaitingTextColor = Color.SlateGray;
        public override void Initialize()
        {
            if (Instance != null
                || (Instance == null && typeof(Fez).Assembly != typeof(FezChaosMod).Assembly && typeof(Fez).Assembly.GetType("FezGame.ChaosMod.FezChaosMod") != null))//Injected via MonoMod and as HAT
            {
                //TODO keep ONLY whatever instance of FezChaosMod has the highest Version; could probably remove this if we rely on HAT, and remove support for MonoMod 
                Common.Logger.Log("ChaosMod", "Warning: Attempted to initialize another instance of FezChaosMod");
                ServiceHelper.RemoveComponent(this);
                return;
            }

            Instance = this;
            this.DrawOrder = int.MaxValue;

            LoadingText = "Loading Content...";
            _ = Waiters.Wait(() => MemoryContentManager.AssetExists("Skies/DEFAULT"), () => { Waiters.Wait(1, Initialize0); });//wait until the assets are loaded
        }
        private void Initialize0()
        {
            //Game.IsFixedTimeStep = true;
            //Game.TargetElapsedTime = TimeSpan.FromTicks(1);
            if (!DidInit)
            {
                LoadingText = "Loading effects";
                //DidInit = true;

                Stopwatch initBenchmarkTimer = Stopwatch.StartNew();

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

                ChaosEffectsList.Add(new ChaosEffect("BlackHolesEnable", () => SetBlackHoles(true), 1, duration: 10d, onDone: () => SetBlackHoles(null), category: "BlackHoles", pauseTimerTest: () => PlayerInBlackHole));
                ChaosEffectsList.Add(new ChaosEffect("BlackHolesDisable", () => SetBlackHoles(false), 1, duration: 10d, onDone: () => SetBlackHoles(null), category: "BlackHoles", pauseTimerTest: () => PlayerInBlackHole));
                //ChaosEffectsList.Add(new ChaosEffect("BlackHolesNormal", () => SetBlackHoles(null), 1, duration: 10d, category: "BlackHoles"));

                ChaosEffectsList.Add(new ChaosEffect("SetZoom1", () => { CameraManager.PixelsPerTrixel = 1f; }, 1f, () => { return NormalLevelZoom != 1f; }, 10d, ResetZoom, category: "Zoom"));
                ChaosEffectsList.Add(new ChaosEffect("SetZoom2", () => { CameraManager.PixelsPerTrixel = 2f; }, 1f, () => { return NormalLevelZoom != 2f; }, 10d, ResetZoom, category: "Zoom"));
                ChaosEffectsList.Add(new ChaosEffect("SetZoom3", () => { CameraManager.PixelsPerTrixel = 3f; }, 1f, () => { return NormalLevelZoom != 3f; }, 10d, ResetZoom, category: "Zoom"));
                ChaosEffectsList.Add(new ChaosEffect("SetZoom4", () => { CameraManager.PixelsPerTrixel = 4f; }, 1f, () => { return NormalLevelZoom != 4f; }, 10d, ResetZoom, category: "Zoom"));

                ChaosEffectsList.Add(new ChaosEffect("CubeAddGold", () =>
                {
                    GameState.SaveData.CubeShards++;
                    GameState.SaveData.ScoreDirty = true;
                    GameState.OnHudElementChanged();
                    ServiceHelper.Get<IGomezService>().OnCollectedShard();
                }, 1f, category: "Inventory.GoldCube"));
                ChaosEffectsList.Add(new ChaosEffect("CubeSubGold", () =>
                {
                    GameState.SaveData.CubeShards--;
                    GameState.SaveData.ScoreDirty = true;
                    GameState.OnHudElementChanged();
                }, 1f, category: "Inventory.GoldCube"));
                ChaosEffectsList.Add(new ChaosEffect("CubeAddAnti", () =>
                {
                    GameState.SaveData.SecretCubes++;
                    GameState.SaveData.ScoreDirty = true;
                    GameState.OnHudElementChanged();
                    ServiceHelper.Get<IGomezService>().OnCollectedAnti();
                }, 1f, category: "Inventory.AntiCube"));
                ChaosEffectsList.Add(new ChaosEffect("CubeSubAnti", () =>
                {
                    GameState.SaveData.SecretCubes--;
                    GameState.SaveData.ScoreDirty = true;
                    GameState.OnHudElementChanged();
                }, 1f, category: "Inventory.AntiCube"));
                ChaosEffectsList.Add(new ChaosEffect("AddKey", () =>
                {
                    GameState.SaveData.Keys++;
                    GameState.OnHudElementChanged();
                }, 1f, category: "Inventory.Key"));
                ChaosEffectsList.Add(new ChaosEffect("SubKey", () =>
                {
                    GameState.SaveData.Keys--;
                    GameState.OnHudElementChanged();
                }, 1f, category: "Inventory.Key"));

                //ChaosEffectsList.Add(new ChaosEffect("Jetpack", () => { GameState.JetpackMode = true; }, 1f));
                //ChaosEffectsList.Add(new ChaosEffect("DebugModeEnable", () => { GameState.DebugMode = true; }, 1f));
                //ChaosEffectsList.Add(new ChaosEffect("DebugModeDisable", () => { GameState.DebugMode = false; }, 1f));

                ChaosEffectsList.Add(new ChaosEffect("SetGravityInverted", () => {
                    if(!CurrentLevelInfo.HasSinkBlocks)
                        CollisionManager.GravityFactor = PlayerManager.Action.IsEnteringDoor() ? 1f : -1.0f;//tertiary operator so the door puts Gomez on the top trile surface instead of the bottom
                }, 1f, ()=> !CurrentLevelInfo.HasSinkBlocks, duration: 3d, onDone: ResetGravity, category: "Gravity", pauseTimerTest: () => CurrentLevelInfo.HasSinkBlocks));
                //ChaosEffectsList.Add(new ChaosEffect("SetGravity1.0", () => { CollisionManager.GravityFactor = 1.0f; }, 1f, duration: 10d, category: "Gravity"));
                ChaosEffectsList.Add(new ChaosEffect("SetGravityMoon", () => { CollisionManager.GravityFactor = 0.165f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity"));//The gravity of Earth's moon, assuming 1f is equivalent to Earth's gravity
                ChaosEffectsList.Add(new ChaosEffect("SetGravityFezMoon", () => { CollisionManager.GravityFactor = 0.25f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity"));//The gravity of the game's moon level, "PYRAMID"
                ChaosEffectsList.Add(new ChaosEffect("SetGravity0.5", () => { CollisionManager.GravityFactor = 0.5f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity"));
                ChaosEffectsList.Add(new ChaosEffect("SetGravity1.5", () => { CollisionManager.GravityFactor = 1.5f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity"));
                ChaosEffectsList.Add(new ChaosEffect("SetGravity1.7", () => { CollisionManager.GravityFactor = 1.7f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity"));
                ChaosEffectsList.Add(new ChaosEffect("SetGravity1.9", () => { CollisionManager.GravityFactor = 1.9f; }, 1f, duration: 10d, onDone: ResetGravity, category: "Gravity"));
                //Note: setting the gravity to 2 or greater causes weird things to occur

                //ChaosEffectsList.Add(new ChaosEffect("SoundEffect_ResolvePuzzle", () => { LevelService.ResolvePuzzleSoundOnly(); }, 1f));

                ChaosEffectsList.Add(new ChaosEffect("SetSkyRandom", () =>
                {
                    Sky sky = Skies[random.Next(0, Skies.Length)];
                    ChaosModWindow.LogLine("Changed Sky: " + sky.Name);//TODO Changing from certain skies to certain other skies can cause the foreground to go black for some reason
                    LevelManager.ChangeSky(sky);
                    SkyChangerDelayTimer.Restart();
                }, 3f, () => SkyChangerDelayTimer.Elapsed.TotalSeconds > 2f, category: "Sky"));
                SkyChangerDelayTimer.Start();
                /*ChaosEffectsList.Add(new ChaosEffect("Blind", () =>
                {
                    Sky sky = BlackSky;
                    LevelManager.ChangeSky(sky);//Only seems to work when changing from certain skies
                }, 1f, ()=>CurrentLevelInfo.Sky.Name!="BLACK", duration: 5, onDone: ()=>LevelManager.ChangeSky(CurrentLevelInfo.Sky), category: "Sky"));*/
                ChaosEffectsList.Add(new ChaosEffect("Starfield", () =>
                {
                    Starfield.Opacity = 1;
                    Starfield.Draw();
                }, 5f, duration: 60, onDone: () => { Starfield.Opacity = 0; Starfield.Draw(); }, category: "Starfield"));
                ChaosEffectsList.Add(new ChaosEffect("PlayRandomMusic", () =>
                {
                    string songname = BGMusicNames[random.Next(0, BGMusicNames.Length)];
                    ChaosModWindow.LogLine("Changed Music: " + songname);
                    //TODO don't stop playing song beacause of level transitions; might have to forgo SoundManager
                    SM.PlayNewSong(songname);
                    SongStarterDelayTimer.Restart();
                }, 3f, () => SongStarterDelayTimer.Elapsed.TotalSeconds > 2f, category: "Music"));
                SongStarterDelayTimer.Start();

                //ChaosEffectsList.Add(new ChaosEffect("GoToIntro", () => { LevelManager.ChangeLevel("GOMEZ_HOUSE_2D"); }, 0.1f));
                /*ChaosEffectsList.Add(new ChaosEffect("ReloadLevel", ()=>
                {
                    if(LevelManager.LastDestinationVolumeId.HasValue)
                        LevelService.ChangeLevelToVolume(LevelManager.Name, (int)LevelManager.LastDestinationVolumeId, false, false, false);//can crash the game
                    else
                        LevelManager.ChangeLevel(LevelManager.Name);
                }, 1f, () => !(InCutsceneLevel) && TimeInLevelTimer.Elapsed.TotalSeconds > 5f, category: "Teleport"));*/
                ChaosEffectsList.Add(new ChaosEffect("GoToRandomLevel", () =>
                {
                    if (LevelNamesForRandTele.Count <= 0)
                    {
                        ChaosModWindow.LogLine("No levels found in the random teleport level list");
                        return;
                    }
                    string levelname = LevelNamesForRandTele[random.Next(0, LevelNamesForRandTele.Count)];
                    LevelManager.ChangeLevel(levelname);
                    //GC.Collect();
                }, 0.01f, () => !InCutsceneLevel && TimeInLevelTimer.Elapsed.TotalSeconds > 5f && !IsHurting && CurrentLevelInfo.Gravity > 0, category: "Teleport"));
                ChaosEffectsList.Add(new ChaosEffect("GoToRandomHubLevel", () =>
                {
                    string levelname = HubLevelNames[random.Next(0, HubLevelNames.Length)];
                    LevelManager.ChangeLevel(levelname);
                }, 0.1f, () => !InCutsceneLevel && TimeInLevelTimer.Elapsed.TotalSeconds > 5f && !IsHurting && CurrentLevelInfo.Gravity>0, category: "Teleport"));
                LevelManager.LevelChanged += TimeInLevelTimer.Restart;
                TimeInLevelTimer.Start();


                ChaosEffectsList.Add(new ChaosEffect("Glitches5", () =>
                {
                    EnsureGlitches();
                    Glitches.ActiveGlitches = 5;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.ActiveGlitches"));
                ChaosEffectsList.Add(new ChaosEffect("Glitches25", () =>
                {
                    EnsureGlitches();
                    Glitches.ActiveGlitches = 25;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.ActiveGlitches"));
                ChaosEffectsList.Add(new ChaosEffect("Glitches50", () =>
                {
                    EnsureGlitches();
                    Glitches.ActiveGlitches = 50;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.ActiveGlitches"));
                ChaosEffectsList.Add(new ChaosEffect("Glitches500", () =>
                {
                    EnsureGlitches();
                    Glitches.ActiveGlitches = 500;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.ActiveGlitches"));
                ChaosEffectsList.Add(new ChaosEffect("FreezeChance0.1", () =>
                {
                    EnsureGlitches();
                    Glitches.FreezeProbability = 0.1f;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.FreezeProbability"));
                ChaosEffectsList.Add(new ChaosEffect("FreezeChance0.2", () =>
                {
                    EnsureGlitches();
                    Glitches.FreezeProbability = 0.2f;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.FreezeProbability"));
                ChaosEffectsList.Add(new ChaosEffect("FreezeChance0.5", () =>
                {
                    EnsureGlitches();
                    Glitches.FreezeProbability = 0.5f;
                }, 1f, duration: 10, onDone: ResetGlitches, category: "Glitches.FreezeProbability"));
                ChaosEffectsList.Add(new ChaosEffect("FreezeChance1.0", () =>
                {
                    EnsureGlitches();
                    Glitches.FreezeProbability = 1f;
                }, 1f, duration: 3, onDone: ResetGlitches, category: "Glitches.FreezeProbability"));


                //TODO add effects that spawn things
                /*ChaosEffectsList.Add(new ChaosEffect("SpawnVase", () =>
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
                }, 5f, () => PlayerManager.Ground.First != null));*/


                //TODO add effects that mess with the controls


                #endregion

                LoadingText = "Initializing Chaos Mod settings window...";
                Thread thread = new Thread(() => (this.ChaosModWindow = new ChaosModWindow(this)).ShowDialog());
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                initBenchmarkTimer.Stop();
                DidInit = true;
                Timer.Start();
                _ = Waiters.Wait(() => ChaosModWindow != null && ChaosModWindow.Created && !ChaosModWindow.IsDisposed && ChaosModWindow.Visible, () =>
                {
                    LoadingText = null;
                    ChaosModWindow.LogLineDebug(WorldInfo.GetAllLevelDataAsString());

                    ChaosModWindow.LogLineDebug($"Skies: {{{String.Join(", ", SkiesNames)}}}");
                    ChaosModWindow.LogLineDebug($"Songs: {{{String.Join(", ", BGMusicNames)}}}");
                    ChaosModWindow.LogLineDebug($"Hubs: {{{String.Join(", ", HubLevelNames)}}}");

                    ChaosModWindow.LogLineDebug("{\"ScriptEntityTypeDescriptors\": " + ScriptDescriptor.ListAllScriptEntityTypeDescriptors() + "}");
                    ChaosModWindow.LogLineDebug("{\"ActionAnimations\": {" + String.Join(", ", Common.Util.GetValues<ActionType>().Where(a => a != 0).Select(a => $"\"{a}\": \"{a.GetAnimationPath()}\""))+ "}}");

                    ChaosModWindow.LogLineDebug($"Initialization duration: {initBenchmarkTimer.Elapsed.TotalSeconds}s ({initBenchmarkTimer.Elapsed.TotalMilliseconds}ms)");
                });
            }
        }
        /// <summary>
        /// An instance of an effect whose effect is active or the effect text is visible on the screen.
        /// </summary>
        public class ActiveChaosEffect // Needs to be a  class because if it's a struct then HasDoneOnDone will never be set to true
        {
            /// <summary>
            /// The <see cref="ChaosEffect"/> for this <see cref="ActiveChaosEffect"/>.
            /// </summary>
            private ChaosEffect Effect { get; }
            /// <summary>
            /// The timer that tracks how long the effect has been running. Will be set to <c>null</c> when the effect is done.
            /// </summary>
            private Stopwatch ActiveTimer { get; set; }
            /// <summary>
            /// The timer used to make the text blink when the effect is first displayed. 
            /// </summary>
            private Stopwatch BlinkerTimer { get; set; }
            /// <summary>
            /// Returns <c>true</c> if the effect has been active for longer than its duration. 
            /// </summary>
            public bool IsDone => ActiveTimer == null || ActiveTimer.Elapsed.TotalSeconds >= Effect.Duration;
            /// <summary>
            /// Tracks whether or not <see cref="OnDone"/> for this <see cref="ActiveChaosEffect"/> has been called.
            /// </summary>
            private bool HasDoneOnDone;
            /// <summary>
            /// A value from 0 to 1 representing the percentage completion of the effect.
            /// </summary>
            public double Progress => ActiveTimer != null ? ActiveTimer.Elapsed.TotalSeconds / Effect.Duration : 1;
            /// <summary>
            /// Determines whether the effect text should be drawn on screen.
            /// </summary>
            public bool Hidden;
            /// <summary>
            /// The <see cref="ChaosEffect.Name"/> of the unterlaying <see cref="ChaosEffect"/>.
            /// </summary>
            public string Name => Effect.Name;
            /// <summary>
            /// Constructs a new <see cref="ActiveChaosEffect"/> from the provided <see cref="ChaosEffect"/>.
            /// </summary>
            /// <param name="effect">The <see cref="ChaosEffect"/></param>
            public ActiveChaosEffect(ChaosEffect effect)
            {
                Effect = effect;
                HasDoneOnDone = false;
                ActiveTimer = Stopwatch.StartNew();
                BlinkerTimer = Stopwatch.StartNew();
                Hidden = false;
            }
            /// <summary>
            /// If this ActiveChaosEffect is not <see cref="Hidden">Hidden</see>, draws the effect text to the screen at the specified index, otherwise does nothing. 
            /// </summary>
            /// <param name="index">The index to draw the effect text.</param>
            public void Draw(int index)
            {
                if (!Hidden)
                {
                    double blinkTime = BlinkerTimer.Elapsed.TotalSeconds;
                    //Blink the text for .5 seconds every .5 seconds for the first 3 seconds
                    bool ShouldBlink = blinkTime < 3 && blinkTime % 1 < .5;
                    double timeLeft = Effect.Duration - (ActiveTimer != null ? ActiveTimer.Elapsed.TotalSeconds : 0);
                    string Text = timeLeft < 60 ? Math.Ceiling(timeLeft).ToString() : null;//Only show the time if it has less than 60 seconds remaining
                    Color color = Hidden ? Color.Transparent : (ShouldBlink ? EffectTextColorBlink : (Progress >= 1 ? EffectTextColorDone : EffectTextColorActive));
                    ChaosModEffectTextDrawer.Draw(Effect.Name, Progress, index, Text, color, ActiveTimer!=null && !ActiveTimer.IsRunning);
                }
            }
            /// <summary>
            /// If this <see cref="ActiveChaosEffect"/>'s <see cref="OnDone">OnDone</see> method has not been called, call the underlaying <see cref="ChaosEffect.Func"/> method.
            /// <br/><br/>
            /// <inheritdoc cref="ChaosEffect.Func"/>
            /// </summary>
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
            /// <summary>
            /// The Category of the underlaying Effect.<br/><br/>
            /// <inheritdoc cref="ChaosEffect.Category"/>
            /// </summary>
            public string Category => Effect.Category;
            /// <summary>
            /// <inheritdoc cref="ChaosEffect.ShouldPauseTimer"/>
            /// </summary>
            public bool ShouldPauseTimer => Effect.ShouldPauseTimer;
            /// <summary>
            /// If this is the first time calling this function for this ActiveChaosEffect instance, calls the <see cref="ChaosEffect"/>'s <see cref="ChaosEffect.OnDone">OnDone</see>
            /// </summary>
            public void OnDone()
            {
                if (!HasDoneOnDone)
                {
                    HasDoneOnDone = true;
                    Effect.OnDone?.Invoke();
                    ActiveTimer = null;
                    //ActiveTimer.Stop();
                    FezChaosMod.Instance.ChaosEffectEnded(this);
                }
            }

            /// <summary>
            /// Pauses the <see cref="ActiveTimer">ActiveTimer</see> if it exists.
            /// </summary>
            public void Pause() { ActiveTimer?.Stop(); }
            /// <summary>
            /// Unpauses the <see cref="ActiveTimer">ActiveTimer</see> if it exists.
            /// </summary>
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
            if(this.disposing)
            {
                return;
            }

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
                        effect.Activate();
                        break;
                    }
                }
            }
            if (!DidInit)
            {
                LoadingText = "Initializing Chaos Mod...";
            }

            if (Enabled && DidInit)
            {
                //draw the big countdown timer bar thing
                double timeLeft = DelayBetweenEffects - elapsedtime;
                if (timeLeft < 0)
                    timeLeft = 0;
                string Text = Math.Ceiling(timeLeft).ToString();
                ChaosModNextEffectCountDownProgressBar.DrawProgressBar(
                    elapsedtime / DelayBetweenEffects,
                    Text,
                    new Rectangle(0, 0, viewport.Width, (int)Math.Ceiling(DrawingTools.Instance.MeasureString("0").Y * scale) + 4),
                    scale, ChaosTimerPaused);

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
                        if (item.IsDone && index < activeChaosEffects.Count-LatestEffectsToDisplay)
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

                //draw the active effect text
                index = Math.Min(MaxActiveEffectsToDisplay, activeChaosEffects.Count) - 1;
                for (int i = 0; i < activeChaosEffects.Count; ++i)
                {
                    var activeEffect = activeChaosEffects[i];
                    activeEffect.Hidden = i < UnlistedActiveEffects;
                    activeEffect.Draw(index);
                    if (!activeEffect.Hidden)
                        index--;
                }
            }

            if (LoadingText != null)
            {
                var vp = ServiceHelper.Get<IGraphicsDeviceService>().GraphicsDevice.Viewport;
                drawingTools.DrawShadowedText(LoadingText, InitializingChaosModSettingsWindowWaitingTextColor, new Vector2(vp.Width * .01f, vp.Height * .9f), scale);
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
                   $"Gomez Texture: {(PlayerManager.Action != ActionType.None ? PlayerManager.Action.GetAnimationPath() : null)}\n" +
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
            //Note: ExactUp is used for doors and whatnot
            s += InputManager.Up != 0 || InputManager.ExactUp != 0 ? "u " : "  ";
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

            return s;
        }
    }
}