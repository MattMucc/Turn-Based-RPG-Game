using UnityEngine;
using UnityEngine.UI;
[CreateAssetMenu(menuName="Game/UI/Battle UI Layout")]
public sealed class BattleUILayout:ScriptableObject {
 public BattleCommandView Commands;
 public Image ShortcutDimmer;
 public BattleMenuButton CommandButton;
 public RectTransform RemainingTurn;
 public ScrollRect TopOrder,RightOrder;
 public BattleTurnCardView Portrait;
 public RectTransform PartyStack;
 public BattleResourceView ResourceCard;
 public Vector2 OrderSpacing=new Vector2(4,4);
 [Min(0)] public float OrderPadding=4,PartySpacing=14,ExpandedWidth=220,StatusExtraHeight=18;
 public Color ActiveCard=new Color(.07f,.19f,.25f,.97f);
}
