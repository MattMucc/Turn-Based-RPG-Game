using UnityEngine;
using UnityEngine.UI;
using TMPro;
public sealed class ModularTooltipView : MonoBehaviour {
 public TMP_Text Text;
 public ScrollRect Scroll;
 public Vector2 MaximumSize=new Vector2(280,260);
 [Min(0)] public float MinimumHeight=60,VerticalPadding=20;
 public void Fit(Vector2 available){
  var root=(RectTransform)transform;float width=Mathf.Max(40,Mathf.Min(MaximumSize.x,available.x));
  root.sizeDelta=new Vector2(width,root.sizeDelta.y);Canvas.ForceUpdateCanvases();
  float textHeight=Text.GetPreferredValues(Text.text,Mathf.Max(20,Text.rectTransform.rect.width),0).y;
  float height=Mathf.Min(Mathf.Min(MaximumSize.y,available.y),Mathf.Max(MinimumHeight,textHeight+VerticalPadding));
  root.sizeDelta=new Vector2(width,Mathf.Max(20,height));Text.rectTransform.sizeDelta=new Vector2(Text.rectTransform.sizeDelta.x,textHeight+VerticalPadding);
 }
}
