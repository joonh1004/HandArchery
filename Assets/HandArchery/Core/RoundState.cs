using System;
using System.Collections.Generic;
namespace HandArchery.Core
{
    public sealed class RoundState
    {
        private readonly HashSet<int> inFlight = new HashSet<int>();
        private int lastShotId;
        public RoundState(int capacity = 5)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            RemainingShots = capacity;
        }
        public int Capacity { get; }
        public int RemainingShots { get; private set; }
        public int TotalScore { get; private set; }
        public int ShotsInFlight { get { return inFlight.Count; } }
        public bool IsFinished { get { return RemainingShots == 0 && inFlight.Count == 0; } }
        public bool TryBeginShot(out int shotId)
        {
            shotId = 0;
            if (RemainingShots == 0 || lastShotId == int.MaxValue) return false;
            shotId = ++lastShotId;
            inFlight.Add(shotId);
            RemainingShots--;
            return true;
        }
        public bool ResolveShot(int shotId, int score)
        {
            if (score < 0 || score > 10 || !inFlight.Contains(shotId) || TotalScore > int.MaxValue - score) return false;
            inFlight.Remove(shotId);
            TotalScore += score;
            return true;
        }
        public void Reset()
        {
            // Keep the lifetime id counter so collisions from the prior round stay invalid.
            inFlight.Clear();
            RemainingShots = Capacity;
            TotalScore = 0;
        }
    }
}
