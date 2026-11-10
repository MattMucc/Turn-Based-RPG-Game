using System;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    void Party()
    {
        Label(Rect("Team",body,0,0,500,46),"ACTIVE PARTY & RESERVES",19);
        var members=Manager.Progression.Members;
        var list=List(body,"Cast",0,50,500,300,members.Count*48);
        for(int i=0;i<members.Count;i++)
        {
            int index=i; var member=members[i]; if(!member)continue;
            int slot=Manager.BattleParty.IndexOf(member);
            string tag=slot<0?"Reserve":slot==0?"Captain":"Active";
            Button(list,$"{DisplayName(member)}   Lv {member.Level}   · {tag}",0,i*48,486,()=>{memberIndex=index;Rebuild();},()=>ShowMember(member),44);
        }
        Label(Rect("Assign heading",body,0,366,500,32),"Place selected character in active slot:",15);
        for(int i=0;i<3;i++)
        {
            int slot=i;
            var b=Button(body,slot==0?"1 · Captain":$"{slot+1}",i*166,400,158,()=>{Manager.TrySetActiveMember(slot,Member);Rebuild();});
            b.interactable=Member && Manager.ReserveParty.Contains(Member) && slot<Manager.BattleParty.Count;
        }
        Button(body,"Make selected member captain",0,450,490,()=>{Manager.TrySetCaptain(Member);Rebuild();}).interactable=Member && Manager.BattleParty.IndexOf(Member)>0;
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
        Label(Rect("Points",body,0,0,500,48),$"{Member.UnspentPoints} ATTRIBUTE POINTS",20);
        int i=0;foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat)))
        {
            var current=stat;
            Button(body,$"{stat}   {Member.Stat(stat)}     +",0,60+i++*56,490,()=>{Member.SpendPoint(current);Rebuild();},()=>ShowMember(Member),44).interactable=Member.UnspentPoints>0;
        }
        ShowMember(Member);
    }
    BattleActionLoadout Loadout()
    {
        var value=Member.GetComponent<BattleActionLoadout>();
        if(value){value.BindInventory(Manager.Inventory);value.InitializeFavorites();}return value;
    }
    void Categories()
    {
        var categories=new[]{BattleActionCategory.Attack,BattleActionCategory.Skill,BattleActionCategory.Magic,BattleActionCategory.Item};
        var labels=new[]{"Attacks","Skills","Spells","Items"};
        for(int i=0;i<4;i++){var c=categories[i];Button(body,labels[i],i*125,0,120,()=>{category=c;assigning=null;Rebuild();});}
    }
    string Binding(int slot) => $"{slot+1} / "+ControllerGlyphs.Adapt("LT")+" + "+(ControllerGlyphs.PlayStation?new[]{"Square","Triangle","Cross","Circle"}[slot]:new[]{"X","Y","A","B"}[slot]);
    void Shortcuts()
    {
        Categories();var loadout=Loadout();
        if(!loadout){Detail("No abilities","This character has no available abilities.");return;}
        if(assigning!=null)
        {
            Detail(assigning.Name,assigning.Tooltip+"\n\nChoose a shortcut slot. Other abilities remain available in battle.");
            for(int i=0;i<4;i++)
            {
                int slot=i;Button(body,Binding(slot)+"   ·   "+(loadout.Favorite(category,slot)?.Name??"Empty"),0,70+i*54,490,()=>{loadout.AssignFavorite(assigning,slot);assigning=null;Rebuild();});
            }
            Button(body,"Remove shortcut",0,302,490,()=>{loadout.AssignFavorite(assigning,-1);assigning=null;Rebuild();});
            Button(body,"Back",0,356,490,()=>{assigning=null;Rebuild();});return;
        }
        var options=loadout.Options.Where(o=>o!=null&&o.Category==category).ToList();
        var list=List(body,"Abilities",0,60,500,440,options.Count*48);
        for(int i=0;i<options.Count;i++)
        {
            var option=options[i];
            Button(list,option.Name+(option.FavoriteSlot>=0?"   ["+Binding(option.FavoriteSlot)+"]":""),0,i*48,486,
                ()=>{assigning=option;Rebuild();},()=>Detail(option.Name,option.Tooltip),44);
        }
        Detail("Favorite shortcuts",options.Count==0?"No abilities learned in this category.":"Select an ability, then assign it to one of four shortcuts.\n\nKeyboard: 1–4\nController: hold "+ControllerGlyphs.Adapt("LT")+" and press a face button.\n\nAssignments are separate for each character and category. Items use shared party stock.");
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
