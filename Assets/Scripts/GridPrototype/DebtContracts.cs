using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Game.GridPrototype {
 public enum DebtSkill { None, Borrow, Transfer, Settle }
 public enum DebtKind { Credit, Footing, Vigor }
 [Serializable] public sealed class DebtTerms {
  public DebtKind Kind;
  [Min(0)] public int RepayAP=2,RepayMovement;
  [Min(1)] public int DueInTurns=1;
  [Min(0)] public int DefaultAmount=3;
  public DebtTerms Copy()=>new DebtTerms{Kind=Kind,RepayAP=Math.Max(0,RepayAP),RepayMovement=Math.Max(0,RepayMovement),DueInTurns=Math.Max(1,DueInTurns),DefaultAmount=Math.Max(0,DefaultAmount)};
  public string Price=>RepayAP==0&&RepayMovement>0?RepayMovement+" movement":RepayAP+" AP"+(RepayMovement>0?" + "+RepayMovement+" movement":"");
  public string Penalty=>Kind==DebtKind.Credit?"Lose "+DefaultAmount+" AP; any shortfall is collected from future AP gains.":Kind==DebtKind.Footing?"Forfeit "+DefaultAmount+" points from future movement grants.":"Receive "+DefaultAmount+" stacks of Weakened.";
 }
 public sealed class DebtContract {
  public readonly string Id=Guid.NewGuid().ToString("N"),Source;
  public readonly DebtTerms Terms;
  internal int DueTurn;
  internal DebtContract(DebtTerms terms,string source,int due){Terms=terms.Copy();Source=source;DueTurn=due;}
  public int TurnsRemaining(GridUnit owner)=>Math.Max(0,DueTurn-owner.DebtTurn);
  public string Deadline(GridUnit owner)=>TurnsRemaining(owner)==0?"Due this turn":"Due in "+TurnsRemaining(owner)+" turn(s)";
  public string Details(GridUnit owner)=>Terms.Kind+" · "+Source+"\n"+Deadline(owner)+" (turn end)\nRepayment: "+Terms.Price+"\nDefault: "+Terms.Penalty;
 }
 public sealed partial class GridUnit {
  readonly List<DebtContract> contracts=new();
  public IReadOnlyList<DebtContract> Contracts=>contracts;
  public int DebtTurn{get;private set;}
  public int APCollection{get;private set;}
  public int MovementCollection{get;private set;}
  public string LastDebtNotice{get;private set;}="";
  public DebtContract Borrow(DebtTerms terms,string source){if(terms==null)throw new ArgumentNullException(nameof(terms));var c=new DebtContract(terms,source,DebtTurn+Math.Max(1,terms.DueInTurns));contracts.Add(c);return c;}
  public int GainMovement(int amount){int old=Movement;Movement=(int)Math.Min(MaximumMovement,(long)Movement+Math.Max(0,amount));return Movement-old;}
  internal int CollectAPGain(int amount){int n=Math.Min(APCollection,Math.Max(0,amount));APCollection-=n;return Math.Max(0,amount)-n;}
  internal int CollectMovementGrant(int amount){int n=Math.Min(MovementCollection,Math.Max(0,amount));MovementCollection-=n;return Math.Max(0,amount)-n;}
  internal void BeginDebtTurn(){DebtTurn++;LastDebtNotice="";}
  internal void CollectDueContracts(){
   if(Health<=0)return;
   var notices=new List<string>();
   foreach(var c in contracts.Where(c=>c.DueTurn<=DebtTurn).ToArray()){
    int amount=c.Terms.DefaultAmount;
    if(c.Terms.Kind==DebtKind.Credit){int paid=Math.Min(ActionPoints,amount);ActionPoints-=paid;APCollection=(int)Math.Min(int.MaxValue,(long)APCollection+amount-paid);}
    else if(c.Terms.Kind==DebtKind.Footing)MovementCollection=(int)Math.Min(int.MaxValue,(long)MovementCollection+amount);
    else ApplyStatus(CombatStatus.Weakened,amount);
    contracts.Remove(c);notices.Add(c.Terms.Kind+" defaulted: "+c.Terms.Penalty);
   }
   LastDebtNotice=string.Join("\n",notices);
  }
  public bool QuoteSettlement(IEnumerable<string> ids,int fee,out int ap,out int movement){
   ap=Math.Max(0,fee);movement=0;var selected=ids?.ToList();if(selected==null||selected.Count==0||selected.Distinct().Count()!=selected.Count)return false;
   foreach(var id in selected){var c=contracts.Find(c=>c.Id==id);if(c==null)return false;ap=(int)Math.Min(int.MaxValue,(long)ap+c.Terms.RepayAP);movement=(int)Math.Min(int.MaxValue,(long)movement+c.Terms.RepayMovement);}
   return true;
  }
  public bool CanSettle(IEnumerable<string> ids,int fee)=>TurnOpen&&ActionAvailable&&Health>0&&QuoteSettlement(ids,fee,out int ap,out int movement)&&ActionPoints>=ap&&Movement>=movement;
  public bool Settle(IEnumerable<string> ids,int fee){
   var selected=ids?.ToArray();if(!CanSettle(selected,fee))return false;
   QuoteSettlement(selected,fee,out int ap,out int move);if(!SpendActionWithActionPoints(ap))return false;
   if(move>0)SpendMovement(move);contracts.RemoveAll(c=>selected.Contains(c.Id));LastDebtNotice="Settled "+selected.Length+" contract(s): "+ap+" AP, "+move+" movement.";return true;
  }
  public bool TransferDebt(string id,GridUnit target,int fee){
   var c=contracts.Find(c=>c.Id==id);if(c==null||target==null||target==this||target.Health<=0||Health<=0)return false;
   if(!SpendActionWithActionPoints(fee))return false;
   int remaining=Math.Max(1,c.TurnsRemaining(this));contracts.Remove(c);c.DueTurn=target.DebtTurn+remaining;target.contracts.Add(c);
   LastDebtNotice="Transferred "+c.Terms.Kind+" to "+target.Name+".";return true;
  }
  public bool TryEnemySettlement(){
   int ap=1,move=0;var selected=new List<string>();
   foreach(var c in contracts.Where(c=>c.DueTurn<=DebtTurn).OrderBy(c=>c.Terms.RepayAP).ThenBy(c=>c.Terms.RepayMovement)){
    if((long)ap+c.Terms.RepayAP>ActionPoints||(long)move+c.Terms.RepayMovement>Movement)continue;
    ap+=c.Terms.RepayAP;move+=c.Terms.RepayMovement;selected.Add(c.Id);
   }
   return selected.Count>0&&Settle(selected,1);
  }
  public string DebtDetails(){var lines=contracts.Select(c=>c.Details(this)).ToList();if(APCollection>0)lines.Add("AP collection: "+APCollection+" from future gains");if(MovementCollection>0)lines.Add("Movement collection: "+MovementCollection+" from future grants");return string.Join("\n\n",lines);}
 }
 public static class DebtStartingKit {
  public static List<BattleActionOption> Create()=>new(){
   Offensive("On Credit","Borrow power for a heavy single-target blow.",32,1,new DebtTerms(),0),
   Offensive("Borrowed Momentum","Strike and gain up to 3 movement points.",18,1,new DebtTerms{Kind=DebtKind.Footing,RepayAP=0,RepayMovement=2,DueInTurns=2},1,3),
   Offensive("Unfair Terms","A weakening bargain exposes the target's defenses.",8,3,new DebtTerms{Kind=DebtKind.Vigor,RepayAP=1,RepayMovement=2,DueInTurns=3,DefaultAmount=2},2,0,CombatStatus.Exposed),
   new BattleActionOption{Name="Assign Liability",Description="Transfer one personal contract to an enemy, preserving its remaining deadline.",Category=BattleActionCategory.Skill,DebtSkill=DebtSkill.Transfer,ActionPointCost=2,Effect=ActionEffect.Support,Reach=3,Damage=0,FavoriteSlot=3},
   new BattleActionOption{Name="Settle Accounts",Description="Pay and clear selected personal contracts in one action.",Category=BattleActionCategory.Skill,DebtSkill=DebtSkill.Settle,ActionPointCost=1,Effect=ActionEffect.Support,Damage=0,FavoriteSlot=-1}
  };
  static BattleActionOption Offensive(string name,string description,int damage,int reach,DebtTerms terms,int favorite,int movement=0,CombatStatus status=CombatStatus.None)=>new BattleActionOption{Name=name,Description=description,Category=BattleActionCategory.Skill,ActionPointCost=2,DebtSkill=DebtSkill.Borrow,Contract=terms,MovementGranted=movement,Damage=damage,Reach=reach,Scaling=ScalingStat.Soul,StatScaling=1,InflictedStatus=status,StatusStacks=2,StatusChance=100,SignatureQte=true,QteDuration=1.5f,QteBonusDamage=8,FavoriteSlot=favorite};
 }
}
