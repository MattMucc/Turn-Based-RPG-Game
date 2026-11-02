using UnityEngine;

namespace Game.GridPrototype
{
    public enum BattleHoverKind { Automatic, Ally, Hostile, Interactable, Dangerous }

    // Presentation metadata only; does not add interaction, explosion or AI behavior.
    public sealed class BattleHoverInfo : MonoBehaviour
    {
        public BattleHoverKind Kind;
        public string DisplayName;
    }
}
