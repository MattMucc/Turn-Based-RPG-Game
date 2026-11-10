using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public sealed partial class InSceneGridBattle
    {
        public string PreviewDamage(BattleTarget target)
        {
            var action = ChosenAction ?? HoveredAction;
            if (target == null || !target.HasHealth || action == null || (action.Effect != ActionEffect.Damage && action.Effect != ActionEffect.Heal) || !ActionReady) return "";
            var unit = target.Unit;
            if (action.Effect == ActionEffect.Heal) return unit == null ? "Cannot heal objects." : unit.Health <= 0 ? "Does not revive." : $"Healing: +{Mathf.Min(action.Healing, unit.MaxHealth - unit.Health)} HP";
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
    }
}
