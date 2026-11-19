using UnityEngine;
[CreateAssetMenu(menuName="Game/UI/Target and Examine Layout")]
public sealed class TargetUILayout : ScriptableObject {
 public TargetDetailsView Target;
 public TargetInspectionView Inspection;
 public StatusBadgeView StatusBadge;
 public StatusTooltipView StatusTooltip;
 public Vector2 RightOrderOffset=new Vector2(0,68);
}
