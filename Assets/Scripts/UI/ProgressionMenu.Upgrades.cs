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
        HideItemTooltip();upgradeCards.Clear();Manager.BindEquipment();InventoryEntries();BeginMerchantLayout(true);float width=body.rect.width;
        if(!blacksmith)MerchantButton(body,"Back",width-112,body.rect.height-112,112,()=>ShowPage(Page.Inventory));
        var copies=Manager.Equipment.Owned.Where(e=>e!=null&&e.Definition&&(forgeCategory==0||(int)e.Definition.Kind==forgeCategory-1)).ToList();
        if(!copies.Any(e=>e.InstanceId==upgradeSelection))upgradeSelection=copies.FirstOrDefault()?.InstanceId;
        MerchantLabel(Rect("Upgrade heading",body,0,0,width,40),BlacksmithServiceAvailable?blacksmith.DisplayName+" · "+Manager.Equipment.Coins+" coins":"UPGRADE RECIPES · Visit a blacksmith",19);
        string[] filters={"All","Weapons","Armor","Accessories"};float tabWidth=(width-18)/4;
        for(int f=0;f<filters.Length;f++){
            int category=f;var tab=MerchantButton(body,filters[f],f*(tabWidth+6),44,tabWidth,()=>{forgeCategory=category;upgradePending=false;upgradeNotice=null;Rebuild();},null,32);
            tab.interactable=!upgradePending;var text=tab.GetComponentInChildren<TMP_Text>();if(!modularMerchant){text.enableAutoSizing=true;text.fontSizeMin=11;text.fontSizeMax=15;}
            if(f==forgeCategory)tab.image.color=(modularMerchant?activeMerchantLayout.SelectedColor:new Color(.16f,.36f,.44f));
        }
        int columns=Mathf.Max(2,Mathf.FloorToInt((width+8)/96));float size=Mathf.Min(88,(width-8*(columns-1)-12)/columns),rowHeight=size+32;
        var list=MerchantList(body,"Forge equipment grid",0,88,width,Mathf.Max(100,body.rect.height-166),Mathf.Ceil(copies.Count/(float)columns)*rowHeight+8);
        for(int i=0;i<copies.Count;i++){
            var copy=copies[i];var owner=Manager.EquipmentOwner(copy.InstanceId);var proxy=inventoryEquipment.First(e=>e.Value==copy).Key;
            float x=(i%columns)*(size+8)+4,y=(i/columns)*rowHeight+4;
            var button=MerchantButton(list,copy.Definition.DisplayName,x,y,size,()=>{HideItemTooltip();upgradeSelection=copy.InstanceId;upgradePending=false;upgradeNotice=null;Rebuild();},null,size);
            MerchantIcon(button,proxy,size);
            button.interactable=!upgradePending;
            if(copy.InstanceId==upgradeSelection)button.image.color=(modularMerchant?activeMerchantLayout.SelectedColor:new Color(.16f,.36f,.44f));
            var caption=MerchantLabel(Rect("Equipped owner",list,x,y+size+2,size,26),owner?DisplayName(owner):"Unequipped",11);if(!modularMerchant){caption.enableAutoSizing=true;caption.fontSizeMin=9;caption.fontSizeMax=11;caption.alignment=TextAlignmentOptions.Center;}
            var pointer=button.gameObject.AddComponent<PlayerInventoryPointer>();
            pointer.Hover=()=>{if(!upgradePending)ShowForgeTooltip(copy,button.transform);};pointer.Exit=HideItemTooltip;
            button.GetComponent<PlayerMenuFocus>().Preview=()=>{if(!upgradePending)ShowForgeTooltip(copy,button.transform);};
            upgradeCards[copy.InstanceId]=button;
        }
        if(copies.Count==0)MerchantLabel(Rect("Empty forge category",list,12,12,width-24,44),"No equipment in this category.",16);
        if(blacksmith&&!upgradePending)MerchantButton(body,"Leave",0,body.rect.height-52,112,Close);
        FinishMerchantLayout(copies.Count,upgradePending);
        var selected=Manager.Equipment.Find(upgradeSelection);
        if(selected!=null){
            bool available=EquipmentUpgrades.Requirements(Manager.Equipment,selected,selected.UpgradeLevel,out _,out var reason);
            MerchantButton(detailRoot,upgradePending?"Confirm upgrade":"Review upgrade",12,detailRoot.rect.height-92,detailRoot.rect.width-24,()=>{
                HideItemTooltip();
                if(!open||InBattle)return;
                if(!BlacksmithServiceAvailable){upgradeNotice="The blacksmith is no longer available.";upgradePending=false;Rebuild();return;}
                if(!EquipmentUpgrades.Requirements(Manager.Equipment,selected,selected.UpgradeLevel,out _,out upgradeNotice)){upgradePending=false;Rebuild();return;}
                if(!upgradePending){upgradeExpectedLevel=selected.UpgradeLevel;upgradeQuote=JsonUtility.ToJson(selected.Definition.Upgrades[upgradeExpectedLevel]);upgradePending=true;Rebuild();return;}
                if(selected.UpgradeLevel!=upgradeExpectedLevel||upgradeExpectedLevel>=selected.Definition.Upgrades.Count||JsonUtility.ToJson(selected.Definition.Upgrades[upgradeExpectedLevel])!=upgradeQuote)upgradeNotice="Recipe changed. Review the upgrade again.";
                else if(Manager.TryUpgradeEquipment(selected.InstanceId,upgradeExpectedLevel,out upgradeNotice))upgradeNotice=$"Upgraded {selected.Definition.DisplayName} to +{selected.UpgradeLevel}.";
                upgradePending=false;Rebuild();
            }).interactable=available&&BlacksmithServiceAvailable;
            if(upgradePending)MerchantButton(detailRoot,"Cancel upgrade",12,detailRoot.rect.height-46,detailRoot.rect.width-24,()=>{upgradePending=false;Rebuild();});
            MerchantLabel(Rect("Upgrade notice",body,124,body.rect.height-56,width-124,52),upgradeNotice??(!BlacksmithServiceAvailable?"Interact with a nearby blacksmith to purchase upgrades.":available?"":reason),14);
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
        // Keep transaction controls fixed; only rebuild the scrollable presentation.
        foreach(Transform child in detailRoot.Cast<Transform>().ToArray())if(!child.GetComponent<UnityEngine.UI.Button>()){
            child.gameObject.SetActive(false);if(Application.isPlaying)Destroy(child.gameObject);else DestroyImmediate(child.gameObject);
        }
        var content=List(detailRoot,"Forge details",0,0,detailRoot.rect.width,detailRoot.rect.height-110,300);
        float y=8,w=content.rect.width-24;
        void Row(DetailSection kind,string value,int size=16){y=ProgressionText(content,kind.ToString(),value,y,w,size)+6;}
        Row(DetailSection.Name,copy.Definition.DisplayName,23);
        if(copy.Definition.Image){var image=Rect("Artwork",content,12,y,112,112).gameObject.AddComponent<UnityEngine.UI.RawImage>();image.texture=copy.Definition.Image;image.raycastTarget=false;y+=124;}
        Row(DetailSection.Description,copy.Definition.Description??"");
        Row(DetailSection.Upgrade,$"Upgrade {copy.UpgradeLevel} / {copy.Definition.Upgrades.Count}"+(next?$" → {copy.UpgradeLevel+1}":" · Maximum"));
        Row(DetailSection.Availability,owner?"Equipped by "+DisplayName(owner):"Unequipped",14);
        if(next){
            Row(DetailSection.Price,$"FEE: {copy.Definition.Upgrades[copy.UpgradeLevel].Fee} coins / {Manager.Equipment.Coins} owned");
            Row(DetailSection.Materials,"MATERIALS · required / owned");
            foreach(var cost in costs){var material=Manager.Equipment.Materials.Find(m=>m!=null&&m.Id==cost.Key);Row(DetailSection.Materials,$"{material?.Name??cost.Key}: {cost.Value} / {material?.Quantity??0}",14);}
            Row(DetailSection.Stats,"EQUIPMENT STATS · current → upgraded");
            foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat)))Row(DetailSection.Stats,$"{AttributePreview.Name(stat)}: {EquipmentUpgrades.Bonus(copy,stat)} → {EquipmentUpgrades.Bonus(copy,stat,copy.UpgradeLevel+1)}",14);
        }
        if(!possible)Row(DetailSection.Notice,reason);
        content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(((RectTransform)content.parent).rect.height,y+12));
        ComposeDetails(content,DetailPanelKind.Blacksmith);
    }
}
