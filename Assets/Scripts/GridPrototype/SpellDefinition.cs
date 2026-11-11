using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.GridPrototype
{
    [Serializable] public sealed class SpellTier
    {
        [Min(0)] public int RequiredExperience;
        public BattleActionOption Action = new() { Category = BattleActionCategory.Magic };
    }
    [Serializable] public sealed class LearnedSpell
    {
        public string Id;
        [Min(0)] public int Experience;
    }
    [CreateAssetMenu(menuName = "Game/Spell Definition")]
    public sealed class SpellDefinition : ScriptableObject
    {
        public string Id, DisplayName;
        [TextArea] public string Description;
        public Texture Image;
        [Min(1)] public int ExperiencePerCast = 1;
        public List<SpellTier> Tiers = new();
        public IEnumerable<SpellTier> OrderedTiers => Tiers.Where(t => t != null && t.Action != null).OrderBy(t => t.RequiredExperience);
        public int MaximumExperience => OrderedTiers.Select(t => t.RequiredExperience).DefaultIfEmpty(0).Max();
        public static SpellDefinition Find(string id) => string.IsNullOrEmpty(id) ? null : Resources.LoadAll<SpellDefinition>("Spells").FirstOrDefault(s => s.Id == id);
        public BattleActionOption CreateAction(SpellTier tier)
        {
            var action = JsonUtility.FromJson<BattleActionOption>(JsonUtility.ToJson(tier.Action));
            action.Category = BattleActionCategory.Magic; action.SpellId = Id;
            action.SpellTier = OrderedTiers.ToList().IndexOf(tier) + 1;
            action.Name = DisplayName + (OrderedTiers.Count() > 1 ? " " + Roman(action.SpellTier) : "");
            action.FavoriteSlot = -1;
            if (!action.Image) action.Image = Image;
            return action;
        }
        static string Roman(int number) => number <= 5 ? new[] { "", "I", "II", "III", "IV", "V" }[Mathf.Max(0, number)] : number.ToString();
    }
    // Snapshot a single committed cast. Multi-hit/area spells cannot multiply mastery gains.
    public sealed class SpellMasteryUse
    {
        readonly BattleActionLoadout owner;
        readonly BattleActionOption action;
        bool successful, completed;
        public SpellMasteryUse(BattleActionLoadout owner, BattleActionOption action)
        {
            if (!owner || action == null || action.Category != BattleActionCategory.Magic || string.IsNullOrEmpty(action.SpellId)) return;
            if (!owner.Options.Contains(action)) return;
            this.owner = owner; this.action = action;
        }
        public void Record(bool landed, bool hostile, int healthLost)
        {
            if (completed || !owner || !landed) return;
            successful |= action.Effect == ActionEffect.Damage ? hostile : action.Effect == ActionEffect.Heal && !hostile && healthLost < 0;
        }
        public int Complete()
        {
            if (completed) return 0; completed = true;
            return successful && owner ? owner.AwardSpellExperience(action.SpellId) : 0;
        }
    }
}
