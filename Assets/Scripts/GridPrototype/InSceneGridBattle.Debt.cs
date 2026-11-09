using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public sealed partial class InSceneGridBattle
    {
        readonly List<DebtStack> selectedDebts = new();
        public IReadOnlyList<DebtStack> SelectedDebts => selectedDebts;
        public bool SelectDebts(IEnumerable<DebtStack> selection)
        {
            if (!ActionReady) return false;
            var next = new List<DebtStack>();
            foreach (var debt in selection) if (debt == null || !ContainsDebt(Active, debt) || next.Contains(debt)) return false; else next.Add(debt);
            selectedDebts.Clear(); selectedDebts.AddRange(next);
            if (ChosenAction?.Effect == ActionEffect.TransferDebt) {
                ChosenAction.UpdateDebtShape(Active); ChosenAction.MaxTargets = Mathf.Min(ChosenAction.MaxTargets, Mathf.Max(1, next.Count));
                if (ChosenAction.MaxTargets == 1) ChosenAction.Pattern = BattleTargetPattern.Single;
            }
            return true;
        }
        static bool ContainsDebt(GridUnit unit, DebtStack debt)
        { foreach (var item in unit.Debts) if (item == debt) return true; return false; }
        public bool CanUseOption(BattleActionOption option) => option != null && option.Available &&
            (!option.DebtSkill || Active.UsesDebt) && (!option.ChoosesDebt || Active.DebtCount > 0);
        public string PreviewDamage(BattleTarget target)
        {
            var action = ChosenAction ?? HoveredAction;
            if (target == null || !target.HasHealth || action == null || (action.Effect != ActionEffect.Damage && action.Effect != ActionEffect.Foreclose) || !ActionReady) return "";
            var unit = target.Unit;
            if (action.Effect == ActionEffect.Foreclose)
            {
                if (unit == null) return "Debt collection affects living creatures only.";
                int collection = 0, might = 0, guard = 0;
                foreach (var debt in selectedDebts) {
                    if (debt.Kind == DebtKind.Blood) collection += Mathf.RoundToInt(debt.BloodDamage * action.SkillMultiplier(Active));
                    else if (debt.Kind == DebtKind.Might) might++; else guard++;
                }
                int shield = Mathf.Min(unit.Barrier, collection);
                return $"Collection: {Mathf.Min(target.Health, collection - shield)} HP · {shield} absorbed\n{might} Weakened · {guard} Exposed · bypasses defense";
            }
            int power = action.CalculateDamage(Active, null, action.Damage);
            int dealt = action.CalculateDamage(Active, unit, action.Damage);
            int absorbed = Mathf.Min(unit?.Barrier ?? 0, dealt);
            string result = $"On hit: {Mathf.Min(target.Health, dealt - absorbed)} HP" + (absorbed > 0 ? $" · {absorbed} absorbed" : "");
            if (action.SignatureQte && action.QteBonusDamage > 0)
            {
                int best = action.CalculateDamage(Active, unit, action.Damage + action.QteBonusDamage);
                result += $" → {Mathf.Min(target.Health, Mathf.Max(0, best - (unit?.Barrier ?? 0)))} HP with QTE";
            }
            result += $"\n{power} power − {power - dealt} mitigation = {dealt}";
            if (unit?.EffectiveBlock == true) result += " (Block)";
            if (action.Pattern == BattleTargetPattern.Multiple) result += " · per hit";
            return result;
        }
        public string RecentHitFeedback { get; private set; } = "";
        public float RecentHitFeedbackUntil { get; private set; }
        void Feedback(BattleTarget target, bool landed, int lost, int absorbed, bool blocked)
        {
            RecentHitFeedback = target.Name + (landed ? $": {lost} HP damage" + (absorbed > 0 ? $" · {absorbed} absorbed" : "") + (blocked ? " · Blocked" : "") : ": Miss");
            RecentHitFeedbackUntil = Time.unscaledTime + 2;
        }
        void ApplyDebtAction(BattleActionOption action, List<DebtStack> spent, BattleTarget target, int batch, int batchCount)
        {
            if (target.Unit == null || !target.Alive) return;
            if (action.Effect == ActionEffect.TransferDebt)
            {
                for (int i = batch; i < spent.Count; i += batchCount) target.Unit.ReceiveDebt(spent[i]);
            }
            else if (action.Effect == ActionEffect.Foreclose)
            {
                int before = target.Health, barrier = target.Unit.Barrier;
                foreach (var debt in spent) target.Unit.CollectDebt(debt, action.CommittedSkillMultiplier);
                RecordHit(target.ActorIndex, before - target.Health);
                Feedback(target, true, before - target.Health, barrier - target.Unit.Barrier, false);
            }
        }
    }
}
