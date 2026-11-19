using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class QteUIView:MonoBehaviour {
 public TMP_Text Caption,Countdown,Key;
 public RectTransform Fill,Prompt,Secondary,LeftLane,RightLane,BottomLane;
 public Image Icon,SecondaryIcon;
 public QteProgressRing Ring;
 public QteTriangleCue Triangle;
 public RectTransform Lane(Game.GridPrototype.QteLane lane)=>lane==Game.GridPrototype.QteLane.Left?LeftLane:lane==Game.GridPrototype.QteLane.Right?RightLane:BottomLane;
}
