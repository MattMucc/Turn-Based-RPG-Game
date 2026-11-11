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
        var profile=Member.GetComponent<CharacterEquipment>();var candidate=Manager.Equipment.Find(id)?.Definition;var current=profile.At(equipmentSlot);
        string text=candidate?candidate.Description:"Remove this equipment. An empty weapon slot provides Punch and the common Push action.";
        text+="\n\nSTATS · current → equipped";
        foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat))){
            int before=Member.Stat(stat), after=Member.StatReplacingEquipment(stat,current,candidate);
            text+=$"\n{AttributePreview.Name(stat)}: {before} → {after}";
        }
        text+="\n\nChanging equipment never heals HP/MP. Removing maximum-pool bonuses clamps current values. Removed weapon shortcuts become empty; assign new ones under Shortcuts.";
        Detail(candidate?candidate.DisplayName:"Unequip",text,candidate?candidate.Image:null);
        if(candidate&&candidate.Kind==EquipmentKind.Weapon){
            var source=new GridUnit(Member.name,Vector2Int.zero);Member.Configure(source);
            source.Strength=Member.StatReplacingEquipment(CharacterStat.Strength,current,candidate);
            source.Magic=Member.StatReplacingEquipment(CharacterStat.Magic,current,candidate);
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
    float DrawAttackPattern(Transform parent,BattleActionOption action,float y,float width)
    {
        int extent=Mathf.Clamp(action.Reach+action.Radius,2,8), side=extent*2+1;
        float size=Mathf.Min(18,(width-24)/side);
        var board=new GridBoard(side,side);var origin=new Vector2Int(extent,extent);var target=origin+Vector2Int.up*Mathf.Clamp(action.Reach,1,extent);
        var footprint=GridTargeting.Footprint(board,origin,target,action);
        var frame=Rect("Pattern "+action.Name,parent,12,y,side*size,side*size);frame.gameObject.AddComponent<Image>().color=Color.black;
        for(int x=0;x<side;x++)for(int z=0;z<side;z++){
            var point=new Vector2Int(x,z);var tile=Rect("Tile",frame,x*size+1,(side-1-z)*size+1,size-2,size-2).gameObject.AddComponent<Image>();tile.raycastTarget=false;
            tile.color=point==origin?new Color(.6f,1,.2f):footprint.Contains(point)?new Color(.9f,.2f,.2f):GridTargeting.CanTarget(board,origin,point,action)?new Color(.2f,.55f,1):new Color(.18f,.2f,.22f);
        }
        y+=side*size+6;
        return ProgressionText(parent,"Pattern legend","Green: user · Blue: reach · Red: example hit area\nFlat-ground preview; terrain and sight still apply."+(action.Reach+action.Radius>8?" Preview cropped to 8 tiles.":""),y,width,12)+8;
    }
}
