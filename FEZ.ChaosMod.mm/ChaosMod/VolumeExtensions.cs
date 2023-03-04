using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace FezGame.ChaosMod
{
    public static class VolumeExtensions
    {
        //Adapted from private bool FezGame.Components.VolumesHost.TestObstruction(TrileInstance trile, float hitDistance, Vector3 hitStart, bool isBlackHole)
        private static bool TestObstruction(Vector3 vector, TrileInstance trile, float hitDistance, Vector3 hitStart, bool isBlackHole)
        {
            if (trile != null && trile.Enabled && !trile.Trile.Immaterial && (trile.Trile.ActorSettings.Type != ActorType.Hole || isBlackHole))
            {
                return (trile.Emplacement.AsVector + Vector3.One / 2f + vector * -0.5f - hitStart).Dot(vector) <= hitDistance + 0.25f;
            }
            return false;
        }
        //Adapted from void FezGame.Components.VolumesHost.TestVolumes(bool force)
        public static bool IsContainedInCurrentViewpoint(this Volume volume, Vector3 vector3)
        {
            var CameraManager = ServiceHelper.Get<IGameCameraManager>();
            var LevelManager = ServiceHelper.Get<IGameLevelManager>();

            Vector3 mask = CameraManager.Viewpoint.VisibleAxis().GetMask();
            Vector3 vector = CameraManager.Viewpoint.ForwardVector();
            /*if (PlayerManager.Background)
            {
                vector *= -1f;
            }*/
            Ray ray = new Ray
            {
                Position = vector3 * (Vector3.One - mask) - vector * LevelManager.Size,
                Direction = vector
            };
            /*if (PlayerManager.Action == ActionType.PullUpBack || PlayerManager.Action == ActionType.PullUpFront || PlayerManager.Action == ActionType.PullUpCornerLedge)
            {
                ray2.Position += new Vector3(0f, 0.5f, 0f);
            }*/
            float? num = volume.BoundingBox.Intersects(ray);
            if (volume.ActorSettings != null && volume.ActorSettings.IsBlackHole)
            {
                if (!num.HasValue)
                {
                    num = volume.BoundingBox.Intersects(new Ray(ray.Position + new Vector3(0f, 0.3f, 0f), ray.Direction));
                }
                if (!num.HasValue)
                {
                    num = volume.BoundingBox.Intersects(new Ray(ray.Position - new Vector3(0f, 0.3f, 0f), ray.Direction));
                }
            }
            if (num.HasValue)
            {
                bool flag2 = false;
                bool isBlackHole = volume.ActorSettings != null && volume.ActorSettings.IsBlackHole;
                NearestTriles nearestTriles = LevelManager.NearestTrile(ray.Position, /*PlayerManager.Background ? QueryOptions.Background :*/ QueryOptions.None);
                if (LevelManager.Name != "PIVOT_TWO" && nearestTriles.Surface != null)
                {
                    flag2 |= TestObstruction(vector, nearestTriles.Surface, num.Value, ray.Position, isBlackHole);
                }
                if (nearestTriles.Deep != null)
                {
                    flag2 |= TestObstruction(vector, nearestTriles.Deep, num.Value, ray.Position, isBlackHole);
                }
                if (!flag2 && ((volume.ActorSettings != null && volume.ActorSettings.IsBlackHole) || volume.Orientations.Contains(CameraManager.VisibleOrientation)))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsContainedInViewpoint(this Volume volume, Vector3 vector3, FezEngine.Viewpoint viewpoint, Level level)
        {
            Vector3 mask = viewpoint.VisibleAxis().GetMask();
            Vector3 vector = viewpoint.ForwardVector();
            Ray ray = new Ray
            {
                Position = vector3 * (Vector3.One - mask) - vector * level.Size,
                Direction = vector
            };
            float? num = volume.BoundingBox.Intersects(ray);
            if (volume.ActorSettings != null && volume.ActorSettings.IsBlackHole)
            {
                if (!num.HasValue)
                {
                    num = volume.BoundingBox.Intersects(new Ray(ray.Position + new Vector3(0f, 0.3f, 0f), ray.Direction));
                }
                if (!num.HasValue)
                {
                    num = volume.BoundingBox.Intersects(new Ray(ray.Position - new Vector3(0f, 0.3f, 0f), ray.Direction));
                }
            }
            if (num.HasValue)
            {
                if ((volume.ActorSettings != null && volume.ActorSettings.IsBlackHole) || volume.Orientations.Contains(viewpoint.VisibleOrientation()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
