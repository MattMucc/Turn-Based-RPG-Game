using System;
using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    string progressionSelection;
    MenuProgressionEntry shownProgressionEntry;
    readonly List<(MenuProgressionEntry entry, Button button)> progressionCards = new();

    void Progression()
    {
        progressionCards.Clear();
        float width=Mathf.Max(300,body.rect.width), tabWidth=(width-16)/3;
        var kinds=new[]{ProgressionEntryKind.Spell,ProgressionEntryKind.Weapon,ProgressionEntryKind.Gear};
        var labels=new[]{"Spells","Weapons","Gear"};
        for(int i=0;i<kinds.Length;i++) {
            var kind=kinds[i];
            var tab=Button(body,labels[i],i*(tabWidth+8),0,tabWidth,()=>{progressionKind=kind;progressionSelection=null;Rebuild();});
            if(kind==progressionKind)tab.image.color=new Color(.16f,.36f,.44f);
        }
        var data=Member.GetComponent<CharacterMenuData>();
        var entries=data&&data.Progression!=null?data.Progression.Where(e=>e!=null&&e.Kind==progressionKind).ToList():new List<MenuProgressionEntry>();
        Manager.BindEquipment();
        if(progressionKind!=ProgressionEntryKind.Spell)foreach(var copy in Manager.Equipment.Owned.Where(e=>e!=null&&e.Definition)){
            var definition=copy.Definition;
            var kind=definition.Kind==EquipmentKind.Weapon?ProgressionEntryKind.Weapon:ProgressionEntryKind.Gear;
            if(kind!=progressionKind)continue;
            entries.Add(new MenuProgressionEntry{Kind=kind,Name=definition.DisplayName,Image=definition.Image,Description=definition.Description,Equipment=definition,EquipmentCopy=copy});
        }
        // A presentation record must never hide other learned spells without records.
        if(progressionKind==ProgressionEntryKind.Spell) {
            var loadout=Loadout();
            if(loadout)foreach(var spell in loadout.Options.Where(o=>o!=null&&o.Category==BattleActionCategory.Magic&&string.IsNullOrEmpty(o.SpellId)))
                if(!entries.Any(e=>string.Equals(e.Name,spell.Name,StringComparison.OrdinalIgnoreCase)))
                    entries.Add(new MenuProgressionEntry{Kind=ProgressionEntryKind.Spell,Name=spell.Name,Description=spell.Description});
            if(loadout)foreach(var learned in loadout.LearnedSpells) {
                var spell=SpellDefinition.Find(learned.Id);if(!spell)continue;
                entries.RemoveAll(e=>e.Name==spell.DisplayName);
                var tiers=spell.OrderedTiers.ToList();var next=tiers.FirstOrDefault(t=>t.RequiredExperience>learned.Experience);
                entries.Add(new MenuProgressionEntry{Kind=ProgressionEntryKind.Spell,Name=spell.DisplayName,Image=spell.Image,Description=spell.Description+"\nMastery belongs to this character. View Abilities for tier patterns and calculated effects.",
                    Level=tiers.Count(t=>t.RequiredExperience<=learned.Experience),MaximumLevel=tiers.Count,Experience=learned.Experience,NextLevelExperience=next?.RequiredExperience??spell.MaximumExperience,
                    Milestones=tiers.Select((t,i)=>new UpgradeMilestone{Level=i+1,Name=spell.CreateAction(t).Name,Description=$"Requires {t.RequiredExperience} cumulative Spell XP · {t.Action.MagicPointCost} MP\n{t.Action.Description}"}).ToList()});
            }
        }
        Label(Rect("Collection count",body,0,48,width,28),$"{DisplayName(Member)} · {entries.Count} entries",15);
        var list=List(body,"Progression entries",0,84,width,Mathf.Max(180,body.rect.height-84),entries.Count*88);
        for(int i=0;i<entries.Count;i++) {
            var entry=entries[i]; float cardWidth=width-8;
            var card=Button(list,entry.Name??"Unnamed entry",0,i*88,cardWidth,()=>ShowEntry(entry),()=>ShowEntry(entry),80);
            var label=card.transform.Find("Label").GetComponent<TMP_Text>();
            label.text=entry.Name;label.rectTransform.anchoredPosition=new Vector2(78,-8);label.rectTransform.sizeDelta=new Vector2(cardWidth-90,26);
            label.enableAutoSizing=true;label.fontSizeMin=12;label.fontSizeMax=17;
            var art=Rect("Entry artwork",card.transform,10,10,58,58);
            art.gameObject.AddComponent<Image>().color=new Color(.025f,.06f,.09f);
            if(entry.Image){var texture=Rect("Artwork",art,0,0,58,58).gameObject.AddComponent<RawImage>();texture.texture=entry.Image;texture.raycastTarget=false;}
            else Label(Rect("Artwork placeholder",art,0,0,58,58),progressionKind==ProgressionEntryKind.Spell?"S":progressionKind==ProgressionEntryKind.Weapon?"W":"G",24).alignment=TextAlignmentOptions.Center;
            int maximum=Mathf.Max(1,entry.MaximumLevel),level=Mathf.Clamp(entry.Level,1,maximum);
            string status=entry.EquipmentCopy!=null?"Copy: "+(Manager.EquipmentOwner(entry.EquipmentCopy.InstanceId)?DisplayName(Manager.EquipmentOwner(entry.EquipmentCopy.InstanceId)):"Unequipped"):maximum==1?"Single level":level>=maximum?$"Lv {level} / {maximum} · Mastered":$"Lv {level} / {maximum} · XP {Mathf.Max(0,entry.Experience)} / {Mathf.Max(1,entry.NextLevelExperience)}";
            Label(Rect("Entry progress",card.transform,78,38,cardWidth-90,24),status,13);
            if(maximum>1) ProgressionBar(card.transform,78,67,cardWidth-94,level>=maximum?1:entry.Experience/(float)Mathf.Max(1,entry.NextLevelExperience));
            progressionCards.Add((entry,card));
        }
        if(entries.Count==0) {
            string empty=progressionKind==ProgressionEntryKind.Spell?"No spells learned yet.":progressionKind==ProgressionEntryKind.Weapon?"No weapon records available yet.":"No gear records available yet.";
            Label(Rect("Empty collection",list,16,20,width-40,90),empty,18);
            Detail(labels[(int)progressionKind],empty+"\n\nSelect a different character or collection tab. Upgrade milestones will appear here when available.");
        } else ShowEntry(entries.FirstOrDefault(e=>EntryKey(e)==progressionSelection)??entries[0]);
    }
    void RestoreProgressionSelection()
    {
        var selected=progressionCards.Find(c=>EntryKey(c.entry)==progressionSelection);
        if(selected.button)events.SetSelectedGameObject(selected.button.gameObject);
    }
    void ProgressionBar(Transform parent,float x,float y,float width,float progress)
    {
        var track=Rect("Mastery experience",parent,x,y,width,7);track.gameObject.AddComponent<Image>().color=new Color(.09f,.13f,.17f);
        var fill=Rect("Mastery fill",track,0,0,width*Mathf.Clamp01(progress),7);fill.gameObject.AddComponent<Image>().color=Accent;
    }
    float ProgressionText(Transform parent,string name,string value,float y,float width,int fontSize,Color? color=null)
    {
        var text=Label(Rect(name,parent,12,y,width,30),value,fontSize);
        if(color.HasValue)text.color=color.Value;
        float height=Mathf.Max(24,text.GetPreferredValues(value,width,0).y+6);text.rectTransform.sizeDelta=new Vector2(width,height);return y+height;
    }
    static string EntryKey(MenuProgressionEntry entry)=>entry.EquipmentCopy?.InstanceId??entry.Name;
    void ShowEntry(MenuProgressionEntry entry)
    {
        progressionSelection=EntryKey(entry);
        if(shownProgressionEntry==entry)return; // Hovering the same entry must not reset details scrolling.
        shownProgressionEntry=entry;
        foreach(var card in progressionCards)if(card.button)card.button.image.color=card.entry==entry?new Color(.16f,.36f,.44f):new Color(.08f,.17f,.25f);
        Clear(detailRoot);
        float width=detailRoot.rect.width-24;
        var name=Label(Rect("Progression name",detailRoot,12,0,width,50),entry.Name,23);
        name.enableAutoSizing=true;name.fontSizeMin=14;name.fontSizeMax=23;
        var content=List(detailRoot,"Progression details",0,56,detailRoot.rect.width,Mathf.Max(150,detailRoot.rect.height-56),300);
        width=content.rect.width-24;float y=12;
        if(entry.Image) {
            var art=Rect("Progression artwork",content,(content.rect.width-136)/2,y,136,136);
            var image=art.gameObject.AddComponent<RawImage>();image.texture=entry.Image;image.raycastTarget=false;y+=150;
        }
        y=ProgressionText(content,"Entry description",string.IsNullOrWhiteSpace(entry.Description)?"No description available yet.":entry.Description,y,width,16)+12;
        if(entry.Equipment){
            var source=new GridUnit(Member.name,Vector2Int.zero);Member.Configure(source);
            var current=Member.GetComponent<CharacterEquipment>()?.CopyAt(EquipmentSlot.Weapon);
            if(entry.Equipment.Kind==EquipmentKind.Weapon){
                source.Strength=Member.StatReplacingCopy(CharacterStat.Strength,current,entry.EquipmentCopy);
                source.Magic=Member.StatReplacingCopy(CharacterStat.Magic,current,entry.EquipmentCopy);
                y=DrawEquipmentAttacks(content,entry.Equipment,source,y,width,entry.EquipmentCopy);
            }
            y=ProgressionText(content,"Equipment progression pending","Visit Abilities to inspect all owned weapon Attacks, including locked mastery unlocks. Upgrade requirements are listed on the Upgrades page; a blacksmith performs upgrades.",y+12,width,15);
            content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(((RectTransform)content.parent).rect.height,y+12));return;
        }
        int maximum=Mathf.Max(1,entry.MaximumLevel),level=Mathf.Clamp(entry.Level,1,maximum);
        bool mastered=level>=maximum;
        string progress=maximum==1?"Single level · No mastery levels":mastered?$"Mastered · Level {level} / {maximum}":$"Level {level} / {maximum}\nXP {Mathf.Max(0,entry.Experience)} / {Mathf.Max(1,entry.NextLevelExperience)}";
        y=ProgressionText(content,"Mastery summary",progress,y,width,17,Accent);
        if(maximum>1){ProgressionBar(content,12,y,width,mastered?1:entry.Experience/(float)Mathf.Max(1,entry.NextLevelExperience));y+=22;}
        var milestones=(entry.Milestones??new List<UpgradeMilestone>()).Where(m=>m!=null&&m.Level>=1&&m.Level<=maximum).OrderBy(m=>m.Level).ToList();
        var next=milestones.FirstOrDefault(m=>m.Level>level);
        if(next!=null)y=ProgressionText(content,"Next milestone",$"Next unlock · Level {next.Level} · {next.Name}",y+8,width,16,Accent)+8;
        y=ProgressionText(content,"Milestone heading","UPGRADE MILESTONES",y+12,width,16)+4;
        if(milestones.Count==0)y=ProgressionText(content,"No milestones","No upgrade milestones available yet.",y,width,15);
        foreach(var milestone in milestones) {
            bool unlocked=milestone.Level<=level;
            string state=unlocked?"Unlocked":next!=null&&milestone.Level==next.Level?"Next":"Locked";
            var card=Rect("Milestone "+milestone.Level,content,8,y,content.rect.width-16,60);
            card.gameObject.AddComponent<Image>().color=unlocked?new Color(.065f,.16f,.15f):new Color(.055f,.09f,.14f);
            float h=ProgressionText(card,"Milestone title",$"{state} · Level {milestone.Level} · {milestone.Name}",8,width-16,16,unlocked?new Color(.5f,.9f,.7f):next!=null&&milestone.Level==next.Level?Accent:Ink);
            h=ProgressionText(card,"Milestone description",milestone.Description??"",h+4,width-16,15)+8;
            card.sizeDelta=new Vector2(card.sizeDelta.x,h);y+=h+10;
        }
        content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(((RectTransform)content.parent).rect.height,y+12));
    }
}
