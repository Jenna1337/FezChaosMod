using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FezGame.Randomizer
{
    static class BoundingBoxExtensions
    {
        public static Vector3 Center(this BoundingBox boundingBox)
        {
            return (boundingBox.Min + boundingBox.Max) / 2f;
        }
    }
}
