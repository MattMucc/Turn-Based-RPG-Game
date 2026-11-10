using System;
using System.Collections.Generic;
using UnityEngine;
namespace Game.GridPrototype
{
    // Owned by GameManager, not by a character or an encounter. Session persistence; saves are future work.
    [Serializable]
    public sealed class PartyInventory
    {
        public List<BattleActionOption> Items = new() { HealthPotion(), ManaPotion() };
        [NonSerialized] private int revision;
        public int Revision => revision;
        public void NotifyChanged() => revision++;
        public static BattleActionOption HealthPotion(int quantity = 3) => new BattleActionOption {
            ItemId = "health-potion", Name = "Health Potion", Description = "Restore health to a living target. Does not revive.",
            Category = BattleActionCategory.Item, Effect = ActionEffect.Heal, Healing = 50, Damage = 0,
            Reach = 3, Pattern = BattleTargetPattern.Single, Quantity = quantity
        };
        public int Count(string id) => Items.Find(x => x != null && x.ItemId == id)?.Quantity ?? 0;
        public static BattleActionOption ManaPotion(int quantity = 3) => new BattleActionOption {
            ItemId = "mana-potion", Name = "Mana Potion", Description = "Restore Magic Points to a living target.",
            Category = BattleActionCategory.Item, Effect = ActionEffect.RestoreMagic, MagicRestored = 8, Damage = 0,
            Reach = 3, Pattern = BattleTargetPattern.Single, Quantity = quantity
        };
        public static bool UsableOutsideCombat(BattleActionOption item) => item != null && item.Category == BattleActionCategory.Item
            && (item.Effect == ActionEffect.Heal || item.Effect == ActionEffect.RestoreMagic);
        public int PreviewRecovery(BattleActionOption item, BattleCharacterInfo member, out int current, out int maximum)
        {
            current = maximum = 0;
            if (!member || !UsableOutsideCombat(item) || !Items.Contains(item)) return 0;
            int hpMax = Mathf.Max(1, member.BaseHealth + member.Stat(CharacterStat.Vitality) * member.HealthPerVitality);
            int hp = member.StoredHealth < 0 ? hpMax : Mathf.Clamp(member.StoredHealth, 0, hpMax);
            bool health = item.Effect == ActionEffect.Heal;
            maximum = health ? hpMax : member.EffectiveMaxMagicPoints;
            current = health ? hp : member.StoredMagicPoints < 0 ? maximum : Mathf.Clamp(member.StoredMagicPoints, 0, maximum);
            if (item.Quantity <= 0 || (!health && hp == 0)) return 0;
            return Mathf.Min(maximum - current, Mathf.Max(0, health ? item.Healing : item.MagicRestored));
        }
        public bool TryUseOutsideCombat(BattleActionOption item, BattleCharacterInfo member)
        {
            int recovered = PreviewRecovery(item, member, out _, out _);
            if (recovered <= 0) return false;
            if (item.Effect == ActionEffect.Heal) member.HealOutsideCombat(recovered);
            else member.RestoreMagicOutsideCombat(recovered);
            item.Quantity--; NotifyChanged(); return true;
        }
        public void Add(BattleActionOption definition, int amount)
        {
            if (definition == null || definition.Category != BattleActionCategory.Item || string.IsNullOrWhiteSpace(definition.ItemId) || amount <= 0) return;
            var stock = Items.Find(x => x != null && x.ItemId == definition.ItemId);
            if (stock == null) { stock = JsonUtility.FromJson<BattleActionOption>(JsonUtility.ToJson(definition)); stock.Quantity = 0; Items.Add(stock); NotifyChanged(); }
            stock.Quantity = (int)Math.Min(int.MaxValue, (long)stock.Quantity + amount);
        }
    }
}
