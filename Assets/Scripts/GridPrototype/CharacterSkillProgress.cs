using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Game.GridPrototype {
 public sealed partial class BattleCharacterInfo {
  [SerializeField] int spentSkillPoints;
  [SerializeField] List<string> learnedSkillNodes=new();
  [SerializeField] string negotiatedSkill;
  public int SkillRevision{get;private set;}
  public int SkillPoints=>Math.Max(0,Math.Max(1,Level)-1-spentSkillPoints);
  public string NegotiatedSkill=>negotiatedSkill;
  public bool KnowsNode(string id)=>!string.IsNullOrEmpty(id)&&learnedSkillNodes.Contains(id);
  public bool CanLearnNode(MenuSkillNode node){
   var data=GetComponent<CharacterMenuData>();if(!data||!data.DebtTree||node==null||!data.SkillTree.Contains(node)||node.Placeholder||node.Starting||KnowsNode(node.Id)||Level<node.RequiredLevel||SkillPoints<node.SkillPointCost)return false;
   return node.Prerequisites.All(id=>data.SkillTree.Any(n=>n.Id==id&&(n.Starting||KnowsNode(id))));
  }
  public bool LearnNode(MenuSkillNode node,string permanentSkill=null){
   if(!CanLearnNode(node))return false;
   if(node.Id=="debt-negotiated"&&(!string.IsNullOrEmpty(negotiatedSkill)||!new[]{"On Credit","Borrowed Momentum","Unfair Terms"}.Contains(permanentSkill)))return false;
   spentSkillPoints+=Math.Max(0,node.SkillPointCost);learnedSkillNodes.Add(node.Id);
   if(node.Id=="debt-negotiated")negotiatedSkill=permanentSkill;
   SkillRevision++;return true;
  }
  void CaptureSkills(SavedCharacter saved){saved.SpentSkillPoints=spentSkillPoints;saved.LearnedSkillNodes=new List<string>(learnedSkillNodes);saved.NegotiatedSkill=negotiatedSkill;}
  void RestoreSkills(SavedCharacter saved){spentSkillPoints=Math.Max(0,saved.SpentSkillPoints);learnedSkillNodes=new List<string>(saved.LearnedSkillNodes??new());negotiatedSkill=saved.NegotiatedSkill;SkillRevision++;}
  void ConfigureDebt(GridUnit unit){unit.ConsolidatedPayment=KnowsNode("debt-consolidated");unit.NegotiatedSkill=KnowsNode("debt-negotiated")?negotiatedSkill:null;}
 }
 public static class DebtSkillTree {
  public static List<MenuSkillNode> Create(){
   var nodes=new List<MenuSkillNode>();string[] names={"On Credit","Borrowed Momentum","Unfair Terms","Settle Accounts","Assign Liability"};
   for(int i=0;i<names.Length;i++)nodes.Add(new MenuSkillNode{Id="debt-start-"+i,Name=names[i],ActionName=names[i],Starting=true,Position=new Vector2(i*145,0),Description="Starting Skill."});
   void Add(string id,string name,int level,int cost,float x,float y,string description,bool passive=false,string prerequisite=null,bool placeholder=false){nodes.Add(new MenuSkillNode{Id=id,Name=name,ActionName=passive||placeholder?null:name,RequiredLevel=level,SkillPointCost=cost,Position=new Vector2(x,y),Description=description,Passive=passive,Placeholder=placeholder,Prerequisites=prerequisite==null?new():new(){prerequisite}});}
   Add("debt-loan","Loan",3,2,0,160,"Quick Skill. Spend 1 AP to gain 4 AP, capped at 9. Borrow a 3 AP repayment due in two upcoming turns; default collects 4 AP. Only one originated Loan may remain outstanding.");
   Add("debt-extension","Extension",4,2,260,160,"Quick Skill. Spend 1 AP to delay one personal contract by one turn. Each contract can be extended once, including after transfer.");
   Add("debt-consolidated","Consolidated Payment",6,2,400,320,"Waive Settle Accounts' activation fee when clearing at least two contracts. Repayment prices remain unchanged.",true,"debt-start-3");
   Add("debt-reach","Long Reach",7,2,650,160,"Increase the reach of liability-transfer Skills by one tile.",true,"debt-start-4");
   Add("debt-redistribute","Redistribute Liability",10,3,650,350,"Spend 3 AP and one action to transfer up to three personal contracts. Assign each to an enemy; multiple contracts may share a recipient.",false,"debt-start-4");
   Add("debt-negotiated","Negotiated Terms",12,3,260,510,"Permanently choose one starting offensive Skill when unlocking. Its new contracts receive one extra turn before collection.",true,"debt-extension");
   Add("debt-outstanding","Outstanding Balance",16,4,0,670,"Future Skill. Its effect and final requirements are not designed; learning is unavailable.",false,"debt-loan",true);
   return nodes;
  }
  public static List<BattleActionOption> Actions()=>new(){
   new BattleActionOption{Name="Loan",Description="Borrow AP against a future payment.",Category=BattleActionCategory.Skill,DebtSkill=DebtSkill.Loan,RequiredSkillNode="debt-loan",ActionPointCost=1,ActionPointRecovery=4,Effect=ActionEffect.Support,Damage=0,Contract=new DebtTerms{RepayAP=3,DueInTurns=2,DefaultAmount=4}},
   new BattleActionOption{Name="Extension",Description="Delay one personal contract by one turn. Each contract can be extended once.",Category=BattleActionCategory.Skill,DebtSkill=DebtSkill.Extend,RequiredSkillNode="debt-extension",ActionPointCost=1,Effect=ActionEffect.Support,Damage=0},
   new BattleActionOption{Name="Redistribute Liability",Description="Transfer up to three personal contracts to selected enemies without resetting their deadlines.",Category=BattleActionCategory.Skill,DebtSkill=DebtSkill.Redistribute,RequiredSkillNode="debt-redistribute",ActionPointCost=3,Effect=ActionEffect.Support,Damage=0,Reach=3,Pattern=BattleTargetPattern.Multiple,MaxTargets=3}
  };
 }
}
