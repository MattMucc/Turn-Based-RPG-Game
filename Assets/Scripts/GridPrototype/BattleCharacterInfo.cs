using UnityEngine;

namespace Game.GridPrototype
{
    // Authored stats and prototype formula tuning; level growth and allocated points are applied; equipped stat modifiers are applied.
    public sealed class BattleCharacterInfo : MonoBehaviour
    {
        public int EffectiveSpeed => Stat(CharacterStat.Speed);
        [Header("Level growth (prototype tuning)")]
        public StatValues AutomaticGrowth = new StatValues { Vitality = 1 };
        public StatValues AllocatedPoints = new StatValues();
        [SerializeField] private int unspentPoints;
        [SerializeField] private int rewardedLevel = 1;
        public int UnspentPoints => unspentPoints;
        [SerializeField] private int storedHealth = -1;
        public int StoredHealth => storedHealth;
        private int storedMagicPoints = -1;
        public int StoredMagicPoints => storedMagicPoints;
        public void CaptureProgress(SavedCharacter saved){saved.Level=Level;saved.Unspent=unspentPoints;saved.Rewarded=rewardedLevel;saved.Allocated=JsonUtility.FromJson<StatValues>(JsonUtility.ToJson(AllocatedPoints));saved.Health=storedHealth;saved.Magic=storedMagicPoints;}
        public void RestoreProgress(SavedCharacter saved){Level=saved.Level;unspentPoints=saved.Unspent;rewardedLevel=saved.Rewarded;AllocatedPoints=JsonUtility.FromJson<StatValues>(JsonUtility.ToJson(saved.Allocated));storedHealth=saved.Health;storedMagicPoints=saved.Magic;}

        public void StoreMagicPoints(GridUnit unit) => storedMagicPoints = Mathf.Clamp(unit.MagicPoints, 0, unit.MaxMagicPoints);
        public void StoreHealth(GridUnit unit) => storedHealth = Mathf.Clamp(unit.Health, 0, unit.MaxHealth);
        public void RestoreBattleHealth(GridUnit unit)
        { if (storedHealth >= 0) unit.Health = Mathf.Clamp(storedHealth, 1, unit.MaxHealth); }
        public void HealOutsideCombat(int amount)
        {
            if (amount <= 0) return;
            int maximum = EffectiveMaxHealth;
            storedHealth = (int)System.Math.Min(maximum, (long)(storedHealth < 0 ? maximum : storedHealth) + amount);
        }
        public int BaseStat(CharacterStat stat) => stat == CharacterStat.Vitality ? Vitality : stat == CharacterStat.Strength ? Strength : stat == CharacterStat.Magic ? Magic : stat == CharacterStat.PhysicalDefense ? PhysicalDefense : stat == CharacterStat.MagicalDefense ? MagicalDefense : stat == CharacterStat.Soul ? Soul : Speed;
        public int Stat(CharacterStat stat) => StatReplacingEquipment(stat,null,null);
        public int StatReplacingCopy(CharacterStat stat,OwnedEquipment previous,OwnedEquipment next) => (int)System.Math.Max(0,System.Math.Min(int.MaxValue,
            (long)Mathf.Max(0,BaseStat(stat))+Mathf.Max(0,AutomaticGrowth.Get(stat))*(long)Mathf.Max(0,Level-1)+AllocatedPoints.Get(stat)
            +(GetComponent<CharacterEquipment>()?.Bonus(stat)??0)-EquipmentUpgrades.Bonus(previous,stat)+EquipmentUpgrades.Bonus(next,stat)));
        public int StatReplacingEquipment(CharacterStat stat, EquipmentDefinition previous, EquipmentDefinition next) => (int)System.Math.Max(0, System.Math.Min(int.MaxValue,
            (long)Mathf.Max(0, BaseStat(stat)) + Mathf.Max(0, AutomaticGrowth.Get(stat)) * (long)Mathf.Max(0, Level - 1) + AllocatedPoints.Get(stat)
            + (GetComponent<CharacterEquipment>()?.Bonus(stat) ?? 0) - (previous?.Bonuses?.Get(stat) ?? 0) + (next?.Bonuses?.Get(stat) ?? 0)));
        public int EffectiveMaxHealth => (int)System.Math.Min(int.MaxValue, System.Math.Max(1, (long)BaseHealth + (long)Stat(CharacterStat.Vitality) * HealthPerVitality));
        public void MaterializeVitals() { if(storedHealth<0)storedHealth=EffectiveMaxHealth; if(storedMagicPoints<0)storedMagicPoints=EffectiveMaxMagicPoints; }
        public void ClampStoredVitals() { if(storedHealth>=0)storedHealth=Mathf.Clamp(storedHealth,0,EffectiveMaxHealth); if(storedMagicPoints>=0)storedMagicPoints=Mathf.Clamp(storedMagicPoints,0,EffectiveMaxMagicPoints); }
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
        public bool SpendPoints(System.Collections.Generic.IReadOnlyDictionary<CharacterStat, int> allocation)
        {
            if (allocation == null) return false;
            long total = 0;
            foreach (var pair in allocation)
            {
                if (!System.Enum.IsDefined(typeof(CharacterStat), pair.Key) || pair.Value < 0 ||
                    (long)Stat(pair.Key) + pair.Value > int.MaxValue ||
                    (long)AllocatedPoints.Get(pair.Key) + pair.Value > int.MaxValue) return false;
                total += pair.Value;
            }
            if (total <= 0 || total > unspentPoints) return false;
            // Validate the whole transaction before changing any character state.
            MaterializeVitals();
            foreach (var pair in allocation) AllocatedPoints.Increase(pair.Key, pair.Value);
            unspentPoints -= (int)total;
            return true;
        }
        private string StatDescription(CharacterStat stat) => BaseStat(stat) < 0 && Stat(stat) == 0 ? "Not assigned (0)" : Stat(stat).ToString();
        [Header("Prototype stat tuning")]
        [Min(1)] public int BaseHealth = 100;
        [Min(0)] public int HealthPerVitality = 10;
        [Header("Action Points (AP)")]
        [Range(0, 9)] [UnityEngine.Serialization.FormerlySerializedAs("StartingSkillPoints")] public int StartingActionPoints = 3;
        [Min(0)] [UnityEngine.Serialization.FormerlySerializedAs("SkillPointsPerTurn")] public int ActionPointsPerTurn = 1;
        [Min(0)] [UnityEngine.Serialization.FormerlySerializedAs("SkillPointsPerBasicAttack")] public int ActionPointsPerBasicAttack = 1;
        [Header("Magic Points (prototype tuning)")]
        [Min(0)] public int StartingMaxMagicPoints = 24;
        [Min(0)] public int MagicPointsPerLevel = 2;
        [Tooltip("Max MP per Magic gained through level growth or allocation, above this character's base Magic. Base Magic does not inflate starting MP.")]
        [Min(0)] public int MagicPointsPerMagic = 2;
        [Min(0)] public int FocusMagicRecovery = 8;
        public int EffectiveMaxMagicPoints => (int)System.Math.Min(int.MaxValue,
            (long)Mathf.Max(0, StartingMaxMagicPoints) + (long)Mathf.Max(0, Level - 1) * Mathf.Max(0, MagicPointsPerLevel)
            + (long)Mathf.Max(0, Stat(CharacterStat.Magic) - Mathf.Max(0, BaseStat(CharacterStat.Magic))) * Mathf.Max(0, MagicPointsPerMagic));
        public void RestoreMagicOutsideCombat(int amount)
        {
            if (amount <= 0) return;
            storedMagicPoints = (int)System.Math.Min(EffectiveMaxMagicPoints,
                (long)(storedMagicPoints < 0 ? EffectiveMaxMagicPoints : storedMagicPoints) + amount);
        }
        [Header("Enemy prototype") ]
        [Min(1)] public int EnemyComboActionPointCost = 3;
        [Header("Status tuning")]
        [Min(0)] public int BurningDamagePerTick = 5, RegenerationHealingPerTick = 5;
        public void Configure(GridUnit unit)
        {
            unit.InitializeMagicPoints(EffectiveMaxMagicPoints, storedMagicPoints);
            unit.FocusRecovery = Mathf.Max(0, FocusMagicRecovery);
            unit.InitializeActionPoints(StartingActionPoints);
            unit.TurnActionPointGain = Mathf.Max(0, ActionPointsPerTurn);
            unit.AttackActionPointGain = Mathf.Max(0, ActionPointsPerBasicAttack);
            unit.MaxHealth = EffectiveMaxHealth;
            unit.Health = unit.MaxHealth;
            unit.Soul = Stat(CharacterStat.Soul); unit.Strength = Stat(CharacterStat.Strength); unit.Magic = Stat(CharacterStat.Magic);
            unit.PhysicalDefense = Stat(CharacterStat.PhysicalDefense); unit.MagicalDefense = Stat(CharacterStat.MagicalDefense);
            unit.BurnTickDamage = Mathf.Max(0, BurningDamagePerTick); unit.RegenerationTickHealing = Mathf.Max(0, RegenerationHealingPerTick);
        }
        public Texture Portrait;
        public string DisplayName;
        [Min(0)] public int Level;
        public string Race, Role;
        public int Vitality = -1, Strength = -1, Magic = -1, PhysicalDefense = -1, MagicalDefense = -1;
        [UnityEngine.Serialization.FormerlySerializedAs("Initiative")] public int Speed = -1;
        public int Soul = 0;
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
            return $"<b>{name}</b>\nHealth  {unit.Health} / {unit.MaxHealth}\nMP  {unit.MagicPoints} / {unit.MaxMagicPoints}\nAP  {unit.ActionPoints} / {GridUnit.MaximumActionPoints}\nMovement  {unit.Movement} / {GridUnit.MaximumMovement}\nLevel  {(Level > 0 ? Level.ToString() : "Not assigned")}\nRace  {Assigned(Race)}\nRole  {Assigned(Role)}\n\n<b>Stats</b>\nVitality  {StatDescription(CharacterStat.Vitality)}\nStrength  {StatDescription(CharacterStat.Strength)}\nMagic  {StatDescription(CharacterStat.Magic)}\nSoul  {StatDescription(CharacterStat.Soul)}\nPhysical Defense  {StatDescription(CharacterStat.PhysicalDefense)}\nMagical Defense  {StatDescription(CharacterStat.MagicalDefense)}\nSpeed  {EffectiveSpeed}{(Speed < 0 && EffectiveSpeed == 0 ? " (unassigned; defaults to 0)" : "")}\n\n<b>Statuses</b>\n{statuses}\n\n<b>Resistances</b>\n{Assigned(Resistances)}";
        }
        private static string Assigned(string value) => string.IsNullOrWhiteSpace(value) ? "Not assigned" : value;
    }
}
