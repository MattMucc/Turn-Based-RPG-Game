using System;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using TMPro;

public sealed partial class ProgressionMenu
{
    Blacksmith blacksmith;
    public bool BlacksmithServiceAvailable => open&&page==Page.Upgrades&&blacksmith&&blacksmith.CanServe(Manager);
    public void OpenBlacksmith(Blacksmith smith)
    {
        if(!smith||!smith.CanInteract)return;
        Open();if(!open)return;ShowPage(Page.Upgrades);forgeCategory=0;blacksmith=smith;upgradePending=false;Rebuild();
    }
    string upgradeSelection,upgradeNotice,upgradeQuote;
    bool upgradePending;
    int upgradeExpectedLevel;
    int forgeCategory;
    readonly System.Collections.Generic.Dictionary<string,UnityEngine.UI.Button> upgradeCards=new();
    void RestoreUpgradeSelection()
    {
        var selected=upgradePending?buttons.Find(b=>b.name=="Confirm upgrade"):upgradeCards.TryGetValue(upgradeSelection??"",out var card)?card:null;
        if(selected&&events)events.SetSelectedGameObject(selected.gameObject);
        var copy=Manager.Equipment.Find(upgradeSelection);if(copy!=null)PreviewUpgrade(copy);
    }
    void UpgradesPage()
    {
        HideItemTooltip();upgradeCards.Clear();Manager.BindEquipment();InventoryEntries();float width=body.rect.width;
        if(!blacksmith)Button(body,"Back",width-112,body.rect.height-112,112,()=>ShowPage(Page.Inventory));
        var copies=Manager.Equipment.Owned.Where(e=>e!=null&&e.Definition&&(forgeCategory==0||(int)e.Definition.Kind==forgeCategory-1)).ToList();
        if(!copies.Any(e=>e.InstanceId==upgradeSelection))upgradeSelection=copies.FirstOrDefault()?.InstanceId;
        Label(Rect("Upgrade heading",body,0,0,width,40),BlacksmithServiceAvailable?blacksmith.DisplayName+" · "+Manager.Equipment.Coins+" coins":"UPGRADE RECIPES · Visit a blacksmith",19);
        string[] filters={"All","Weapons","Armor","Accessories"};float tabWidth=(width-18)/4;
        for(int f=0;f<filters.Length;f++){
            int category=f;var tab=Button(body,filters[f],f*(tabWidth+6),44,tabWidth,()=>{forgeCategory=category;upgradePending=false;upgradeNotice=null;Rebuild();},null,32);
            tab.interactable=!upgradePending;var text=tab.GetComponentInChildren<TMP_Text>();text.enableAutoSizing=true;text.fontSizeMin=11;text.fontSizeMax=15;
            if(f==forgeCategory)tab.image.color=new Color(.16f,.36f,.44f);
        }
        int columns=Mathf.Max(2,Mathf.FloorToInt((width+8)/96));float size=Mathf.Min(88,(width-8*(columns-1)-12)/columns),rowHeight=size+32;
        var list=List(body,"Forge equipment grid",0,88,width,Mathf.Max(100,body.rect.height-166),Mathf.Ceil(copies.Count/(float)columns)*rowHeight+8);
        for(int i=0;i<copies.Count;i++){
            var copy=copies[i];var owner=Manager.EquipmentOwner(copy.InstanceId);var proxy=inventoryEquipment.First(e=>e.Value==copy).Key;
            float x=(i%columns)*(size+8)+4,y=(i/columns)*rowHeight+4;
            var button=Button(list,copy.Definition.DisplayName,x,y,size,()=>{HideItemTooltip();upgradeSelection=copy.InstanceId;upgradePending=false;upgradeNotice=null;Rebuild();},null,size);
            button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);DrawInventoryIcon(button.transform,proxy,8,8,size-16);
            button.interactable=!upgradePending;
            if(copy.InstanceId==upgradeSelection)button.image.color=new Color(.16f,.36f,.44f);
            var caption=Label(Rect("Equipped owner",list,x,y+size+2,size,26),owner?DisplayName(owner):"Unequipped",11);caption.enableAutoSizing=true;caption.fontSizeMin=9;caption.fontSizeMax=11;caption.alignment=TextAlignmentOptions.Center;
            var pointer=button.gameObject.AddComponent<PlayerInventoryPointer>();
            pointer.Hover=()=>{if(!upgradePending)ShowForgeTooltip(copy,button.transform);};pointer.Exit=HideItemTooltip;
            button.GetComponent<PlayerMenuFocus>().Preview=()=>{if(!upgradePending)ShowForgeTooltip(copy,button.transform);};
            upgradeCards[copy.InstanceId]=button;
        }
        if(copies.Count==0)Label(Rect("Empty forge category",list,12,12,width-24,44),"No equipment in this category.",16);
        if(blacksmith&&!upgradePending)Button(body,"Leave",0,body.rect.height-52,112,Close);
        var selected=Manager.Equipment.Find(upgradeSelection);
        if(selected!=null){
            bool available=EquipmentUpgrades.Requirements(Manager.Equipment,selected,selected.UpgradeLevel,out _,out var reason);
            Button(detailRoot,upgradePending?"Confirm upgrade":"Review upgrade",12,detailRoot.rect.height-92,detailRoot.rect.width-24,()=>{
                HideItemTooltip();
                if(!open||InBattle)return;
                if(!BlacksmithServiceAvailable){upgradeNotice="The blacksmith is no longer available.";upgradePending=false;Rebuild();return;}
                if(!EquipmentUpgrades.Requirements(Manager.Equipment,selected,selected.UpgradeLevel,out _,out upgradeNotice)){upgradePending=false;Rebuild();return;}
                if(!upgradePending){upgradeExpectedLevel=selected.UpgradeLevel;upgradeQuote=JsonUtility.ToJson(selected.Definition.Upgrades[upgradeExpectedLevel]);upgradePending=true;Rebuild();return;}
                if(selected.UpgradeLevel!=upgradeExpectedLevel||upgradeExpectedLevel>=selected.Definition.Upgrades.Count||JsonUtility.ToJson(selected.Definition.Upgrades[upgradeExpectedLevel])!=upgradeQuote)upgradeNotice="Recipe changed. Review the upgrade again.";
                else if(Manager.TryUpgradeEquipment(selected.InstanceId,upgradeExpectedLevel,out upgradeNotice))upgradeNotice=$"Upgraded {selected.Definition.DisplayName} to +{selected.UpgradeLevel}.";
                upgradePending=false;Rebuild();
            }).interactable=available&&BlacksmithServiceAvailable;
            if(upgradePending)Button(detailRoot,"Cancel upgrade",12,detailRoot.rect.height-46,detailRoot.rect.width-24,()=>{upgradePending=false;Rebuild();});
            Label(Rect("Upgrade notice",body,124,body.rect.height-56,width-124,52),upgradeNotice??(!BlacksmithServiceAvailable?"Interact with a nearby blacksmith to purchase upgrades.":available?"":reason),14);
            PreviewUpgrade(selected);
        }else Detail("Upgrades","No equipment in this category.");
    }
    void ShowForgeTooltip(OwnedEquipment copy,Transform source)
    {
        HideItemTooltip();itemTooltip=Rect("Forge item tooltip",shell,0,0,280,120);
        var local=shell.InverseTransformPoint(source.position);itemTooltip.anchoredPosition=new Vector2(Mathf.Clamp(local.x-shell.rect.xMin+72,8,shell.rect.width-288),-Mathf.Clamp(shell.rect.yMax-local.y+12,8,shell.rect.height-128));
        var image=itemTooltip.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=Panel;image.raycastTarget=false;
        Label(Rect("Description",itemTooltip,12,10,256,100),"<b>"+copy.Definition.DisplayName+"</b>\n"+copy.Definition.Description,14);
    }
    void PreviewUpgrade(OwnedEquipment copy)
    {
        var owner=Manager.EquipmentOwner(copy.InstanceId);
        bool possible=EquipmentUpgrades.Requirements(Manager.Equipment,copy,copy.UpgradeLevel,out var costs,out var reason);
        bool next=copy.UpgradeLevel>=0&&copy.UpgradeLevel<copy.Definition.Upgrades.Count;
        string text=copy.Definition.Description+$"\nUpgrade cap: +{copy.Definition.Upgrades.Count}"+$"\n\nUpgrade +{copy.UpgradeLevel}"+(next?$" → +{copy.UpgradeLevel+1}":" · Maximum")+"\n"+(owner?"Equipped by "+DisplayName(owner):"Unequipped");
        if(next){
            text+=$"\n\nFEE: {copy.Definition.Upgrades[copy.UpgradeLevel].Fee} coins / {Manager.Equipment.Coins} owned";
            text+="\n\nMATERIALS · required / owned";
            foreach(var cost in costs){var material=Manager.Equipment.Materials.Find(m=>m!=null&&m.Id==cost.Key);text+=$"\n{material?.Name??cost.Key}: {cost.Value} / {material?.Quantity??0}";}
            text+="\n\nEQUIPMENT STATS · current → upgraded";
            foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat)))text+=$"\n{AttributePreview.Name(stat)}: {EquipmentUpgrades.Bonus(copy,stat)} → {EquipmentUpgrades.Bonus(copy,stat,copy.UpgradeLevel+1)}";
        }
        if(!possible)text+="\n\n"+reason;
        Detail(copy.Definition.DisplayName,text,copy.Definition.Image);
        var viewport=(RectTransform)detailContent.parent;viewport.sizeDelta=new Vector2(viewport.sizeDelta.x,Mathf.Max(80,detailRoot.rect.height+viewport.anchoredPosition.y-110));
        detailContent.sizeDelta=new Vector2(detailContent.sizeDelta.x,Mathf.Max(viewport.sizeDelta.y,detailText.rectTransform.sizeDelta.y+20));
    }
}
