using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Structure.Scripting;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FezGame.GameInfo
{
    public struct LootMetadata
    {
        public ActorType ActorType { get; }
        public Vector3 Location { get; }
        public string SpawnNameType { get; }
        public string LevelName { get; }
        public ArtObjectInstance Container { get; }
        public CodeInput[] CodePattern { get; }
        public TrileInstance ContainingTrile { get; }
        public string MapName { get; }
        public List<ScriptCondition> Conditions { get; }

        internal LootMetadata(ActorType actorType, Vector3 location, string spawnNameType, string levelName, ArtObjectInstance Container = null, CodeInput[] codePattern = null, TrileInstance ContainingTrile = null, string mapName = null, List<ScriptCondition> conditions = null)
        {
            ActorType = actorType;
            Location = location;
            SpawnNameType = spawnNameType;
            LevelName = levelName;
            this.Container = Container;
            CodePattern = codePattern;
            this.ContainingTrile = ContainingTrile;
            if (Container != null && ContainingTrile != null)
            {
                //Cannot have both a ContainingTrile and a Container
                System.Diagnostics.Debugger.Break();
                throw new ArgumentException("Cannot have both a ContainingTrile and a Container!");
            }
            MapName = mapName;
            Conditions = conditions;
        }
        public override string ToString()
        {
            var str = $"{{\"Type\": \"{this.GetType().GetFormattedName()}\", \"ActorType\": \"{ActorType}\"";
                str += $", \"Location\": {Location.ToJsonString()}";
                str += $", \"SpawnNameType\": \"{SpawnNameType}\"";
                str += $", \"LevelName\": \"{LevelName}\"";
            if (Container != null)
                str += $", \"ContainerActorType\": \"{Container.ArtObject.ActorType}\"";
            if (CodePattern != null && CodePattern.Length > 0)
                str += $", \"CodePattern\": [{String.Join(", ", CodePattern.Select(a => $"\"{a}\""))}]";
            if (ContainingTrile != null)
                str += $", \"ContainingTrileName\": \"{LevelInfo.GetTrile(LevelName, ContainingTrile).Name}\"";
            if (MapName != null)
                str += $", \"MapName\": \"{MapName}\"";
            if (Conditions != null && Conditions.Count > 0)
                str += $", \"Conditions\": [{String.Join(", ", Conditions.Select(c => $"\"{c}\""))}]";
            str += "}";
            return str;
        }
    }
    public class LootMetadataList : List<LootMetadata>
    {
        internal void AddContained(ArtObjectInstance ao, ActorType actorType, string levelName, CodeInput[] codePattern, List<ScriptCondition> conditions = null)
        {
            Add(new LootMetadata(actorType, ao.Position, ao.ArtObjectName, levelName, Container: ao, codePattern: codePattern, conditions: conditions));
        }
        internal void AddMap(ArtObjectInstance ao, string levelName, string mapName)
        {
            Add(new LootMetadata(ActorType.TreasureMap, ao.Position, ao.ArtObjectName, levelName, Container: ao, mapName: mapName));
        }
        internal void AddFork(ArtObjectInstance ao, string levelName)
        {
            List<CodeInput> codeInputs = new List<CodeInput>();
            // - Vector3.One / 2f
            foreach(var vibrator in ao.ActorSettings.VibrationPattern)
            {
                switch(vibrator)
                {
                case VibrationMotor.LeftLow:
                    codeInputs.Add(CodeInput.SpinLeft);
                    break;
                case VibrationMotor.RightHigh:
                    codeInputs.Add(CodeInput.SpinRight);
                    break;
                }
            }
            Add(new LootMetadata(ActorType.SecretCube, ao.Position, ao.ArtObjectName, levelName, Container: ao, codePattern: codeInputs.ToArray()));
        }
        internal void AddTrile(TrileEmplacement emplacement, ActorType actorType, string levelName, TrileInstance ContainingTrile)
        {
            Add(new LootMetadata(actorType, emplacement.AsVector, "Trile", levelName, ContainingTrile: ContainingTrile));
        }
        private void AddGlitchyDespawner(ActorType actorType, Vector3 Position, string spawnNameType, string levelName, CodeInput[] codePattern, List<ScriptCondition> conditions)
        {
            Add(new LootMetadata(actorType, Position - Vector3.One / 2f, spawnNameType, levelName, codePattern: codePattern, conditions: conditions));
        }
        internal void AddCode(Entity ent, ActorType actorType, Level level, CodeInput[] codePattern, List<ScriptCondition> conditions)
        {
            switch (ent.Type)
            {
            case "ArtObject":
                AddContained(level.ArtObjects[(int)ent.Identifier], actorType, level.Name, codePattern, conditions);
                return;
            case "Volume":
                AddGlitchyDespawner(actorType, level.Volumes[(int)ent.Identifier].BoundingBox.Center(), "Volume", level.Name, codePattern, conditions);
                return;
            }
        }
        internal void AddHardcoded(ActorType actorType, Vector3 vector3, string SpawnTypeName, string levelName)
        {
            Add(new LootMetadata(actorType, vector3, SpawnTypeName, levelName));
        }
        internal void AddOwl(NpcInstance npc, string levelName)
        {
            Add(new LootMetadata(npc.ActorType, npc.Position, "Npc", levelName));
        }
        public override string ToString()
        {
            return $"{{\"Type\": \"{this.GetType().GetFormattedName()}\", \"Count\": {Count}, \"Values\": [{String.Join(", ", this)}]}}";
        }
    }
}