using System;
using UnityEngine;

namespace Game.GridPrototype
{
    public static class AttributePreview
    {
        public static GridUnit AfterPoints(BattleCharacterInfo member, System.Collections.Generic.IReadOnlyDictionary<CharacterStat, int> allocation)
        {
            var unit = new GridUnit(member.name, Vector2Int.zero); member.Configure(unit);
            long Count(CharacterStat stat) => allocation != null && allocation.TryGetValue(stat, out int value) ? Math.Max(0, value) : 0;
            int Sum(int value, long delta) => (int)Math.Min(int.MaxValue, (long)value + Math.Max(0L, delta));
            unit.MaxHealth = Sum(unit.MaxHealth, Count(CharacterStat.Vitality) * member.HealthPerVitality);
            unit.Strength = Sum(unit.Strength, Count(CharacterStat.Strength));
            unit.Magic = Sum(unit.Magic, Count(CharacterStat.Magic));
            unit.Soul = Sum(unit.Soul, Count(CharacterStat.Soul));
            unit.PhysicalDefense = Sum(unit.PhysicalDefense, Count(CharacterStat.PhysicalDefense));
            unit.MagicalDefense = Sum(unit.MagicalDefense, Count(CharacterStat.MagicalDefense));
            unit.InitializeMagicPoints(Sum(unit.MaxMagicPoints, Count(CharacterStat.Magic) * member.MagicPointsPerMagic), unit.MagicPoints);
            return unit;
        }
        public static GridUnit AfterPoint(BattleCharacterInfo member, CharacterStat stat)
        {
            var unit = new GridUnit(member.name, Vector2Int.zero); member.Configure(unit);
            if (member.Stat(stat) == int.MaxValue) return unit;
            switch (stat)
            {
                case CharacterStat.Vitality: unit.MaxHealth = Add(unit.MaxHealth, member.HealthPerVitality); break;
                case CharacterStat.Soul: unit.Soul = Add(unit.Soul, 1); break;
                case CharacterStat.Strength: unit.Strength = Add(unit.Strength, 1); break;
                case CharacterStat.Magic:
                    unit.Magic = Add(unit.Magic, 1);
                    unit.InitializeMagicPoints(Add(unit.MaxMagicPoints, member.MagicPointsPerMagic), unit.MagicPoints); break;
                case CharacterStat.PhysicalDefense: unit.PhysicalDefense = Add(unit.PhysicalDefense, 1); break;
                case CharacterStat.MagicalDefense: unit.MagicalDefense = Add(unit.MagicalDefense, 1); break;
            }
            return unit;
        }
        static int Add(int value, int amount) => (int)Math.Min(int.MaxValue, (long)value + Math.Max(0, amount));
        public static string Name(CharacterStat stat) => stat == CharacterStat.PhysicalDefense ? "Physical Defense" : stat == CharacterStat.MagicalDefense ? "Magical Defense" : stat.ToString();
        public static string Explanation(CharacterStat stat) => stat switch
        {
            CharacterStat.Vitality => "Increases maximum health.",
            CharacterStat.Soul => "Improves the damage of Soul-scaling Skills.",
            CharacterStat.Strength => "Improves Strength-scaling actions. Push requires your Strength to exceed the target's Strength.",
            CharacterStat.Magic => "Improves Magic-scaling actions and increases maximum MP.",
            CharacterStat.PhysicalDefense => "Reduces incoming physical damage. Element and damage type are separate.",
            CharacterStat.MagicalDefense => "Reduces incoming magical damage. Element and damage type are separate.",
            _ => "Higher Speed acts earlier in the battle's turn order. It does not grant extra turns."
        };
    }
}
