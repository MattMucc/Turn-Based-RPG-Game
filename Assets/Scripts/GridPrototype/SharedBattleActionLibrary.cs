using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    [CreateAssetMenu(menuName="Game/Shared Battle Action Library")]
    public sealed class SharedBattleActionLibrary : ScriptableObject
    {
        [Tooltip("Common attacks, skills and spells. Consumables stay in each character's carried inventory.")]
        public List<BattleActionOption> Actions = new();
        public int Revision { get; private set; }
        public void NotifyChanged() => Revision++;
        void OnValidate() => NotifyChanged();
    }
}
