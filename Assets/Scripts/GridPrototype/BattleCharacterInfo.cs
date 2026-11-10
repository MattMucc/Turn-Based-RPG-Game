using UnityEngine;

namespace Game.GridPrototype
{
    // Authored stats and prototype formula tuning; level growth and allocated points are applied; equipment modifiers remain future work.
    public sealed class BattleCharacterInfo : MonoBehaviour
    {
        public int EffectiveInitiative => Stat(CharacterStat.Initiative);
        [Header("Level growth (prototype tuning)")]
        public StatValues AutomaticGrowth = new StatValues { Vitality = 1 };
        public StatValues AllocatedPoints = new StatValues();
        [SerializeField] private int unspentPoints;
        [SerializeField] private int rewardedLevel = 1;
        public int UnspentPoints => unspentPoints;
        [SerializeField] private int storedHealth = -1;
        public int StoredHealth => storedHealth;
        public void StoreHealth(GridUnit unit) => storedHealth = Mathf.Clamp(unit.Health, 0, unit.MaxHealth);
        public void RestoreBattleHealth(GridUnit unit)
        { if (storedHealth >= 0) unit.Health = Mathf.Clamp(storedHealth, 1, unit.MaxHealth); }
        public void HealOutsideCombat(int amount)
        {
            if (amount <= 0) return;
            int maximum = Mathf.Max(1, BaseHealth + Stat(CharacterStat.Vitality) * HealthPerVitality);
            storedHealth = (int)System.Math.Min(maximum, (long)(storedHealth < 0 ? maximum : storedHealth) + amount);
        }
        public int BaseStat(CharacterStat stat) => stat == CharacterStat.Vitality ? Vitality : stat == CharacterStat.Strength ? Strength : stat == CharacterStat.Magic ? Magic : stat == CharacterStat.PhysicalDefense ? PhysicalDefense : stat == CharacterStat.MagicalDefense ? MagicalDefense : Initiative;
        public int Stat(CharacterStat stat) => (int)System.Math.Min(int.MaxValue, (long)Mathf.Max(0, BaseStat(stat)) + Mathf.Max(0, AutomaticGrowth.Get(stat)) * (long)Mathf.Max(0, Level - 1) + AllocatedPoints.Get(stat));
        public void GrantLevel(int level, int pointsPerLevel)
        {
            Level = Mathf.Max(1, Mathf.Max(Level, level));
            if (level <= rewardedLevel) return;
            unspentPoints = (int)System.Math.Min(int.MaxValue, (long)unspentPoints + (level - rewardedLevel) * (long)Mathf.Max(0, pointsPerLevel));
            rewardedLevel = level;
        }
        public bool SpendPoint(CharacterStat stat)
        {
            if (unspentPoints <= 0 || !System.Enum.IsDefined(typeof(CharacterStat), stat) || Stat(stat) == int.MaxValue) return false;
            AllocatedPoints.Add(stat); unspentPoints--; return true;
        }
        private string StatDescription(CharacterStat stat) => BaseStat(stat) < 0 && Stat(stat) == 0 ? "Not assigned (0)" : Stat(stat).ToString();
        [Header("Prototype stat tuning")]
        [Min(1)] public int BaseHealth = 100;
        [Min(0)] public int HealthPerVitality = 10;
        [Min(0)] public int BurningDamagePerTick = 5, RegenerationHealingPerTick = 5;
        public void Configure(GridUnit unit)
        {
            unit.MaxHealth = Mathf.Max(1, BaseHealth + Stat(CharacterStat.Vitality) * HealthPerVitality);
            unit.Health = unit.MaxHealth;
            unit.Strength = Stat(CharacterStat.Strength); unit.Magic = Stat(CharacterStat.Magic);
            unit.PhysicalDefense = Stat(CharacterStat.PhysicalDefense); unit.MagicalDefense = Stat(CharacterStat.MagicalDefense);
            unit.BurnTickDamage = Mathf.Max(0, BurningDamagePerTick); unit.RegenerationTickHealing = Mathf.Max(0, RegenerationHealingPerTick);
        }
        public Texture Portrait;
        public string DisplayName;
        [Min(0)] public int Level;
        public string Race, Role;
        public int Vitality = -1, Strength = -1, Magic = -1, PhysicalDefense = -1, MagicalDefense = -1, Initiative = -1;
        [TextArea] public string Resistances;
        [TextArea] public string Statuses;
        public static string Value(int value) => value < 0 ? "Not assigned" : value.ToString();
        public string Details(GridUnit unit, string fallbackName)
        {
            string name = string.IsNullOrWhiteSpace(DisplayName) ? fallbackName : DisplayName;
            string statuses = string.IsNullOrWhiteSpace(Statuses) ? "None" : Statuses;
            string live = unit.EffectDetails();
            if (!string.IsNullOrEmpty(live)) statuses = live + (statuses == "None" ? "" : "\n" + statuses);
            if (unit.EffectiveBlock) statuses = "Blocking" + (statuses == "None" ? "" : "\n" + statuses);
            return $"<b>{name}</b>\nHealth  {unit.Health} / {unit.MaxHealth}\nLevel  {(Level > 0 ? Level.ToString() : "Not assigned")}\nRace  {Assigned(Race)}\nRole  {Assigned(Role)}\n\n<b>Stats</b>\nVitality  {StatDescription(CharacterStat.Vitality)}\nStrength  {StatDescription(CharacterStat.Strength)}\nMagic  {StatDescription(CharacterStat.Magic)}\nPhysical Defense  {StatDescription(CharacterStat.PhysicalDefense)}\nMagical Defense  {StatDescription(CharacterStat.MagicalDefense)}\nInitiative  {EffectiveInitiative}{(Initiative < 0 && EffectiveInitiative == 0 ? " (unassigned; defaults to 0)" : "")}\n\n<b>Statuses</b>\n{statuses}\n\n<b>Resistances</b>\n{Assigned(Resistances)}";
        }
        private static string Assigned(string value) => string.IsNullOrWhiteSpace(value) ? "Not assigned" : value;
    }
}
