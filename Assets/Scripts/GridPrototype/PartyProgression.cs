using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public enum CharacterStat { Vitality, Strength, Magic, PhysicalDefense, MagicalDefense, Initiative }
    [Serializable] public sealed class StatValues
    {
        public int Vitality, Strength, Magic, PhysicalDefense, MagicalDefense, Initiative;
        public int Get(CharacterStat stat) => stat == CharacterStat.Vitality ? Vitality : stat == CharacterStat.Strength ? Strength : stat == CharacterStat.Magic ? Magic : stat == CharacterStat.PhysicalDefense ? PhysicalDefense : stat == CharacterStat.MagicalDefense ? MagicalDefense : Initiative;
        public void Add(CharacterStat stat)
        {
            switch (stat) {
                case CharacterStat.Vitality: Vitality++; break; case CharacterStat.Strength: Strength++; break;
                case CharacterStat.Magic: Magic++; break; case CharacterStat.PhysicalDefense: PhysicalDefense++; break;
                case CharacterStat.MagicalDefense: MagicalDefense++; break; case CharacterStat.Initiative: Initiative++; break;
            }
        }
    }
    [Serializable] public sealed class PartyProgression
    {
        [Tooltip("Full victory XP is shared, not divided. Include inactive/benched party members here.")]
        public List<BattleCharacterInfo> Members = new List<BattleCharacterInfo>();
        [Min(1)] public int FirstLevelCost = 100;
        [Min(0)] public int CostIncreasePerLevel = 50;
        [Range(1, 100)] public int LevelCap = 50;
        [Min(0)] public int PointsPerLevel = 2;
        [SerializeField] long experience;
        public long Experience => experience;
        public int Level { get { Evaluate(out int level, out _); return level; } }
        public long Progress { get { Evaluate(out _, out long remaining); return remaining; } }
        public long Cost(int level) => Math.Max(1L, FirstLevelCost) + Math.Max(0L, CostIncreasePerLevel) * Math.Max(0L, level - 1);
        void Evaluate(out int level, out long remaining)
        {
            level = 1; remaining = experience;
            while (level < Mathf.Clamp(LevelCap, 1, 100) && remaining >= Cost(level)) { remaining -= Cost(level); level++; }
        }
        public void Register(BattleCharacterInfo member)
        {
            if (!member) return;
            if (!Members.Contains(member)) Members.Add(member);
            member.GrantLevel(Level, PointsPerLevel);
        }
        public int AwardExperience(int amount)
        {
            if (amount <= 0) return 0;
            int before = Level;
            experience = experience > long.MaxValue - amount ? long.MaxValue : experience + amount;
            foreach (var member in new HashSet<BattleCharacterInfo>(Members)) if (member) member.GrantLevel(Level, PointsPerLevel);
            return Level - before;
        }
    }
}
