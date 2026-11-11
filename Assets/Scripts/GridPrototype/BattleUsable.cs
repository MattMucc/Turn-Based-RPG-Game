using UnityEngine;
using UnityEngine.Events;
namespace Game.GridPrototype
{
    [RequireComponent(typeof(BattleHoverInfo))]
    public sealed class BattleUsable : MonoBehaviour
    {
        public bool AllowDuringCombat = true;
        [Min(1)] public int Reach = 1;
        public bool CostsAction = true;
        public bool OneUseOnly;
        public UnityEvent OnUse = new UnityEvent();
        bool used;
        public bool Used => used;
        public void RestoreUsed(bool value){used=value;}
        public bool Available => isActiveAndEnabled && AllowDuringCombat && (!OneUseOnly || !used);
        public bool TryUse() { if(!Available)return false; used=true; OnUse.Invoke(); return true; }
    }
}
