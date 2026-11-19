using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.GridPrototype;
public sealed class OverheadCharacterView : MonoBehaviour {
 public TMP_Text Name,Level,Health,BarrierText;
 public RectTransform HealthFill,Barrier,BarrierFill;
 public BattleStatusRow Statuses;
 public void Initialize(TMP_FontAsset font){if(Statuses)Statuses.Initialize(null,font);}
 public void Present(string name,int level,GridUnit unit){
  Name.text=name;Level.text="Lv "+(level>0?level.ToString():"?");Health.text=unit.Health+" / "+unit.MaxHealth;
  HealthFill.anchorMax=new Vector2(Mathf.Clamp01(unit.Health/(float)Mathf.Max(1,unit.MaxHealth)),1);
  Barrier.gameObject.SetActive(unit.Barrier>0);BarrierFill.anchorMax=new Vector2(unit.Barrier/(float)Mathf.Max(1,unit.BarrierMaximum),1);
  if(BarrierText)BarrierText.text=unit.Barrier+" / "+unit.BarrierMaximum;
  if(Statuses)Statuses.Show(unit);
  // Overheads never intercept world targeting or defensive input.
  foreach(var graphic in GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
 }
}
