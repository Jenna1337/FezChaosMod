using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FezGame.ChaosMod
{
    internal class GlobalGlitchesManager : DrawableGameComponent
    {
        public bool IsFrozen => freezeForFrames > 0;

        private RenderTargetHandle FreezeRth;
        private Mesh GlitchMesh;
        private SoundEffect[] sGlitches;
        private SoundEffect[] sTimestretches;
        private SoundEmitter eTimeStretch;
        private readonly List<SoundEmitter> eGlitches = new List<SoundEmitter>();
        private float freezeForFrames;
        private float disappearRF;
        private float resetRF;

        private Random random;
        private ShaderInstancedIndexedPrimitives<VertexPositionTextureInstance, Matrix> Geometry;


        private readonly DrawableGameComponent glitches;
        private static Type NesGlitchesType;


        [ServiceDependency]
        public ISoundManager SoundManager { private get; set; }

        [ServiceDependency]
        public IContentManagerProvider CMProvider { private get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { private get; set; }

        [ServiceDependency]
        public IDefaultCameraManager CameraManager { private get; set; }

        [ServiceDependency]
        public ITargetRenderingManager TargetRenderingManager { private get; set; }


        public GlobalGlitchesManager(Game game) : base(game)
        {
            NesGlitchesType = typeof(FezGame.Fez).Assembly.GetType("FezGame.Components.NesGlitches");
            glitches = (DrawableGameComponent)NesGlitchesType.GetConstructor(new[] { typeof(Game) }).Invoke(new[] { game });
            ServiceHelper.AddComponent(glitches);
            ServiceHelper.InjectServices(this);

            this.DrawOrder = glitches.DrawOrder + 1;

            //Note: If these are not loaded on the Global CMProvider, the game gets very cursed 
            (NesGlitchesType.GetField("GlitchMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(glitches) as Mesh)
                .Texture = CMProvider.Global.Load<Texture2D>("Other Textures/glitches/glitch_atlas");
            NesGlitchesType.GetField("sGlitches", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(glitches,
                (from x in CMProvider.GetAllIn("Sounds/Intro\\Elders\\Glitches")
                 select CMProvider.Global.Load<SoundEffect>(x)).ToArray());
            NesGlitchesType.GetField("sTimestretches", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(glitches,
                (from x in CMProvider.GetAllIn("Sounds/Intro\\Elders\\Timestretches")
                 select CMProvider.Global.Load<SoundEffect>(x)).ToArray());
        }

        public override void Initialize()
        {
            base.Initialize();
            FreezeRth = TargetRenderingManager.TakeTarget();
            GlitchMesh = new Mesh
            {
                Effect = new GlitchyPostEffect(),
                AlwaysOnTop = true,
                DepthWrites = false,
                Blending = BlendingMode.Opaque,
                Culling = CullMode.None,
                SamplerState = SamplerState.PointWrap,
                Texture = CMProvider.Global.Load<Texture2D>("Other Textures/glitches/glitch_atlas")
            };
            GlitchMesh.AddGroup().Geometry = Geometry = new ShaderInstancedIndexedPrimitives<VertexPositionTextureInstance, Matrix>(PrimitiveType.TriangleList, 60);
            Geometry.Vertices = new VertexPositionTextureInstance[4]
            {
                new VertexPositionTextureInstance(Vector3.Zero, Vector2.Zero),
                new VertexPositionTextureInstance(new Vector3(0f, 1f, 0f), new Vector2(0f, 1f)),
                new VertexPositionTextureInstance(new Vector3(1f, 1f, 0f), new Vector2(1f, 1f)),
                new VertexPositionTextureInstance(new Vector3(1f, 0f, 0f), new Vector2(1f, 0f))
            };
            Geometry.Indices = new int[6] { 0, 1, 2, 0, 2, 3 };
            random = RandomHelper.Random;
            Geometry.Instances = new Matrix[1000];
            for (int i = 0; i < 1000; i++)
            {
                float num = random.Next(1, 5);
                float num2 = random.Next(1, 3);
                bool flag = 0.75 > random.NextDouble();
                Geometry.Instances[i] = new Matrix(RandomHelper.Random.Next(-8, 54), RandomHelper.Random.Next(-8, 30), flag ? num : num2, flag ? num2 : num, (random.Next(0, 3) == 0) ? 1 : 0, (random.Next(0, 3) == 0) ? 1 : 0, (random.Next(0, 3) == 0) ? 1 : 0, random.Next(0, 2), (random.Next(0, 3) == 0) ? 1 : 0, (random.Next(0, 3) == 0) ? 1 : 0, (random.Next(0, 3) == 0) ? 1 : 0, 0f, 0f, 0f, 0f, 0f);
                Geometry.InstancesDirty = true;
            }
            Geometry.MaximizeBuffers(1000);
            sGlitches = (from x in CMProvider.GetAllIn("Sounds/Intro\\Elders\\Glitches")
                         select CMProvider.Global.Load<SoundEffect>(x)).ToArray();
            sTimestretches = (from x in CMProvider.GetAllIn("Sounds/Intro\\Elders\\Timestretches")
                              select CMProvider.Global.Load<SoundEffect>(x)).ToArray();
        }

        public float FreezeProbability { get; set; }

        public float DisappearProbability { get; set; }

        public int ActiveGlitches { get; set; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            TargetRenderingManager.ReturnTarget(FreezeRth);
            ServiceHelper.RemoveComponent(glitches);
        }

        public override void Update(GameTime gameTime)
        {
            if (GameState.Paused || GameState.Loading)
            {
                return;
            }
            if (RandomHelper.Probability(0.05))
            {
                resetRF = Easing.EaseIn(random.NextDouble(), EasingType.Sine) * 10f;
            }
            ActiveGlitches = Math.Min(ActiveGlitches, 1000);
            Geometry.InstanceCount = ActiveGlitches;
            for (int i = 0; i < Geometry.InstanceCount; i++)
            {
                Matrix matrix = Geometry.Instances[i];
                if (matrix.M34 != 0f)
                {
                    matrix.M43 -= 1f;
                    if (!(matrix.M43 <= 0f))
                    {
                        Geometry.Instances[i].M43 = matrix.M43;
                        continue;
                    }
                    matrix.M34 = 0f;
                    EmitFor(matrix.M11, matrix.M13);
                }
                else if ((double)(DisappearProbability * disappearRF) >= random.NextDouble())
                {
                    matrix.M34 = 1f;
                    matrix.M43 = random.Next(1, 30);
                }
                if (0.05 * (double)resetRF >= random.NextDouble())
                {
                    matrix.M21 = (random.Next(0, 3) == 0) ? 1 : 0;
                    matrix.M22 = (random.Next(0, 3) == 0) ? 1 : 0;
                    matrix.M23 = (random.Next(0, 3) == 0) ? 1 : 0;
                    matrix.M24 = (random.Next(0, 2) == 0) ? 1 : 0;
                    matrix.M31 = (random.Next(0, 3) == 0) ? 1 : 0;
                    matrix.M32 = (random.Next(0, 3) == 0) ? 1 : 0;
                    matrix.M33 = (random.Next(0, 3) == 0) ? 1 : 0;
                }
                if (0.075 * (double)resetRF >= random.NextDouble())
                {
                    Vector2 value = new Vector2(matrix.M41, matrix.M42) * 32f;
                    value += new Vector2(RandomHelper.Random.Next(-2, 3), RandomHelper.Random.Next(-1, 2));
                    value = Vector2.Clamp(value, Vector2.Zero, new Vector2(32f));
                    matrix.M41 = value.X / 32f;
                    matrix.M42 = value.Y / 32f;
                }
                if (0.075 * (double)resetRF >= random.NextDouble())
                {
                    bool flag = matrix.M13 < matrix.M14;
                    matrix.M13 += random.Next(-1, 2);
                    matrix.M14 += random.Next(-1, 2);
                    matrix.M13 = MathHelper.Clamp(matrix.M13, 1f, flag ? 2 : 4);
                    matrix.M14 = MathHelper.Clamp(matrix.M14, 1f, flag ? 4 : 2);
                    if (RandomHelper.Probability(0.75))
                    {
                        matrix.M11 += RandomHelper.Random.Next(-1, 2);
                        matrix.M12 += RandomHelper.Random.Next(-1, 2);
                        matrix.M11 = MathHelper.Clamp(matrix.M11, -8f, 54f);
                        matrix.M12 = MathHelper.Clamp(matrix.M12, -8f, 30f);
                    }
                }
                if (0.015 * (double)resetRF >= random.NextDouble())
                {
                    matrix.M11 = RandomHelper.Random.Next(-8, 54);
                    matrix.M12 = RandomHelper.Random.Next(-8, 30);
                    EmitFor(matrix.M11, matrix.M13);
                }
                Geometry.Instances[i] = matrix;
                Geometry.InstancesDirty = true;
            }
            for (int num = eGlitches.Count - 1; num >= 0; num--)
            {
                if (eGlitches[num].Dead)
                {
                    eGlitches.RemoveAt(num);
                }
            }
        }
        private void EmitFor(float xp, float xs)
        {
            if (!(freezeForFrames > 0f) && RandomHelper.Probability(1f / (float)Math.Sqrt(ActiveGlitches)))
            {
                SoundEmitter soundEmitter = sGlitches[random.Next(0, sGlitches.Length)].Emit((float)random.NextDouble() * 0.5f - 0.25f);
                soundEmitter.Pan = MathHelper.Clamp((xp + xs / 2f) / 27f - 1f, -1f, 1f);
                eGlitches.Add(soundEmitter);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (GameState.Paused || GameState.Loading)
            {
                return;
            }
            if (RandomHelper.Probability(0.1))
            {
                disappearRF = (float)Math.Pow(RandomHelper.Unit(), 2.0) * 10f;
            }
            GlitchMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Forward, 90);
            if (TargetRenderingManager.IsHooked(FreezeRth.Target))
            {
                GlitchMesh.Draw();
                TargetRenderingManager.Resolve(FreezeRth.Target, reschedule: false);
                GameState.SkipRendering = true;
                foreach (SoundEmitter eGlitch in eGlitches)
                {
                    if (!eGlitch.Dead)
                    {
                        eGlitch.FadeOutAndDie(0f);
                    }
                }
                eGlitches.Clear();
                SoundManager.Pause();
                eTimeStretch = RandomHelper.InList(sTimestretches).Emit(loop: true);
            }
            if (freezeForFrames > 0f)
            {
                base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                base.GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);
                TargetRenderingManager.DrawFullscreen(FreezeRth.Target);
                if (FreezeProbability != 1f && (freezeForFrames -= 1f) == 0f)
                {
                    GameState.SkipRendering = false;
                    eTimeStretch.FadeOutAndDie(0f);
                    SoundManager.Resume();
                    eTimeStretch = null;
                }
                base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
            }
            else
            {
                GlitchMesh.Draw();
                if (RandomHelper.Probability(FreezeProbability))
                {
                    TargetRenderingManager.ScheduleHook(base.DrawOrder, FreezeRth.Target);
                    freezeForFrames = RandomHelper.Random.Next(1, 30);
                }
            }
        }
    }
}
