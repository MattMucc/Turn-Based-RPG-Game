using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public enum CharacterStat { Vitality, Strength, Magic, PhysicalDefense, MagicalDefense, Speed, Soul }
    [Serializable] public sealed class StatValues
    {
        public int Vitality, Strength, Magic, PhysicalDefense, MagicalDefense, Soul;
        [UnityEngine.Serialization.FormerlySerializedAs("Initiative")] public int Speed;
        public int Get(CharacterStat stat) => stat == CharacterStat.Vitality ? Vitality : stat == CharacterStat.Strength ? Strength : stat == CharacterStat.Magic ? Magic : stat == CharacterStat.PhysicalDefense ? PhysicalDefense : stat == CharacterStat.MagicalDefense ? MagicalDefense : stat == CharacterStat.Soul ? Soul : Speed;
        public void Add(CharacterStat stat)
            => Increase(stat, 1);
        public void Increase(CharacterStat stat, int amount)
        {
            switch (stat) {
                case CharacterStat.Vitality: Vitality += amount; break; case CharacterStat.Strength: Strength += amount; break;
                case CharacterStat.Magic: Magic += amount; break; case CharacterStat.PhysicalDefense: PhysicalDefense += amount; break;
                case CharacterStat.MagicalDefense: MagicalDefense += amount; break; case CharacterStat.Speed: Speed += amount; break; case CharacterStat.Soul: Soul += amount; break;
            }
        }
    }
    [Serializable] public sealed class PartyProgression
    {
        [Tooltip("Six-character cast. Deployed members earn full victory XP; unused reserves earn half.")]
        public List<BattleCharacterInfo> Members = new List<BattleCharacterInfo>();
        [Min(1)] public int FirstLevelCost = 100;
        [Min(0)] public int CostIncreasePerLevel = 50;
        [Range(1, 100)] public int LevelCap = 50;
        [Min(0)] public int PointsPerLevel = 3;
        [SerializeField] long experience;
        [Serializable] public sealed class MemberExperience
        { public BattleCharacterInfo Member; public long Experience; public int HalfRemainder; }
        [SerializeField] private List<MemberExperience> memberExperience = new();
        MemberExperience Record(BattleCharacterInfo member)
        {
            var record = memberExperience.Find(r => r.Member == member);
            if (record == null) { record = new MemberExperience { Member = member, Experience = experience }; memberExperience.Add(record); }
            return record;
        }
        public int HalfRemainderFor(BattleCharacterInfo member)=>Record(member).HalfRemainder;
        public void RestoreTotal(long total){experience=total;memberExperience.Clear();}
        public void RestoreMember(BattleCharacterInfo member,long xp,int half){var record=Record(member);record.Experience=xp;record.HalfRemainder=half;}
        public long ExperienceFor(BattleCharacterInfo member) => Record(member).Experience;
        public long ProgressFor(BattleCharacterInfo member) { Evaluate(ExperienceFor(member), out _, out long remaining); return remaining; }
        public long Experience => experience;
        public int Level { get { Evaluate(out int level, out _); return level; } }
        public long Progress { get { Evaluate(out _, out long remaining); return remaining; } }
        public long Cost(int level) => Math.Max(1L, FirstLevelCost) + Math.Max(0L, CostIncreasePerLevel) * Math.Max(0L, level - 1);
        void Evaluate(out int level, out long remaining)
            => Evaluate(experience, out level, out remaining);
        void Evaluate(long earned, out int level, out long remaining)
        {
            level = 1; remaining = earned;
            while (level < Mathf.Clamp(LevelCap, 1, 100) && remaining >= Cost(level)) { remaining -= Cost(level); level++; }
        }
        public void Register(BattleCharacterInfo member)
        {
            if (!member) return;
            if (!Members.Contains(member)) Members.Add(member);
            Evaluate(Record(member).Experience, out int level, out _);
            member.GrantLevel(level, PointsPerLevel);
        }
        public int AwardExperience(int amount)
            => AwardExperience(amount, null);
        public int AwardExperience(int amount, IReadOnlyCollection<BattleCharacterInfo> participants)
        {
            if (amount <= 0) return 0;
            var deployed = participants == null ? null : new HashSet<BattleCharacterInfo>(participants);
            int gained = 0;
            foreach (var member in new HashSet<BattleCharacterInfo>(Members)) if (member)
            {
                var record = Record(member);
                long reward = amount;
                if (deployed != null && !deployed.Contains(member))
                { long halves = (long)amount + record.HalfRemainder; reward = halves / 2; record.HalfRemainder = (int)(halves % 2); }
                record.Experience = record.Experience > long.MaxValue - reward ? long.MaxValue : record.Experience + reward;
                Evaluate(record.Experience, out int level, out _);
                gained += Math.Max(0, level - Math.Max(1, member.Level)); member.GrantLevel(level, PointsPerLevel);
            }
            int before = Level;
            experience = experience > long.MaxValue - amount ? long.MaxValue : experience + amount;
            return participants == null ? Level - before : gained;
        }
    }
}
