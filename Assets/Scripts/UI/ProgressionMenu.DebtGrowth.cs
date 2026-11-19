using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
public sealed partial class ProgressionMenu {
 readonly List<GameObject> growthControls=new();
 void ClearGrowthControls(){foreach(var obj in growthControls)if(obj){buttons.RemoveAll(b=>b&&b.transform.IsChildOf(obj.transform));obj.SetActive(false);if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}growthControls.Clear();}
 bool ShowDebtGrowth(MenuSkillNode node,string pending=null){
  var data=Member.GetComponent<CharacterMenuData>();if(!data||!data.DebtTree)return false;
  bool known=node.Starting||Member.KnowsNode(node.Id);
  string text=node.Description+"\n\n"+(node.Placeholder?"Not yet designed · unavailable":known?"Learned":"Locked")+"\nRequired level: "+node.RequiredLevel+"\nSkill Point cost: "+node.SkillPointCost+"\nAvailable Skill Points: "+Member.SkillPoints;
  if(node.Prerequisites.Count>0)text+="\nPrerequisites: "+string.Join(", ",node.Prerequisites.Select(id=>data.SkillTree.FirstOrDefault(n=>n.Id==id)?.Name??id));
  if(node.Id=="debt-negotiated"&&known)text+="\nCurrent terms: "+(string.IsNullOrEmpty(Member.NegotiatedSkill)?"Not selected":Member.NegotiatedSkill);
  if(pending!=null)text+="\n\nPermanent choice: "+pending+"\nThis choice cannot be changed.";
  Detail(node.Name,text,node.Icon);
  int rows=known||node.Placeholder?0:node.Id=="debt-negotiated"?(pending==null?3:2):1;
  if(rows==0)return true;
  var viewport=(RectTransform)detailContent.parent;viewport.sizeDelta=new Vector2(viewport.sizeDelta.x,Mathf.Max(80,viewport.sizeDelta.y-rows*44));
  void Choice(string name,int row,System.Action action,bool enabled){
   var template=GrowthLayout?GrowthLayout.Action:null;UnityEngine.UI.Button button;
   if(template){var view=Instantiate(template,detailRoot,false);var rect=(RectTransform)view.transform;rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.pivot=Vector2.zero;rect.offsetMin=new Vector2(12,8+row*44);rect.offsetMax=new Vector2(-12,46+row*44);view.Label.text=name;button=view.Select;button.name=name;button.onClick.AddListener(()=>action());buttons.Add(button);growthControls.Add(view.gameObject);}
   else {button=Button(detailRoot,name,12,detailRoot.rect.height-46-row*44,detailRoot.rect.width-24,action);growthControls.Add(button.gameObject);}
   button.interactable=enabled;
  }
  if(node.Id!="debt-negotiated")Choice("Learn · "+node.SkillPointCost+" Skill Points",0,()=>{if(!InBattle&&Member.LearnNode(node))Rebuild();},Member.CanLearnNode(node));
  else if(pending!=null){
   Choice("Confirm "+pending+" · "+node.SkillPointCost+" Skill Points",1,()=>{if(!InBattle&&Member.LearnNode(node,pending))Rebuild();},Member.CanLearnNode(node));
   Choice("Cancel",0,()=>ShowSkillNode(node),true);
  }else {string[] names={"On Credit","Borrowed Momentum","Unfair Terms"};for(int i=0;i<names.Length;i++){string name=names[i];Choice(name,2-i,()=>{ClearGrowthControls();ShowDebtGrowth(node,name);},Member.CanLearnNode(node));}}
  return true;
 }
}
