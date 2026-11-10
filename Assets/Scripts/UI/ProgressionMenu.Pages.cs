using System;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    void Party()
    {
        float width=body.rect.width-10, height=body.rect.height;
        Label(Rect("Team",body,0,0,width,40),"PARTY FORMATION",19);
        var active=Manager.GetBattleParty();
        var reserve=Manager.Progression.Members.Where(m=>m&&!active.Contains(m)).ToList();
        float viewHeight=height-188;
        var list=List(body,"Cast",0,44,width,viewHeight,(active.Count+reserve.Count)*88+72);
        float y=0;
        foreach(bool deployed in new[]{true,false})
        {
            var group=deployed?active:reserve;
            Label(Rect(deployed?"Active heading":"Reserve heading",list,8,y,width-24,28),
                (deployed?"ACTIVE":"RESERVES")+"   "+group.Count+" / 3",14).color=deployed?Accent:new Color(.7f,.76f,.83f);
            y+=32;
            foreach(var member in group)
            {
                int index=Manager.Progression.Members.IndexOf(member);
                var unit=new GridUnit(member.name,Vector2Int.zero);member.Configure(unit);
                if(member.StoredHealth>=0)unit.Health=Mathf.Clamp(member.StoredHealth,0,unit.MaxHealth);
                string role=deployed?(active.IndexOf(member)==0?"CAPTAIN":"ACTIVE"):"RESERVE";
                var card=Button(list,DisplayName(member),0,y,width-12,()=>{memberIndex=index;Rebuild();},()=>ShowMember(member),82);
                if(member==Member)card.image.color=new Color(.13f,.3f,.4f);
                var name=card.transform.Find("Label").GetComponent<TMPro.TMP_Text>();
                name.rectTransform.anchoredPosition=new Vector2(78,-5);name.rectTransform.sizeDelta=new Vector2(width-190,24);name.fontSize=15;
                name.enableAutoSizing=true;name.fontSizeMin=11;name.fontSizeMax=15;
                var picture=Rect("Portrait",card.transform,8,8,60,60).gameObject.AddComponent<RawImage>();picture.texture=Portrait(member);picture.raycastTarget=false;picture.color=picture.texture?Color.white:Color.clear;
                Label(Rect("Role",card.transform,width-112,7,90,22),role,11).color=role=="CAPTAIN"?new Color(1,.8f,.35f):Accent;
                Label(Rect("Vitals",card.transform,78,30,width-104,22),$"Lv {member.Level}    HP {unit.Health} / {unit.MaxHealth}    MP {unit.MagicPoints} / {unit.MaxMagicPoints}"+(unit.Health==0?"   KO":""),12);
                bool capped=member.Level>=Manager.Progression.LevelCap;
                float progress=capped?1:Manager.Progression.ProgressFor(member)/(float)Manager.Progression.Cost(Mathf.Max(1,member.Level));
                var track=Rect("XP track",card.transform,78,66,width-244,5);track.gameObject.AddComponent<Image>().color=new Color(.03f,.07f,.1f);
                Rect("XP fill",track,0,0,(width-244)*Mathf.Clamp01(progress),5).gameObject.AddComponent<Image>().color=Accent;
                Label(Rect("XP value",card.transform,width-156,55,138,22),capped?"LEVEL CAP":$"{Manager.Progression.ProgressFor(member)} / {Manager.Progression.Cost(Mathf.Max(1,member.Level))} XP",11);
                y+=88;
            }
        }
        float controls=44+viewHeight+12;
        Label(Rect("Assign heading",body,0,controls,width,32),Manager.ReserveParty.Contains(Member)?"Replace an active member with "+DisplayName(Member)+":" : "Select a reserve to replace an active member.",14);
        for(int i=0;i<3;i++)
        {
            int slot=i;float buttonWidth=(width-16)/3;
            string occupant=i<active.Count?DisplayName(active[i]):"Empty";
            var button=Button(body,$"{i+1}: {occupant}",i*(buttonWidth+8),controls+38,buttonWidth,()=>{Manager.TrySetActiveMember(slot,Member);Rebuild();},
                ()=>Detail("Replace active member",slot<active.Count?DisplayName(Member)+" replaces "+occupant+". The outgoing member joins reserves.":"No active slot available."));
            button.interactable=Member&&Manager.ReserveParty.Contains(Member)&&slot<active.Count;
            var label=button.GetComponentInChildren<TMPro.TMP_Text>();label.enableAutoSizing=true;label.fontSizeMin=10;label.fontSizeMax=15;
        }
        Button(body,"Make "+DisplayName(Member)+" the captain",0,controls+86,width,()=>{Manager.TrySetCaptain(Member);Rebuild();}).interactable=Member&&Manager.BattleParty.IndexOf(Member)>0;
        ShowMember(Member);
    }
    void ShowMember(BattleCharacterInfo member)
    {
        if(!member)return;
        var unit=new GridUnit(DisplayName(member),Vector2Int.zero); member.Configure(unit);
        if (member.StoredHealth >= 0) unit.Health = Mathf.Clamp(member.StoredHealth, 0, unit.MaxHealth);
        string xp=member.Level>=Manager.Progression.LevelCap?"Level cap reached":$"XP {Manager.Progression.ProgressFor(member)} / {Manager.Progression.Cost(Mathf.Max(1,member.Level))}";
        Detail(DisplayName(member),$"Level {member.Level}   ·   {xp}\n\nHP {unit.Health} / {unit.MaxHealth}\nMP {unit.MagicPoints} / {unit.MaxMagicPoints}\n\n{member.UnspentPoints} attribute points available\n\n"+
            string.Join("\n",Enum.GetValues(typeof(CharacterStat)).Cast<CharacterStat>().Select(s=>$"{s}: {member.Stat(s)}")),Portrait(member),
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
            var button=Button(body,$"{AttributePreview.Name(stat)}   {Member.Stat(stat)}     +1",0,84+i++*54,width,
                ()=>{if(Member.UnspentPoints>0&&Member.Stat(current)<int.MaxValue){pendingStat=current;Rebuild();}},
                ()=>{if(!pendingStat.HasValue)PreviewAttribute(current);},44);
            if(pendingStat==current)button.image.color=new Color(.17f,.38f,.4f);
        }
        if(pendingStat.HasValue)
        {
            var stat=pendingStat.Value;
            Button(body,"Confirm +1 "+AttributePreview.Name(stat),0,422,width-140,ConfirmAttribute,()=>PreviewAttribute(stat)).interactable=Member.UnspentPoints>0&&Member.Stat(stat)<int.MaxValue;
            Button(body,"Cancel",width-130,422,130,()=>{pendingStat=null;Rebuild();});
            PreviewAttribute(stat);
        }
        else Detail("Attributes","Hover or select an attribute to see what one point would change.\n\nNo points are spent until you confirm.\n\nThese points are separate from combat SP and future Training Points.",Portrait(Member));
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
        if(stat==CharacterStat.Strength||stat==CharacterStat.Magic)
        {
            var loadout=Loadout();var scaling=stat==CharacterStat.Strength?ScalingStat.Strength:ScalingStat.Magic;
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
    void Inventory()
    {
        Label(Rect("Inventory title",body,0,0,500,44),"SHARED ITEMS",20);
        var items=Manager.Inventory.Items.Where(x=>x!=null).ToList(); var list=List(body,"Items",0,54,500,448,items.Count*48);
        for(int i=0;i<items.Count;i++){var item=items[i];Button(list,$"{item.Name}   ×{item.Quantity}",0,i*48,486,()=>Detail(item.Name,item.Tooltip),()=>Detail(item.Name,item.Tooltip),44);}
        Detail("Party inventory","Items are shared by the entire party.\n\nUse consumables from the Items command in battle.");
    }
    void Progression()
    {
        var kinds=new[]{ProgressionEntryKind.Spell,ProgressionEntryKind.Weapon,ProgressionEntryKind.Gear};var labels=new[]{"Spells","Weapons","Gear"};
        for(int i=0;i<3;i++){var kind=kinds[i];Button(body,labels[i],i*166,0,158,()=>{progressionKind=kind;Rebuild();});}
        var data=Member.GetComponent<CharacterMenuData>();
        var entries=data?data.Progression.Where(e=>e!=null&&e.Kind==progressionKind).ToList():new System.Collections.Generic.List<MenuProgressionEntry>();
        var list=List(body,"Progression entries",0,60,500,440,entries.Count*48);
        for(int i=0;i<entries.Count;i++){var entry=entries[i];Button(list,entry.Name+(entry.MaximumLevel>1?$"   Lv {entry.Level}":""),0,i*48,486,()=>ShowEntry(entry),()=>ShowEntry(entry),44);}
        if(entries.Count==0 && progressionKind==ProgressionEntryKind.Spell)
        {
            var loadout=Loadout();var spells=loadout?loadout.Options.Where(x=>x!=null&&x.Category==BattleActionCategory.Magic).ToList():new System.Collections.Generic.List<BattleActionOption>();
            list.sizeDelta=new Vector2(list.sizeDelta.x,Mathf.Max(440,spells.Count*48));
            for(int i=0;i<spells.Count;i++){var spell=spells[i];Button(list,spell.Name,0,i*48,486,()=>Detail(spell.Name,spell.Tooltip),()=>Detail(spell.Name,spell.Tooltip),44);}
        }
        if (entries.Count > 0) ShowEntry(entries[0]);
        else Detail(labels[(int)progressionKind],"No upgrade milestones available yet.\n\nOwned entries and their future bonuses will appear here as your collection grows.");
    }
    void ShowEntry(MenuProgressionEntry entry)
    {
        bool leveled=entry.MaximumLevel>1; bool mastered=entry.Level>=entry.MaximumLevel;
        string progress=!leveled?"":mastered?"Mastered":$"Level {entry.Level} / {entry.MaximumLevel}   ·   XP {entry.Experience} / {entry.NextLevelExperience}";
        string milestones=string.Join("\n\n",entry.Milestones.OrderBy(x=>x.Level).Select(m=>$"{(m.Level<=entry.Level?"Unlocked":m.Level==entry.Level+1?"Next":"Locked")} · Level {m.Level} · {m.Name}\n{m.Description}"));
        Detail(entry.Name,entry.Description+"\n\n"+progress+"\n\n"+milestones,entry.Image,!leveled?-1:mastered?1:entry.Experience/(float)Mathf.Max(1,entry.NextLevelExperience));
    }
    void SkillTree()
    {
        Label(Rect("Tree title",body,0,0,500,48),DisplayName(Member)+" · SKILLS",18);
        var data=Member.GetComponent<CharacterMenuData>(); var loadout=Loadout();
        if(!data||data.SkillTree.Count==0){Detail("Character Skill Tree","This character's Skill Tree has not been defined yet.\n\nUnique Skills will use Training Points, separately from the SP spent in combat.");return;}
        var tree=List(body,"Skill tree",0,58,500,444,Mathf.Max(444,data.SkillTree.Max(n=>n.Position.y)+90));
        foreach(var node in data.SkillTree)
        {
            foreach(var prerequisite in node.Prerequisites)
            {
                var parent=data.SkillTree.Find(n=>n.Id==prerequisite); if(parent==null)continue;
                Vector2 from=new Vector2(Mathf.Clamp(parent.Position.x,28,450),parent.Position.y+28),to=new Vector2Int((int)Mathf.Clamp(node.Position.x,28,450),(int)node.Position.y+28);
                var line=Rect("Connection",tree,from.x,from.y,(to-from).magnitude,2);line.pivot=new Vector2(0,.5f);line.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg);line.gameObject.AddComponent<Image>().color=new Color(.5f,.43f,.25f);
            }
        }
        foreach(var node in data.SkillTree)
        {
            var n=node;bool learned=loadout&&loadout.Options.Exists(o=>o.Name==n.ActionName&&!string.IsNullOrEmpty(n.ActionName));
            Action preview=()=>Detail(n.Name,n.Description+$"\n\nRequired level: {n.RequiredLevel}\nTraining Points: {n.TrainingPointCost}\n"+(learned?"Learned":"Not learned"),n.Icon);
            var b=Button(tree,n.Passive?"◇":"●",Mathf.Clamp(n.Position.x,28,450)-26,n.Position.y,52,preview,preview,52);
            b.image.color=learned?new Color(.48f,.38f,.13f):new Color(.13f,.19f,.24f);
        }
        Detail("Skill Tree","Select a node to inspect its requirements and effects.\n\nLearning will become available with the character kits.");
    }
    void Settings()
    {
        Label(Rect("Settings title",body,0,0,500,48),"AUDIO & CONTROLS",20);
        Button(body,$"Master volume: {Mathf.RoundToInt(AudioListener.volume*100)}%   −",0,60,366,()=>SetVolume(-.1f));
        Button(body,"+",380,60,110,()=>SetVolume(.1f));
        Button(body,"Display: "+(Screen.fullScreen?"Fullscreen":"Windowed"),0,112,490,()=>{Screen.fullScreen=!Screen.fullScreen;Rebuild();});
        Button(body,"Controller icons: "+(Manager.Input?Manager.Input.ControllerIcons.ToString():"Auto"),0,164,490,()=>{
            if(Manager.Input){Manager.Input.ControllerIcons=(ControllerIconStyle)(((int)Manager.Input.ControllerIcons+1)%3);PlayerPrefs.SetInt("PlayerMenu.Icons",(int)Manager.Input.ControllerIcons);PlayerPrefs.Save();}Rebuild();});
        var defense=Member.GetComponent<DefensePrototypeSettings>();
        Button(body,"Defense timing: "+(defense&&defense.Assisted?"Assisted":"Standard"),0,216,490,()=>{
            bool assisted=!(defense&&defense.Assisted);foreach(var member in Manager.Progression.Members){var d=member.GetComponent<DefensePrototypeSettings>();if(d)d.Assisted=assisted;}
            PlayerPrefs.SetInt("PlayerMenu.Assisted",assisted?1:0);PlayerPrefs.Save();Rebuild();});
        Detail("Settings","Changes take effect immediately.\n\nAssisted timing widens dodge/parry windows for the party.\n\nController icons can follow the detected controller or use your preferred style.\n\nVolume, icon style and timing assistance are remembered between sessions.");
    }
    void SetVolume(float delta){AudioListener.volume=Mathf.Clamp01(AudioListener.volume+delta);PlayerPrefs.SetFloat("PlayerMenu.Volume",AudioListener.volume);PlayerPrefs.Save();Rebuild();}
    void Start()
    {
        if(!Manager)return;
        if(Manager.Input&&PlayerPrefs.HasKey("PlayerMenu.Icons"))Manager.Input.ControllerIcons=(ControllerIconStyle)Mathf.Clamp(PlayerPrefs.GetInt("PlayerMenu.Icons"),0,2);
        if(PlayerPrefs.HasKey("PlayerMenu.Assisted"))foreach(var member in Manager.Progression.Members){var d=member.GetComponent<DefensePrototypeSettings>();if(d)d.Assisted=PlayerPrefs.GetInt("PlayerMenu.Assisted")!=0;}
    }
}
