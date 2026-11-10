using System;
using UnityEngine;

namespace Game.GridPrototype
{
    public sealed partial class GridUnit
    {
        public const int MaximumSkillPoints = 9;
        public int SkillPoints { get; private set; } = 3;
        public int TurnSkillPointGain = 1;
        public int AttackSkillPointGain = 1;
        public void InitializeSkillPoints(int startingPoints) => SkillPoints = Mathf.Clamp(startingPoints, 0, MaximumSkillPoints);
        public int RestoreSkillPoints(int amount)
        {
            int before = SkillPoints;
            SkillPoints = (int)Math.Min(MaximumSkillPoints, (long)SkillPoints + Math.Max(0, amount));
            return SkillPoints - before;
        }
        public bool CanAffordSkillPoints(int cost) => cost >= 0 && SkillPoints >= cost;
        public bool SpendActionWithSkillPoints(int cost)
        {
            if (!CanAffordSkillPoints(cost) || !SpendAction()) return false;
            SkillPoints -= cost;
            return true;
        }
    }
}
