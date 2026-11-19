using UnityEngine;
public sealed partial class BattleHud {
 [SerializeField] ReactionUILayout reactionLayout;
 bool suppressReactionLayout;
 ReactionUILayout Reactions=>suppressReactionLayout?null:reactionLayout?reactionLayout:Resources.Load<ReactionUILayout>("UI/Reactions/Reactions");
 QteUIView modularQte;
 bool BuildModularDefense(){
  if(!Reactions||!Reactions.Defense)return false;var v=Instantiate(Reactions.Defense,root,false);defensePanel=(RectTransform)v.transform;defensePanel.name="Reactive defense";
  defenseCaption=v.Caption;defensePrompt=v.Prompt;defenseResult=v.Result;dodgeWindow=v.Dodge;parryWindow=v.Parry;impactLine=v.Impact;defenseMarker=v.Marker;defensePanel.gameObject.SetActive(false);return true;
 }
 bool BuildModularQte(){
  sequenceFrames.Clear();sequenceIcons.Clear();modularQte=null;
  if(!Reactions||!Reactions.Qte)return false;modularQte=Instantiate(Reactions.Qte,root,false);qteLayer=(RectTransform)modularQte.transform;qteLayer.name="Signature QTE";
  qteCaption=modularQte.Caption;qteCountdown=modularQte.Countdown;qteKey=modularQte.Key;qteFill=modularQte.Fill;qtePrompt=modularQte.Prompt;qteIcon=modularQte.Icon;dualPrompt=modularQte.Secondary;dualIcon=modularQte.SecondaryIcon;tapRing=modularQte.Ring;triangleCue=modularQte.Triangle;qteLayer.gameObject.SetActive(false);return true;
 }
 void PlaceQtePrompt(RectTransform prompt,Game.GridPrototype.QteLane lane){
  var marker=modularQte?modularQte.Lane(lane):null;
  prompt.anchorMin=prompt.anchorMax=marker?marker.anchorMin:Game.GridPrototype.QteLayout.Anchor(lane);prompt.anchoredPosition=marker?marker.anchoredPosition:Game.GridPrototype.QteLayout.Offset(lane);
 }
}
