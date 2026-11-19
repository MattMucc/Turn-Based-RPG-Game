using UnityEngine;
using UnityEngine.UI;
public partial class BattleMenuUI {
 [SerializeField] BattleUILayout battleLayout;
 BattleUILayout BattleLayout=>battleLayout?battleLayout:Resources.Load<BattleUILayout>("UI/Battle/Battle");
 BattleCommandView modularCommands;
 void BuildModularCommands(){
  var l=BattleLayout;if(!l||!l.Commands||!l.CommandButton)return;
  if(l.ShortcutDimmer){
   var previousDimmer=shortcutDimmer;int index=previousDimmer.transform.GetSiblingIndex();
   shortcutDimmer=Instantiate(l.ShortcutDimmer,transform,false);shortcutDimmer.transform.SetSiblingIndex(index);
   foreach(var graphic in shortcutDimmer.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
   shortcutDimmer.gameObject.SetActive(false);previousDimmer.gameObject.SetActive(false);
   if(Application.isPlaying)Destroy(previousDimmer.gameObject);else DestroyImmediate(previousDimmer.gameObject);
  }
  var old=viewport.gameObject;var oldCounter=targetCounter.transform.parent.gameObject;
  modularCommands=Instantiate(l.Commands,transform,false);scroll=modularCommands.Scroll;viewport=(RectTransform)scroll.transform;content=scroll.content;targetCounter=modularCommands.Counter;targetCounter.transform.parent.gameObject.SetActive(false);
  pool.Clear();for(int i=0;i<4;i++)pool.Add(Instantiate(l.CommandButton,content,false));attackButton=pool[0];skillsButton=pool[1];itemsButton=pool[2];runButton=pool[3];
  old.SetActive(false);oldCounter.SetActive(false);if(Application.isPlaying){Destroy(old);Destroy(oldCounter);}else{DestroyImmediate(old);DestroyImmediate(oldCounter);}
  var previous=remainingTurnButton;remainingTurnButton=Instantiate(l.CommandButton,transform,false);remainingTurnButton.name="Remaining turn";remainingTurnButton.Configure("End Turn",()=>battle.EndTurn(),true);remainingTurnButton.SetIcons(endTurnKeyboardIcon,endTurnControllerIcon);BattleUIArrangement.Copy(l.RemainingTurn,(RectTransform)remainingTurnButton.transform);remainingTurnButton.gameObject.SetActive(false);
  previous.gameObject.SetActive(false);if(Application.isPlaying)Destroy(previous.gameObject);else DestroyImmediate(previous.gameObject);
  hud.Layout=l;modularCommands.transform.SetAsLastSibling();tooltipPanel.transform.SetAsLastSibling();
 }
}
