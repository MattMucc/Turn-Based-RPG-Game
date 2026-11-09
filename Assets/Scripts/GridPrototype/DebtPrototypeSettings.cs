using UnityEngine;
namespace Game.GridPrototype
{
    public sealed class DebtPrototypeSettings : MonoBehaviour
    {
        [Min(0)] public float SkillDamageBonusPerStack = .05f;
        [Min(0)] public int BloodDamagePerStack = 5;
        [Min(0)] public int MaximumExtraBorrowPerSkill = 9;
        public void Configure(GridUnit unit)
        { unit.UsesDebt = true; unit.DebtBonusPerStack = SkillDamageBonusPerStack; unit.BloodDebtDamage = BloodDamagePerStack; unit.MaximumExtraBorrow = MaximumExtraBorrowPerSkill; }
    }
}
