using System;
using System.Collections.Generic;
using System.Linq;
using static FezGame.GameInfo.WorldInfo;
using static FezGame.GameInfo.LevelInfo;
using FezGame.GameInfo;

namespace FezGame.Randomizer
{
    public static class IEnumerableExtentions
    {
        public static int[] FindAllIndexof<T>(this IEnumerable<T> values, T val)
        {
            return values.Select((b, i) => object.Equals(b, val) ? i : -1).Where(i => i != -1).ToArray();
        }
    }
    internal class BidirectionalLevelConnectionMap : IEnumerable<LevelConnection>
    {
        private readonly List<Entrance> _forward = new List<Entrance>();
        private readonly List<Entrance> _reverse = new List<Entrance>();

        public int Count { get => _forward.Count; }

        public BidirectionalLevelConnectionMap()
        {
        }

        public BidirectionalLevelConnectionMap(IEnumerable<WorldInfo.LevelConnection> collection)
        {
            foreach (var connection in collection)
                if (!HasConnection(connection))
                    Add(connection.FromLevel, connection.ToLevel);
        }

        public bool HasConnection(LevelConnection connection)
        {
            var a = _forward.FindAllIndexof(connection.FromLevel).Intersect(_reverse.FindAllIndexof(connection.ToLevel));
            var b = _reverse.FindAllIndexof(connection.FromLevel).Intersect(_forward.FindAllIndexof(connection.ToLevel));
            var aa = a.ToArray();
            var bb = b.ToArray();
            return a.Count() > 0 || b.Count() > 0;
        }
        internal LevelInfo.LevelTarget GetLevelTargetReplacement(string fromLevel, string toLevel, int? fromVolume, int? toVolume)
        {
            Entrance fromWhere = LevelInfo.GetLevelInfo(toLevel).Entrances.Where(a => a.Exit.TargetLevelName.Equals(fromLevel) && a.Exit.TargetVolumeId == fromVolume).Single();
            int index = _forward.IndexOf(fromWhere);
            if (index < 0)
                return _forward[_reverse.IndexOf(fromWhere)].Exit;
            return _reverse[index].Exit;
        }
        public void Add(Entrance t1, Entrance t2)
        {
            _forward.Add(t1);
            _reverse.Add(t2);
        }

        internal void Shuffle(Random rng)
        {
            //TODO fix this so it doesn't leave a bunch of levels with one door disconnected from other levels
            for (int i = this.Count - 1; i > 0; i--)
            {
                int swapIndex = rng.Next(i + 1);
                var tmp = this._forward[i];
                this._forward[i] = this._forward[swapIndex];
                this._forward[swapIndex] = tmp;
            }
        }

        public IEnumerator<LevelConnection> GetEnumerator()
        {
            return new BidirectionalLevelConnectionMapEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal bool HasConnection(string fromLevel, string toLevel, int? fromVolume, int? toVolume)
        {
            if (fromLevel == null || fromLevel.Length <= 0)
                return false;
            var test = LevelInfo.GetLevelInfo(fromLevel).Entrances.Where(a => a.LevelName.Equals(toLevel) && a.VolumeId == toVolume);
            if (!test.Any())
                return false;
            var fromWhere = test.Single();
            return _forward.IndexOf(fromWhere) >= 0 || _reverse.IndexOf(fromWhere) >= 0;
        }
        private LevelConnection GetAt(int i, bool isForward)
        {
            return isForward ? new LevelConnection(_forward[i], _reverse[i]) : new LevelConnection(_reverse[i], _forward[i]);
        }
        private class BidirectionalLevelConnectionMapEnumerator : IEnumerator<LevelConnection>
        {
            private int curIndex = -1;
            private bool isForward = true;
            private readonly BidirectionalLevelConnectionMap _collection;
            private LevelConnection curBox;

            public BidirectionalLevelConnectionMapEnumerator(BidirectionalLevelConnectionMap bidirectionalLevelConnectionMap)
            {
                this._collection = bidirectionalLevelConnectionMap;
            }
            // Enumerators are positioned before the first element
            // until the first MoveNext() call.


            public bool MoveNext()
            {
                //Avoids going beyond the end of the collection.
                if (++curIndex >= _collection.Count)
                {
                    if (isForward)
                    {
                        curIndex = -1;
                        isForward = false;
                        return MoveNext();
                    }
                    return false;
                }
                else
                {
                    // Set current box to next item in collection.
                    curBox = _collection.GetAt(curIndex, isForward);
                }
                return true;
            }

            public void Reset() { curIndex = -1; isForward = true; }

            void IDisposable.Dispose() { }

            public LevelConnection Current
            {
                get { return curBox; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}