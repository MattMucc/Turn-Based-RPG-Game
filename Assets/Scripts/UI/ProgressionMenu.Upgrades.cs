using System;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;

public sealed partial class ProgressionMenu
{
    string upgradeSelection,upgradeNotice,upgradeQuote;
    bool upgradePending;
    int upgradeExpectedLevel;
    readonly System.Collections.Generic.Dictionary<string,UnityEngine.UI.Button> upgradeCards=new();
    void RestoreUpgradeSelection()
    {
        var selected=upgradePending?buttons.Find(b=>b.name=="Confirm upgrade"):upgradeCards.TryGetValue(upgradeSelection??"",out var card)?card:null;
        if(selected&&events)events.SetSelectedGameObject(selected.gameObject);
        var copy=Manager.Equipment.Find(upgradeSelection);if(copy!=null)PreviewUpgrade(copy);
    }
    void UpgradesPage()
    {
        upgradeCards.Clear();Manager.BindEquipment();float width=body.rect.width;
        var copies=Manager.Equipment.Owned.Where(e=>e!=null&&e.Definition&&e.Definition.Kind!=EquipmentKind.Accessory).ToList();
        if(!copies.Any(e=>e.InstanceId==upgradeSelection))upgradeSelection=copies.FirstOrDefault()?.InstanceId;
        Label(Rect("Upgrade heading",body,0,0,width,40),"WEAPONS AND ARMOR",19);
        var list=List(body,"Upgrade copies",0,48,width,Mathf.Max(100,body.rect.height-180),copies.Count*58);
        for(int i=0;i<copies.Count;i++){
            var copy=copies[i];var owner=Manager.EquipmentOwner(copy.InstanceId);
            string label=$"{copy.Definition.DisplayName} +{copy.UpgradeLevel} · Copy {i+1}"+(owner?"\n"+DisplayName(owner):"\nUnequipped");
            var button=Button(list,label,0,i*58,width-12,()=>{upgradeSelection=copy.InstanceId;upgradePending=false;upgradeNotice=null;Rebuild();},()=>{if(!upgradePending)PreviewUpgrade(copy);},52);
            if(copy.InstanceId==upgradeSelection)button.image.color=new Color(.16f,.36f,.44f);
            upgradeCards[copy.InstanceId]=button;
        }
        var selected=Manager.Equipment.Find(upgradeSelection);
        if(selected!=null){
            bool available=EquipmentUpgrades.Requirements(Manager.Equipment,selected,selected.UpgradeLevel,out _,out var reason);
            Button(body,upgradePending?"Confirm upgrade":"Review upgrade",0,body.rect.height-112,width-120,()=>{
                if(!open||InBattle)return;
                if(!upgradePending){upgradeExpectedLevel=selected.UpgradeLevel;upgradeQuote=JsonUtility.ToJson(selected.Definition.Upgrades[upgradeExpectedLevel]);upgradePending=true;Rebuild();return;}
                if(selected.UpgradeLevel!=upgradeExpectedLevel||upgradeExpectedLevel>=selected.Definition.Upgrades.Count||JsonUtility.ToJson(selected.Definition.Upgrades[upgradeExpectedLevel])!=upgradeQuote)upgradeNotice="Recipe changed. Review the upgrade again.";
                else if(Manager.TryUpgradeEquipment(selected.InstanceId,upgradeExpectedLevel,out upgradeNotice))upgradeNotice=$"Upgraded {selected.Definition.DisplayName} to +{selected.UpgradeLevel}.";
                upgradePending=false;Rebuild();
            }).interactable=available;
            if(upgradePending)Button(body,"Cancel upgrade",width-112,body.rect.height-112,112,()=>{upgradePending=false;Rebuild();});
            Label(Rect("Upgrade notice",body,0,body.rect.height-66,width,64),upgradeNotice??(available?"Select a copy, review the recipe, then confirm. Materials are shared by the party.":reason),14);
            PreviewUpgrade(selected);
        }else Detail("Upgrades","No owned weapons or armor.");
    }
    void PreviewUpgrade(OwnedEquipment copy)
    {
        var owner=Manager.EquipmentOwner(copy.InstanceId);
        bool possible=EquipmentUpgrades.Requirements(Manager.Equipment,copy,copy.UpgradeLevel,out var costs,out var reason);
        bool next=copy.UpgradeLevel>=0&&copy.UpgradeLevel<copy.Definition.Upgrades.Count;
        string text=copy.Definition.Description+$"\n\nUpgrade +{copy.UpgradeLevel}"+(next?$" → +{copy.UpgradeLevel+1}":" · Maximum")+"\n"+(owner?"Equipped by "+DisplayName(owner):"Unequipped copy");
        if(next){
            text+="\n\nMATERIALS · required / owned";
            foreach(var cost in costs){var material=Manager.Equipment.Materials.Find(m=>m!=null&&m.Id==cost.Key);text+=$"\n{material?.Name??cost.Key}: {cost.Value} / {material?.Quantity??0}";}
            text+="\n\nEQUIPMENT STATS · current → upgraded";
            foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat)))text+=$"\n{AttributePreview.Name(stat)}: {EquipmentUpgrades.Bonus(copy,stat)} → {EquipmentUpgrades.Bonus(copy,stat,copy.UpgradeLevel+1)}";
            text+="\n\nOnly this copy improves. Mastery and unlocked Attacks stay unchanged. Upgrading does not heal HP/MP.";
        }
        if(!possible)text+="\n\n"+reason;
        Detail(copy.Definition.DisplayName,text,copy.Definition.Image);
    }
}
