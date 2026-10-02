using System;

namespace ImpactLab.Gameplay
{
    public sealed class ShotCounter
    {
        public int MaximumShots { get; }
        public int RemainingShots { get; private set; }
        public bool HasShotsRemaining => RemainingShots > 0;
        public event Action<int> RemainingShotsChanged;

        public ShotCounter(int maximumShots)
        {
            if (maximumShots <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumShots), "Must be positive.");
            }
            MaximumShots = maximumShots;
            RemainingShots = maximumShots;
        }

        public bool TryConsume()
        {
            if (!HasShotsRemaining)
            {
                return false;
            }
            RemainingShots--;
            RemainingShotsChanged?.Invoke(RemainingShots);
            return true;
        }

        public void Reset()
        {
            if (RemainingShots == MaximumShots)
            {
                return;
            }
            RemainingShots = MaximumShots;
            RemainingShotsChanged?.Invoke(RemainingShots);
        }
    }
}
