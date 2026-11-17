using System;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;

public sealed partial class ProgressionMenu
{
    BattleCharacterInfo inventoryEquipMember;
    EquipmentSlot inventoryEquipSlot;
    bool inventoryUnequip;
    string inventoryEquipNotice;
    void DrawInventoryEquipmentContext(RectTransform panel,OwnedEquipment copy)
    {
        var owner=Manager.EquipmentOwner(copy.InstanceId);
        Button(panel,"Equip",18,110,404,()=>{inventoryUnequip=false;inventoryEquipNotice=null;inventoryStep=4;DrawInventoryModal();}).interactable=!owner;
        if(owner)Button(panel,"Unequip",18,156,404,()=>{
            inventoryUnequip=true;inventoryEquipMember=owner;inventoryEquipNotice=null;
            inventoryEquipSlot=Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().First(s=>owner.GetComponent<CharacterEquipment>().InstanceAt(s)==copy.InstanceId);
            inventoryStep=6;DrawInventoryModal();
        });
        Button(panel,"Upgrade details",18,202,404,()=>{upgradeSelection=copy.InstanceId;ShowPage(Page.Upgrades);});
        if(owner)Label(Rect("Equipment owner",panel,18,246,404,26),"Equipped by "+DisplayName(owner)+". Unequip before transferring.",12);
    }
    void BackInventoryEquipment()
    {
        if(inventoryStep==6&&!inventoryUnequip)inventoryStep=inventoryEquipment[inventoryItem].Definition.Kind==EquipmentKind.Accessory?5:4;
        else inventoryStep=inventoryStep==5?4:0;
        inventoryEquipNotice=null;DrawInventoryModal();
    }
    void DrawInventoryEquipment(RectTransform panel)
    {
        if(!inventoryEquipment.TryGetValue(inventoryItem,out var copy)||Manager.Equipment.Find(copy.InstanceId)!=copy){
            Label(Rect("Unavailable equipment",panel,18,64,404,100),"This equipment copy is no longer available.",17);return;
        }
        if(inventoryStep==4){
            Label(Rect("Choose equipment wearer",panel,18,54,404,28),"Choose a party member",17);
            var list=List(panel,"Equipment recipients",18,90,404,342,Manager.Progression.Members.Count*52);
            int i=0;foreach(var member in Manager.Progression.Members){
                var recipient=member;var profile=member.GetComponent<CharacterEquipment>();
                var slot=copy.Definition.Kind==EquipmentKind.Weapon?EquipmentSlot.Weapon:copy.Definition.Kind==EquipmentKind.Armor?EquipmentSlot.Armor:EquipmentSlot.AccessoryOne;
                bool compatible=profile&&profile.Accepts(slot,copy.Definition);
                Button(list,DisplayName(member)+(compatible?"":" · incompatible"),0,i++*52,390,()=>{
                    inventoryEquipMember=recipient;inventoryEquipSlot=slot;inventoryStep=copy.Definition.Kind==EquipmentKind.Accessory?5:6;DrawInventoryModal();
                },null,46).interactable=compatible&&!Manager.EquipmentOwner(copy.InstanceId);
            }
            return;
        }
        if(inventoryStep==5){
            Label(Rect("Choose accessory slot",panel,18,54,404,45),DisplayName(inventoryEquipMember)+" · Choose an accessory slot",17);
            int i=0;foreach(var slot in new[]{EquipmentSlot.AccessoryOne,EquipmentSlot.AccessoryTwo}){
                var chosen=slot;var current=inventoryEquipMember.GetComponent<CharacterEquipment>().At(slot);
                Button(panel,SlotName(slot)+": "+(current?current.DisplayName:"Empty"),18,112+i++*58,404,()=>{inventoryEquipSlot=chosen;inventoryStep=6;DrawInventoryModal();},null,50);
            }
            return;
        }
        var equipment=inventoryEquipMember?inventoryEquipMember.GetComponent<CharacterEquipment>():null;
        if(!equipment){Label(Rect("Unavailable wearer",panel,18,64,404,100),"This character is unavailable.");return;}
        var previous=equipment.CopyAt(inventoryEquipSlot);var next=inventoryUnequip?null:copy;
        string beforeId=equipment.InstanceAt(inventoryEquipSlot);
        string comparison=DisplayName(inventoryEquipMember)+" · "+SlotName(inventoryEquipSlot)+"\n"+
            (previous?.Definition?previous.Definition.DisplayName:"Empty")+" → "+(next?.Definition?next.Definition.DisplayName:"Empty")+"\n\n";
        foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat))){
            int before=inventoryEquipMember.Stat(stat),after=inventoryEquipMember.StatReplacingCopy(stat,previous,next);
            comparison+=$"{AttributePreview.Name(stat)}: {before} → <color={(after>before?"#80E3AC":after<before?"#FF9393":"#E0EFFF")}>{after}</color>\n";
        }
        comparison+="\nNo HP or MP is restored. Lower maximum pools clamp current values.";
        if(inventoryEquipSlot==EquipmentSlot.Weapon)comparison+="\nWeapon Attacks change; removed weapon shortcuts become empty.";
        var content=List(panel,"Equipment comparison",18,60,404,310,310);
        float height=ProgressionText(content,"Equipment stat changes",comparison,0,372,16);
        content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(310,height));
        string expectedStats="";foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat)))expectedStats+=inventoryEquipMember.Stat(stat)+":"+inventoryEquipMember.StatReplacingCopy(stat,previous,next)+";";
        var confirmation=inventoryModal;bool committed=false;
        Button(panel,inventoryUnequip?"Confirm unequip":"Confirm equip",18,396,404,()=>{
            if(committed||!open||inventoryModal!=confirmation||!inventoryModal||inventoryStep!=6||InBattle)return;
            if(equipment.InstanceAt(inventoryEquipSlot)!=beforeId){inventoryEquipNotice="Equipment changed. Review the updated comparison.";DrawInventoryModal();return;}
            // Re-evaluate stats before committing, so changed progression/gear cannot invalidate the preview silently.
            string currentStats="";foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat)))currentStats+=inventoryEquipMember.Stat(stat)+":"+inventoryEquipMember.StatReplacingCopy(stat,previous,next)+";";
            if(currentStats!=expectedStats){inventoryEquipNotice="Stats changed. Review the updated comparison.";DrawInventoryModal();return;}
            if(Manager.Equipment.Find(copy.InstanceId)!=copy||(inventoryUnequip&&beforeId!=copy.InstanceId)){inventoryEquipNotice="Equipment changed. Reopen this item.";DrawInventoryModal();return;}
            if(!Manager.TryEquip(inventoryEquipMember,inventoryEquipSlot,inventoryUnequip?null:copy.InstanceId,out inventoryEquipNotice)){DrawInventoryModal();return;}
            committed=true;CloseInventoryModal();Rebuild();
        });
        if(!string.IsNullOrEmpty(inventoryEquipNotice))Label(Rect("Equipment change notice",panel,18,370,404,24),inventoryEquipNotice,12);
    }
}
