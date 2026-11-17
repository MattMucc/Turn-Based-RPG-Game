using System;
using UnityEngine;

namespace Game.GridPrototype
{
    public sealed partial class GridUnit
    {
        public const int MaximumActionPoints = 9;
        public int ActionPoints { get; private set; } = 3;
        public int TurnActionPointGain = 1;
        public int AttackActionPointGain = 1;
        public void InitializeActionPoints(int startingPoints) => ActionPoints = Mathf.Clamp(startingPoints, 0, MaximumActionPoints);
        public int RestoreActionPoints(int amount)
        {
            int before = ActionPoints;
            ActionPoints = (int)Math.Min(MaximumActionPoints, (long)ActionPoints + Math.Max(0, amount));
            return ActionPoints - before;
        }
        public bool CanAffordActionPoints(int cost) => cost >= 0 && ActionPoints >= cost;
        public bool SpendActionWithActionPoints(int cost, int magicCost = 0)
        {
            if (!CanAffordActionPoints(cost) || !CanAffordMagicPoints(magicCost) || !SpendAction()) return false;
            ActionPoints -= cost;
            MagicPoints -= magicCost;
            return true;
        }
    }
}
