using System;
using System.Collections.Generic;
using static FezGame.Randomizer.LevelInfo;
using static FezGame.Randomizer.WorldInfo;

namespace FezGame.Randomizer
{
    class LevelConnectionList : List<LevelConnection>
    {
        public LevelConnectionList() : base()
        {
        }
        public LevelConnectionList(IEnumerable<LevelConnection> collection) : base(collection)
        {
        }
        public bool HasConnection(string fromLevel, int? fromVolume, string toLevel, int? toVolume)
        {
            return this.IndexOf(fromLevel, fromVolume, toLevel, toVolume) >= 0;
        }
        public bool HasConnection(Entrance fromLevel, Entrance toLevel)
        {
            return this.HasConnection(fromLevel.LevelName, fromLevel.VolumeId, toLevel.LevelName, toLevel.VolumeId);
        }
        public int IndexOf(string fromLevel, int? fromVolume, string toLevel, int? toVolume)
        {
            return this.FindIndex(c =>
            {
                bool b = (c.FromLevel.LevelName!=null && c.FromLevel.LevelName.Equals(fromLevel))
                    && (c.ToLevel.LevelName!=null && c.ToLevel.LevelName.Equals(toLevel))
                    && (c.FromLevel.VolumeId != null && c.FromLevel.VolumeId.Equals(fromVolume))
                    && (c.FromLevel.VolumeId != null && c.FromLevel.VolumeId.Equals(toVolume));
                return b;
            });
        }

        public LevelConnectionList Unique()
        {
            LevelConnectionList r = new LevelConnectionList();

            foreach (var i in this)
                if (r.FindIndex(a => a.FromLevel.Equals(i.ToLevel) && a.ToLevel.Equals(i.FromLevel)) < 0)
                    r.Add(i);

            return r;
        }
    }
}
