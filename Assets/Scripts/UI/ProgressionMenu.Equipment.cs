using System;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    EquipmentSlot equipmentSlot;
    bool equipmentPending;
    string pendingEquipment, equipmentNotice;
    static string SlotName(EquipmentSlot slot) => slot==EquipmentSlot.AccessoryOne?"Accessory 1":slot==EquipmentSlot.AccessoryTwo?"Accessory 2":slot.ToString();
    void EquipmentPage()
    {
        Manager.BindEquipment();var profile=Member.GetComponent<CharacterEquipment>();
        if(!profile){Detail("Equipment","No equipment profile is configured for this character.");return;}
        float width=body.rect.width;
        int i=0;foreach(EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot))){
            var captured=slot;var item=profile.At(slot);
            var button=Button(body,SlotName(slot)+": "+(item?item.DisplayName:"Empty"),0,i++*44,width,()=>{equipmentSlot=captured;equipmentPending=false;equipmentNotice=null;Rebuild();},null,38);
            if(slot==equipmentSlot)button.image.color=new Color(.16f,.36f,.44f);
        }
        Label(Rect("Weapon access",body,0,180,width,48),"Weapon types: "+string.Join(", ",profile.AllowedWeaponTypes),14);
        var available=Manager.Equipment.Owned.Where(e=>e!=null&&e.Definition&&e.Definition.Fits(equipmentSlot)).ToList();
        float top=230, bottom=equipmentPending?108:40;
        var list=List(body,"Owned equipment",0,top,width,Mathf.Max(100,body.rect.height-top-bottom),(available.Count+1)*48);
        Button(list,"Unequip "+SlotName(equipmentSlot),0,0,width-12,()=>StageEquipment(null),()=>{if(!equipmentPending)PreviewEquipment(null);});
        i=1;foreach(var owned in available){
            var entry=owned;var owner=Manager.EquipmentOwner(entry.InstanceId);
            string suffix=owner?" · "+DisplayName(owner):!profile.Accepts(equipmentSlot,entry.Definition)?" · incompatible":"";
            Button(list,entry.Definition.DisplayName+suffix,0,i++*48,width-12,()=>StageEquipment(entry.InstanceId),()=>{if(!equipmentPending)PreviewEquipment(entry.InstanceId);});
        }
        if(equipmentPending){
            Button(body,"Confirm equipment",0,body.rect.height-92,width-120,()=>{
                if(!open||InBattle)return;
                Manager.TryEquip(Member,equipmentSlot,pendingEquipment,out equipmentNotice);equipmentPending=false;Rebuild();
            });
            Button(body,"Cancel",width-112,body.rect.height-92,112,()=>{equipmentPending=false;Rebuild();});
        }
        Label(Rect("Equipment notice",body,0,body.rect.height-40,width,40),equipmentNotice??"Select an item to compare, then confirm. Changes are outside combat only.",13);
        PreviewEquipment(equipmentPending?pendingEquipment:profile.InstanceAt(equipmentSlot));
    }
    void StageEquipment(string id)
    {
        var profile=Member.GetComponent<CharacterEquipment>();var item=Manager.Equipment.Find(id);var owner=Manager.EquipmentOwner(id);
        if(item!=null&&(!profile.Accepts(equipmentSlot,item.Definition)||(owner&&(owner!=Member||profile.InstanceAt(equipmentSlot)!=id)))){
            equipmentNotice=owner?"Unequip this copy from its current slot first.":"This weapon type is not available to this character.";Rebuild();return;
        }
        pendingEquipment=id;equipmentPending=true;Rebuild();
    }
    void PreviewEquipment(string id)
    {
        for(int i=detailContent.childCount-1;i>=0;i--){var child=detailContent.GetChild(i);if(child==detailText.transform)continue;child.gameObject.SetActive(false);if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);}
        var profile=Member.GetComponent<CharacterEquipment>();var candidate=Manager.Equipment.Find(id)?.Definition;var current=profile.CopyAt(equipmentSlot);
        string text=candidate?candidate.Description:"An empty weapon slot provides Punch and Push.";
        text+="\n\nSTATS · current → equipped";
        foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat))){
            int before=Member.Stat(stat), after=Member.StatReplacingCopy(stat,current,Manager.Equipment.Find(id));
            text+=$"\n{AttributePreview.Name(stat)}: {before} → {after}";
        }
        text+="\n\nHP/MP are not restored. Lower maximum pools clamp current values. Removed weapon shortcuts become empty.";
        Detail(candidate?candidate.DisplayName:"Unequip",text,candidate?candidate.Image:null);
        if(candidate&&candidate.Kind==EquipmentKind.Weapon){
            var source=new GridUnit(Member.name,Vector2Int.zero);Member.Configure(source);
            source.Soul=Member.StatReplacingCopy(CharacterStat.Soul,current,Manager.Equipment.Find(id));
            source.Strength=Member.StatReplacingCopy(CharacterStat.Strength,current,Manager.Equipment.Find(id));
            source.Magic=Member.StatReplacingCopy(CharacterStat.Magic,current,Manager.Equipment.Find(id));
            float y=detailText.rectTransform.sizeDelta.y+24;
            y=DrawEquipmentAttacks(detailContent,candidate,source,y,376,Manager.Equipment.Find(id));
            detailContent.sizeDelta=new Vector2(detailContent.sizeDelta.x,y+12);
        }
    }
    float DrawEquipmentAttacks(Transform parent,EquipmentDefinition definition,GridUnit source,float y,float width,OwnedEquipment copy=null)
    {
        if(copy!=null){
            int maximum=WeaponMastery.MaximumExperience(definition);
            var next=WeaponMastery.Milestones(definition).FirstOrDefault(m=>m.RequiredExperience>copy.MasteryExperience);
            string summary=maximum==0?"No mastery track":$"Mastery level {WeaponMastery.Level(copy)} · {copy.MasteryExperience} / {maximum} XP"+(next==null?" · Mastered":"");
            y=ProgressionText(parent,"Weapon mastery",summary,y,width,16,Accent);
            if(maximum>0){ProgressionBar(parent,12,y,width,copy.MasteryExperience/(float)maximum);y+=20;}
            foreach(var milestone in WeaponMastery.Milestones(definition))
                y=ProgressionText(parent,"Mastery unlock",(copy.MasteryExperience>=milestone.RequiredExperience?"Unlocked":"Locked")+$" · {milestone.RequiredExperience} XP · {milestone.Name}\n{milestone.Description}",y,width,14);
        }
        y=ProgressionText(parent,"Weapon attacks","AVAILABLE ATTACKS · while equipped",y,width,16,Accent);
        foreach(var attack in copy!=null?WeaponMastery.Attacks(copy):definition.Attacks.Where(a=>a!=null&&a.Category==BattleActionCategory.Attack)){
            y=ProgressionText(parent,"Attack "+attack.Name,attack.TooltipFor(source),y+12,width,15);
            y=DrawAttackPattern(parent,attack,y+8,width);
        }
        return y;
    }
    float DrawAttackPattern(Transform parent,BattleActionOption action,float y,float width,float maximumTileSize=18)
        => DetailsPatternPreview.Draw(parent,action,y,width,maximumTileSize);
}
