using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public enum DebtKind { Blood, Might, Guard }
    // Each stack retains deadlines for previous bearers, preventing transfer loops from refinancing it.
    public sealed class DebtStack
    {
        public readonly DebtKind Kind;
        public readonly int BloodDamage;
        readonly Dictionary<GridUnit, int> deadlines = new();
        public DebtStack(DebtKind kind, int bloodDamage) { Kind = kind; BloodDamage = Mathf.Max(0, bloodDamage); }
        public int DueOn(GridUnit bearer)
        {
            if (!deadlines.TryGetValue(bearer, out int due)) deadlines[bearer] = due = bearer.TurnsStarted + 1;
            return due;
        }
    }
    public sealed partial class GridUnit
    {
        public bool UsesDebt;
        public float DebtBonusPerStack = .05f;
        public int BloodDebtDamage = 5;
        public int MaximumExtraBorrow = 9;
        public int TurnsStarted { get; private set; }
        readonly List<DebtStack> debts = new();
        public IReadOnlyList<DebtStack> Debts => debts;
        public int DebtCount => debts.Count;
        public float SkillDebtMultiplier => UsesDebt ? 1 + DebtCount * DebtBonusPerStack : 1;
        public int TransferCapacity => DebtCount >= 3 ? 4 : 3;
        public void Borrow(DebtKind kind, int count)
        { for (int i = 0; i < count; i++) ReceiveDebt(new DebtStack(kind, BloodDebtDamage)); }
        public void ReceiveDebt(DebtStack debt)
        { if (debt == null || debts.Contains(debt)) return; debt.DueOn(this); debts.Add(debt); }
        public bool RemoveDebt(DebtStack debt) => debts.Remove(debt);
        public void SettleDebts() => debts.Clear();
        public void CollectDebt(DebtStack debt, float damageMultiplier = 1)
        {
            if (debt.Kind == DebtKind.Blood) TakeDamage(Mathf.RoundToInt(debt.BloodDamage * damageMultiplier));
            else ApplyStatus(debt.Kind == DebtKind.Might ? CombatStatus.Weakened : CombatStatus.Exposed, 1);
        }
        void CollectDueDebts()
        {
            var due = debts.FindAll(d => d.DueOn(this) <= TurnsStarted);
            foreach (var debt in due) { debts.Remove(debt); CollectDebt(debt); }
        }
    }
    public static class DebtKit
    {
        public static List<BattleActionOption> Create()
        {
            BattleActionOption Strike(string name, string description, DebtKind debt, int amount, int power, int reach, BattleTargetPattern pattern)
                => new BattleActionOption { Name = name, Description = description, Category = BattleActionCategory.Skill,
                    DebtSkill = true, BorrowType = debt, BorrowStacks = amount, Damage = power, Reach = reach, Pattern = pattern,
                    Scaling = ScalingStat.Strength, StatScaling = 1, SignatureQte = true, QteBonusDamage = 5, QteDuration = 2 };
            var result = new List<BattleActionOption> {
                Strike("Blood Price", "Borrow strength for a crushing blow.", DebtKind.Blood, 2, 24, 1, BattleTargetPattern.Single),
                Strike("Overextend", "Strike three chosen targets; repeated targets are allowed.", DebtKind.Might, 3, 8, 3, BattleTargetPattern.Multiple),
                Strike("Collateral Damage", "Unleash a burst that can also strike allies.", DebtKind.Guard, 3, 16, 3, BattleTargetPattern.Area),
                new BattleActionOption { Name = "Pass the Bill", Description = "Choose liabilities to transfer. The recipient gets a turn to respond.", Effect = ActionEffect.TransferDebt, Reach = 4 },
                new BattleActionOption { Name = "Foreclose", Description = "Consume chosen liabilities and collect their consequences immediately.", Effect = ActionEffect.Foreclose, Reach = 4 },
                new BattleActionOption { Name = "Settle Accounts", Description = "Clear your personal debt and its strength bonus.", Effect = ActionEffect.SettleDebt, Reach = 0 }
            };
            for (int i = 0; i < result.Count; i++) {
                var skill = result[i]; skill.Category = BattleActionCategory.Skill; skill.DebtSkill = true;
                skill.FavoriteSlot = i < 4 ? i : -1; skill.SignatureQte = true;
                if (skill.Effect != ActionEffect.Damage) { skill.Damage = 0; skill.QteBonusDamage = 0; skill.QteDuration = 2; }
            }
            return result;
        }
    }
}
