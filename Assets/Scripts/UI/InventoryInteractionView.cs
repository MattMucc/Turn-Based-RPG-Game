using UnityEngine;
using UnityEngine.UI;
using TMPro;
public sealed class InventoryInteractionView : MonoBehaviour {
 public TMP_Text Title,Lead,Description,Notice;
 public ScrollRect Scroll;
 public RectTransform Choices;
 public InventoryButtonView Confirm,Back;
 public void SizeContent(){
  Canvas.ForceUpdateCanvases();
  if(Choices.gameObject.activeSelf){LayoutRebuilder.ForceRebuildLayoutImmediate(Choices);Scroll.content=Choices;Choices.sizeDelta=new Vector2(Choices.sizeDelta.x,Mathf.Max(Scroll.viewport.rect.height,LayoutUtility.GetPreferredHeight(Choices)));}
  else {Scroll.content=Description.rectTransform;Description.rectTransform.sizeDelta=new Vector2(Description.rectTransform.sizeDelta.x,Mathf.Max(Scroll.viewport.rect.height,Description.GetPreferredValues(Description.text,Mathf.Max(20,Description.rectTransform.rect.width),0).y+12));}
  Scroll.verticalNormalizedPosition=1;
 }
}
