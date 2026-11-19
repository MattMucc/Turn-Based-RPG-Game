using UnityEngine;
using UnityEngine.UI;
using TMPro;
public sealed class BattleSwapCardView : MonoBehaviour {
 public Button Button;
 public RawImage Portrait;
 public Image Highlight;
 public TMP_Text Name,Health;
 public RectTransform HealthFill;
 public void Present(string name,Texture portrait,int health,int maximum,bool available,bool selected,BattleSwapLayout layout){
  Name.text=name;Portrait.texture=portrait;Health.text=health+" / "+maximum;
  HealthFill.anchorMax=new Vector2(Mathf.Clamp01(health/(float)Mathf.Max(1,maximum)),1);
  Button.interactable=available;Highlight.color=!available?layout.Unavailable:selected?layout.Selected:layout.Normal;
  Portrait.color=health<=0?layout.Unavailable:portrait?Color.white:new Color(.12f,.18f,.24f);
 }
}
