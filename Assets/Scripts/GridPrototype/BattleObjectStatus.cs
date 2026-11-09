using UnityEngine;
namespace Game.GridPrototype
{
    // Deliberate opt-in for destructible scenery. This is not a living combatant.
    [RequireComponent(typeof(BattleDestructible))]
    public sealed class BattleObjectStatus : MonoBehaviour
    {
        public bool Flammable = true;
        [Min(0)] public int BurningDamagePerRound = 5;
        GridUnit state;
        public GridUnit State
        {
            get
            {
                var prop=GetComponent<BattleDestructible>();
                if(state==null)state=new GridUnit(name,Vector2Int.zero);
                state.Health=prop.Health;state.MaxHealth=prop.MaxHealth;state.BurnTickDamage=BurningDamagePerRound;
                return state;
            }
        }
        public void ApplyStatus(CombatStatus status,int stacks)
        { if(Flammable && status==CombatStatus.Burning)State.ApplyStatus(status,stacks); }
        public void Extinguish() { if(state!=null)state.RemoveStatus(CombatStatus.Burning); }
        public void TickRound()
        {
            var prop=GetComponent<BattleDestructible>();if(!prop.Alive)return;
            var current=State;int before=current.Health;
            current.BeginTurn(0);current.EndTurn();
            prop.TakeDamage(before-current.Health);
        }
    }
}
