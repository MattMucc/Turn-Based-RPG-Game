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
        public void Add(BattleActionOption definition, int amount)
        {
            if (definition == null || definition.Category != BattleActionCategory.Item || string.IsNullOrWhiteSpace(definition.ItemId) || amount <= 0) return;
            var stock = Items.Find(x => x != null && x.ItemId == definition.ItemId);
            if (stock == null) { stock = JsonUtility.FromJson<BattleActionOption>(JsonUtility.ToJson(definition)); stock.Quantity = 0; Items.Add(stock); NotifyChanged(); }
            stock.Quantity = (int)Math.Min(int.MaxValue, (long)stock.Quantity + amount);
        }
    }
}
