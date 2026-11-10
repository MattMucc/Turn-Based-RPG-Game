using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public enum CombatStatus { None, Burning, Regeneration, Paralyzed, Weakened, Exposed }
    public enum DamageKind { Physical, Magical }
    public enum ScalingStat { None, Strength, Magic }
    public enum CombatElement { None, Fire, Lightning, Ice }
    public enum ActionEffect { Damage, Support, Push, Dash, Heal }

    public sealed partial class GridUnit
    {
        public int Strength, Magic, PhysicalDefense, MagicalDefense;
        public int BurnTickDamage = 5, RegenerationTickHealing = 5;
        public int Barrier { get; private set; }
        public int BarrierMaximum { get; private set; }
        readonly Dictionary<CombatStatus, int> stacks = new Dictionary<CombatStatus, int>();
        int paralyzedAtTurnStart, weakenedAtTurnStart, exposedAtTurnStart;
        public int Stacks(CombatStatus status) => stacks.TryGetValue(status, out int value) ? value : 0;
        public void RemoveStatus(CombatStatus status) => stacks.Remove(status);
        public bool CanReact => !ReactionsDisabled && Stacks(CombatStatus.Paralyzed) == 0;
        public bool CanBlock => Stacks(CombatStatus.Paralyzed) == 0;
        public bool EffectiveBlock => Blocking && CanBlock;
        public void ApplyStatus(CombatStatus status, int amount)
        {
            if (status == CombatStatus.None || amount <= 0 || Health <= 0) return;
            stacks[status] = (int)Math.Min(10L, (long)Stacks(status) + amount);
            if (status == CombatStatus.Paralyzed) Blocking = false;
        }
        public void AddBarrier(int amount)
        {
            if (amount <= 0 || Health <= 0) return;
            Barrier = (int)Math.Min(int.MaxValue, (long)Barrier + amount);
            BarrierMaximum = Math.Max(BarrierMaximum, Barrier);
        }
        public int TakeDamage(int amount)
        {
            amount = Math.Max(0, amount);
            int absorbed = Math.Min(Barrier, amount); Barrier -= absorbed;
            int lost = Math.Min(Health, amount - absorbed); Health -= lost;
            if (Barrier == 0) BarrierMaximum = 0;
            return lost;
        }
        public int Heal(int amount)
        {
            if (Health <= 0) return 0;
            int gained = Math.Min(MaxHealth - Health, Math.Max(0, amount)); Health += gained; return gained;
        }
        void TickStartEffects()
        {
            // Snapshot which effects were present before processing; no newly added effect ticks here.
            bool burn = Stacks(CombatStatus.Burning) > 0, regen = Stacks(CombatStatus.Regeneration) > 0;
            paralyzedAtTurnStart = Stacks(CombatStatus.Paralyzed);
            weakenedAtTurnStart = Stacks(CombatStatus.Weakened); exposedAtTurnStart = Stacks(CombatStatus.Exposed);
            if (burn) { TakeDamage(BurnTickDamage); stacks[CombatStatus.Burning]--; }
            if (regen) { Heal(RegenerationTickHealing); stacks[CombatStatus.Regeneration]--; }
        }
        void TickEndEffects()
        {
            // Prototype duration: paralysis lasts through the afflicted turn, never expires on application.
            if (paralyzedAtTurnStart > 0 && Stacks(CombatStatus.Paralyzed) > 0) stacks[CombatStatus.Paralyzed]--;
            if (weakenedAtTurnStart > 0 && Stacks(CombatStatus.Weakened) > 0) stacks[CombatStatus.Weakened]--;
            if (exposedAtTurnStart > 0 && Stacks(CombatStatus.Exposed) > 0) stacks[CombatStatus.Exposed]--;
            paralyzedAtTurnStart = weakenedAtTurnStart = exposedAtTurnStart = 0;
        }
        public string EffectDetails()
        {
            var entries = new List<string>();
            foreach (var entry in stacks) if (entry.Value > 0) entries.Add(entry.Key + ": " + entry.Value + " stacks");
            if (Barrier > 0) entries.Add("Barrier: " + Barrier);
            return string.Join("\n", entries);
        }
    }
    public static class CombatMath
    {
        public static int Damage(GridUnit source, GridUnit target, int power, DamageKind kind, ScalingStat stat = ScalingStat.None, float scaling = 0, float multiplier = 1)
        {
            int value = stat == ScalingStat.Strength ? (source?.Strength ?? 0) : stat == ScalingStat.Magic ? (source?.Magic ?? 0) : 0;
            float raw = Math.Max(0, power) + Math.Max(0, value) * Math.Max(0, scaling);
            raw *= Mathf.Max(0, multiplier) * (source != null && source.Stacks(CombatStatus.Weakened) > 0 ? .8f : 1f);
            int defense = Math.Max(0, kind == DamageKind.Physical ? (target?.PhysicalDefense ?? 0) : (target?.MagicalDefense ?? 0));
            if (target != null && target.Stacks(CombatStatus.Exposed) > 0) defense = Mathf.RoundToInt(defense * .75f);
            int damage = Mathf.RoundToInt(raw * (100f / (100f + defense)));
            return target != null && target.EffectiveBlock ? damage / 2 : damage;
        }
        public static bool Roll(float percent, float roll) => percent >= 100 || (percent > 0 && roll < percent / 100f);
        public static bool CanPush(int strength, int resistance) => strength > resistance;
    }
}
