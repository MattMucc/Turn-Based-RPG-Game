using UnityEngine;

namespace Game.GridPrototype
{
    public enum BattleControllerMode { Cursor, FreeFlight, Navigation }
    public enum BattleControllerArea { Commands, TurnOrder, Battlefield, Examine }
    public sealed class BattleNavigationState
    {
        public BattleControllerMode Mode { get; private set; } = BattleControllerMode.Cursor;
        public BattleControllerArea Area { get; private set; } = BattleControllerArea.Commands;
        public static Vector2 FilterStick(Vector2 value)
        {
            float magnitude = value.magnitude;
            return magnitude <= 0.25f ? Vector2.zero : value.normalized * Mathf.Clamp01((magnitude - 0.25f) / 0.75f);
        }
        public void ToggleFlight() { Mode = Mode == BattleControllerMode.FreeFlight ? BattleControllerMode.Cursor : BattleControllerMode.FreeFlight; }
        public void UseCursor() { if (Mode != BattleControllerMode.FreeFlight) Mode = BattleControllerMode.Cursor; }
        public void Navigate(BattleControllerArea area) { Mode = BattleControllerMode.Navigation; Area = area; }
    }
}
