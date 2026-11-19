using UnityEngine;
[CreateAssetMenu(menuName="Game/UI/Battle Swap Layout")]
public sealed class BattleSwapLayout : ScriptableObject {
 public BattleSwapView Window;
 public BattleSwapCardView Portrait;
 public Color Selected=new Color(1,.8f,.3f),Normal=new Color(.18f,.23f,.3f),Unavailable=new Color(.3f,.3f,.3f);
}
