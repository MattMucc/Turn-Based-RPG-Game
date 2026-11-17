using System;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    void ShowMember(BattleCharacterInfo member)
    {
        if(!member)return;
        var unit=new GridUnit(DisplayName(member),Vector2Int.zero); member.Configure(unit);
        if (member.StoredHealth >= 0) unit.Health = Mathf.Clamp(member.StoredHealth, 0, unit.MaxHealth);
        string xp=member.Level>=Manager.Progression.LevelCap?"Level cap reached":$"XP {Manager.Progression.ProgressFor(member)} / {Manager.Progression.Cost(Mathf.Max(1,member.Level))}";
        Detail(DisplayName(member),$"Level {member.Level}   ·   {xp}\n\nHP {unit.Health} / {unit.MaxHealth}\nMP {unit.MagicPoints} / {unit.MaxMagicPoints}\n\n{member.UnspentPoints} attribute points available\n\n"+
            string.Join("\n",Enum.GetValues(typeof(CharacterStat)).Cast<CharacterStat>().Select(s=>$"{AttributePreview.Name(s)}: {member.Stat(s)}")),Portrait(member),
            member.Level>=Manager.Progression.LevelCap?1:Manager.Progression.ProgressFor(member)/(float)Manager.Progression.Cost(Mathf.Max(1,member.Level)));
    }
    void Attributes()
    {
        float width=body.rect.width-10;
        Label(Rect("Points",body,0,0,width,42),$"{Member.UnspentPoints} ATTRIBUTE POINTS AVAILABLE",19);
        Label(Rect("Allocation help",body,0,42,width,32),"Select an attribute to preview one point before confirming.",14);
        int i=0;foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat)))
        {
            var current=stat;
            var button=Button(body,$"{AttributePreview.Name(stat)}   {Member.Stat(stat)}     +1",0,76+i++*46,width,
                ()=>{if(Member.UnspentPoints>0&&Member.Stat(current)<int.MaxValue){pendingStat=current;Rebuild();}},
                ()=>{if(!pendingStat.HasValue)PreviewAttribute(current);},38);
            if(pendingStat==current)button.image.color=new Color(.17f,.38f,.4f);
        }
        if(pendingStat.HasValue)
        {
            var stat=pendingStat.Value;
            Button(body,"Confirm +1 "+AttributePreview.Name(stat),0,422,width-140,ConfirmAttribute,()=>PreviewAttribute(stat)).interactable=Member.UnspentPoints>0&&Member.Stat(stat)<int.MaxValue;
            Button(body,"Cancel",width-130,422,130,()=>{pendingStat=null;Rebuild();});
            PreviewAttribute(stat);
        }
        else Detail("Attributes","Attribute Points improve individual stats. Awarded on level-up; separate from combat AP and Skill Points.",Portrait(Member));
    }
    void ConfirmAttribute()
    {
        if(!open||InBattle||!pendingStat.HasValue||!Member)return;
        var stat=pendingStat.Value;pendingStat=null;Member.SpendPoint(stat);Rebuild();
    }
    void PreviewAttribute(CharacterStat stat)
    {
        var before=new GridUnit(Member.name,Vector2Int.zero);Member.Configure(before);
        var after=AttributePreview.AfterPoint(Member,stat);
        string text=AttributePreview.Explanation(stat)+$"\n\n{AttributePreview.Name(stat)}: {Member.Stat(stat)} → {(long)Member.Stat(stat)+ (Member.Stat(stat)<int.MaxValue?1:0)}";
        if(stat==CharacterStat.Vitality)text+=$"\nMaximum HP: {before.MaxHealth} → {after.MaxHealth}";
        if(stat==CharacterStat.Magic)text+=$"\nMaximum MP: {before.MaxMagicPoints} → {after.MaxMagicPoints}";
        if(stat==CharacterStat.PhysicalDefense||stat==CharacterStat.MagicalDefense)
        {
            var kind=stat==CharacterStat.PhysicalDefense?DamageKind.Physical:DamageKind.Magical;
            text+=$"\n\nExample: 100-power incoming hit\nDamage taken: {CombatMath.Damage(null,before,100,kind)} → {CombatMath.Damage(null,after,100,kind)}\nBefore Block or Barrier.";
        }
        if(stat==CharacterStat.Strength||stat==CharacterStat.Magic||stat==CharacterStat.Soul)
        {
            var loadout=Loadout();var scaling=stat==CharacterStat.Strength?ScalingStat.Strength:stat==CharacterStat.Soul?ScalingStat.Soul:ScalingStat.Magic;
            var actions=loadout?loadout.Options.Where(o=>o!=null&&o.Effect==ActionEffect.Damage&&o.Scaling==scaling&&o.StatScaling>0).ToList():new System.Collections.Generic.List<BattleActionOption>();
            text+="\n\nCurrent actions · before enemy defenses";
            foreach(var action in actions)text+=$"\n{action.Name}: {action.CalculateDamage(before,null,action.Damage)} → {action.CalculateDamage(after,null,action.Damage)}";
            if(actions.Count==0)text+="\nNo currently learned actions use this scaling.";
        }
        text+="\n\n"+(Member.UnspentPoints>0?$"Cost: 1 attribute point\nRemaining after confirmation: {Member.UnspentPoints-1}":"No attribute points available.");
        Detail(AttributePreview.Name(stat),text,Portrait(Member));
    }
    BattleActionLoadout Loadout()
    {
        var value=Member.GetComponent<BattleActionLoadout>();
        if(value){value.BindInventory(Manager.Inventory);value.InitializeFavorites();}return value;
    }
}
