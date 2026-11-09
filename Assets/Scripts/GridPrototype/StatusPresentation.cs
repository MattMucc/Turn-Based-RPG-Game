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
            return result;
        }
    }
}
