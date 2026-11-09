using System.Collections.Generic;
namespace Game.GridPrototype
{
    public readonly struct StatusPresentation
    {
        public readonly string Name, Count, Tooltip;
        public readonly int Symbol;
        public StatusPresentation(string name,string count,string tooltip,int symbol)
        { Name=name;Count=count;Tooltip="<b>"+name+"</b>\n"+tooltip;Symbol=symbol; }
        public static List<StatusPresentation> For(GridUnit unit, bool objectClock=false)
        {
            var result=new List<StatusPresentation>();if(unit==null)return result;
            string start=objectClock?"at the start of each new battle round":"at the start of this character's turn";
            int n=unit.Stacks(CombatStatus.Burning);
            if(n>0)result.Add(new StatusPresentation("Burning",n.ToString(),$"{n} damage ticks remaining.\nDeals {unit.BurnTickDamage} damage {start}, then removes one stack. Barrier absorbs damage first.\nMore applications add remaining ticks (maximum 10).",0));
            n=unit.Stacks(CombatStatus.Regeneration);
            if(n>0)result.Add(new StatusPresentation("Regeneration",n.ToString(),$"{n} healing ticks remaining.\nRestores up to {unit.RegenerationTickHealing} health {start}, then removes one stack. Does not revive a knocked target.",1));
            n=unit.Stacks(CombatStatus.Paralyzed);
            if(n>0)result.Add(new StatusPresentation("Paralyzed",n.ToString(),$"{n} afflicted turns remaining.\nPrevents dodging, parrying and Blocking; does not skip actions or movement.\nRemoves one stack at the end of an afflicted turn if present when that turn began.",2));
            if(unit.Barrier>0)result.Add(new StatusPresentation("Barrier",unit.Barrier.ToString(),$"{unit.Barrier} / {unit.BarrierMaximum} shield points remaining.\nAbsorbs damage before health. No turn timer; expires when depleted. The badge number is shield points, not turns.",3));
            if(unit.EffectiveBlock)result.Add(new StatusPresentation("Blocking","", "Reduces incoming attack damage until the start of the next turn. Failed dodge/parry attempts retain this protection. Paralysis prevents Blocking.",4));
            foreach (var effect in new[] { CombatStatus.Weakened, CombatStatus.Exposed })
            {
                n = unit.Stacks(effect); if (n == 0) continue;
                result.Add(new StatusPresentation(effect.ToString(), n.ToString(), $"{n} afflicted turns remaining. " + (effect == CombatStatus.Weakened ? "Deals 20% less attack and spell damage." : "Physical and Magical Defense are reduced by 25%.") + "\nRemoves one stack at the end of an afflicted turn if present at its start.", effect == CombatStatus.Weakened ? 5 : 6));
            }
            foreach (DebtKind kind in System.Enum.GetValues(typeof(DebtKind)))
            {
                n = 0; var deadlines = new SortedDictionary<int, int>(); int damage = 0;
                foreach (var debt in unit.Debts) if (debt.Kind == kind) {
                    n++; damage += debt.BloodDamage; int due = System.Math.Max(0, debt.DueOn(unit) - unit.TurnsStarted);
                    deadlines[due] = deadlines.TryGetValue(due, out int count) ? count + 1 : 1;
                }
                if (n == 0) continue;
                string details = "Pending liability; badge number is debt stacks, not duration.";
                foreach (var due in deadlines) details += due.Key == 0 ? $"\n{due.Value} due at the end of this turn." : $"\n{due.Value} due at the end of turn +{due.Key}.";
                details += kind == DebtKind.Blood ? $"\nUnpaid: {damage} total damage if all collect; Barrier absorbs first." : $"\nUnpaid: one {(kind == DebtKind.Might ? "Weakened" : "Exposed")} duration stack per liability (maximum 10).";
                if (unit.UsesDebt) details += $"\nAll personal debt: {unit.DebtCount}, +{(unit.SkillDebtMultiplier - 1) * 100:0}% Skill damage. Thresholds: 3 / 6 / 9.";
                result.Add(new StatusPresentation(kind + " Debt", n.ToString(), details, 7 + (int)kind));
            }
            return result;
        }
    }
}
