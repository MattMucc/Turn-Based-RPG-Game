using UnityEngine;
[CreateAssetMenu(menuName="Game/UI/Defense and QTE Layout")]
public sealed class ReactionUILayout:ScriptableObject {
 public DefenseUIView Defense;
 public QteUIView Qte;
 public QteSequenceView SequenceStep;
 public Vector2 PromptSize=new Vector2(104,48),RingPromptSize=new Vector2(64,48),StickPromptSize=new Vector2(108,96);
 [Min(1)] public float SequenceSpacing=54;
 [Range(.1f,1)] public float PressedScale=.88f;
 public Color Dodge=new Color(.1f,.45f,.8f),Parry=new Color(1,.72f,.15f),ResolvedDodge=new Color(.2f,.25f,.3f),ResolvedParry=new Color(.35f,.3f,.2f);
 public Color Waiting=Color.white,Success=new Color(.3f,1,.55f),Failure=new Color(1,.25f,.2f),Countdown=new Color(1,.75f,.2f);
}
