using UnityEngine;
[CreateAssetMenu(menuName="Game/UI/Tooltips and Controller Hints")]
public sealed class TooltipHintsLayout : ScriptableObject {
 public ModularTooltipView BattleAction,InventoryHover;
 public RectTransform Cursor;
 public ControllerGuideView Guide;
 public static TooltipHintsLayout Default=>Resources.Load<TooltipHintsLayout>("UI/Hints/Hints");
}
