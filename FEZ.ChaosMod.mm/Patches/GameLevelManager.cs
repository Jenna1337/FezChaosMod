#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using Microsoft.Xna.Framework;
using System.Reflection;
using Common;
using FezEngine.Tools;
using FezGame.Components;
using FezEngine.Components;
using FezGame.ChaosMod;
using static FezGame.GameInfo.LevelInfo;
using FezGame.Randomizer;
using FezGame.GameInfo;
using FezEngine.Structure;

namespace FezGame.Services {
    class patch_GameLevelManager : GameLevelManager {
		[ServiceDependency]
		public new IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public new IGameCameraManager CameraManager { private get; set; }

		public patch_GameLevelManager(Game game)
			: base(game)
		{
		}

		public extern void orig_ChangeLevel(string levelName);
		protected new void ChangeLevel(string levelName)
        {
			LevelTarget newLevelTarget;
			bool newLevelTargetDidInit = false;
			ChaosModWindow.LogLine("Changing Level from " + base.Name + " to " + levelName);
			if (FezRandomizer.RoomRandoMode)
			{
				if (FezRandomizer.HasConnection(base.Name, levelName, PlayerManager.DoorVolume, DestinationVolumeId))
				{
					newLevelTarget = FezRandomizer.GetLevelTargetReplacement(base.Name, levelName, PlayerManager.DoorVolume, DestinationVolumeId);
					levelName = newLevelTarget.TargetLevelName;
					DestinationVolumeId = newLevelTarget.TargetVolumeId;
					ChaosModWindow.LogLine("Hijacked Level load; now to " + newLevelTarget.TargetLevelName);
					newLevelTargetDidInit = true;
				}
			}
			orig_ChangeLevel(levelName);
			if (FezRandomizer.RoomRandoMode && newLevelTargetDidInit)
			{
				if (PlayerManager.DoorVolume.HasValue && levelData.Volumes.TryGetValue(PlayerManager.DoorVolume.Value, out Volume volume))
				{
					/*Volume volume = Volumes[PlayerManager.DoorVolume.Value];
					Vector3 vector = (volume.From + volume.To) / 2f;
					PlayerManager.Position = PlayerManager.Position * (Vector3.UnitY + base.CameraManager.Viewpoint.DepthMask()) + vector * base.CameraManager.Viewpoint.SideMask();*/
				}

				//TODO orient the camera in the correct direction?; changed some stuff here but have yet to test it
				CameraManager.RebuildView();
				CameraManager.ResetViewpoints();
				var doors = LevelInfo.GetLevelInfo(levelName).Entrances.Where(a => a.VolumeId == DestinationVolumeId);
				if (doors.Any())
				{
					CameraManager.ChangeViewpoint(doors.Single().Exit.AsEntrance().FromOrientation.Value.AsViewpoint());
				}
				DestinationVolumeId = null;
			}
		}

	}
}
