using System;
using UnityEngine;

namespace Game.GridPrototype
{
    public sealed partial class GridUnit
    {
        public int MaxMagicPoints { get; private set; } = 24;
        public int MagicPoints { get; private set; } = 24;
        public int FocusRecovery = 8;
        public void InitializeMagicPoints(int maximum, int current = -1)
        {
            MaxMagicPoints = Math.Max(0, maximum);
            MagicPoints = current < 0 ? MaxMagicPoints : Mathf.Clamp(current, 0, MaxMagicPoints);
        }
        public int RestoreMagicPoints(int amount)
        {
            if (Health <= 0) return 0;
            int before = MagicPoints;
            MagicPoints = (int)Math.Min(MaxMagicPoints, (long)MagicPoints + Math.Max(0, amount));
            return MagicPoints - before;
        }
        public bool CanAffordMagicPoints(int cost) => cost >= 0 && MagicPoints >= cost;
        public bool Focus()
        {
            if (Health <= 0 || MagicPoints >= MaxMagicPoints || FocusRecovery <= 0 || !SpendAction()) return false;
            RestoreMagicPoints(FocusRecovery); return true;
        }
    }
}
