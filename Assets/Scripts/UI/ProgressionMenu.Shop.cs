using System;
using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEngine;

public sealed partial class ProgressionMenu
{
    Merchant merchant;
    bool shopSelling,shopPending;
    int shopFilter,shopPrice;
    string shopKey,shopNotice;
    readonly HashSet<BattleActionOption> shopExpanded=new();
    sealed class ShopEntry { public Merchant.Offer Offer; public OwnedEquipment Copy; public string Key; }
    public void OpenShop(Merchant seller)
    {
        if(!seller||!seller.CanInteract)return;
        Open();if(!open)return;ShowPage(Page.Shop);merchant=seller;shopSelling=false;shopFilter=0;shopKey=null;shopNotice=null;shopPending=false;shopExpanded.Clear();Rebuild();
    }
    void ShopPage()
    {
        HideItemTooltip();if(!merchant){Detail("Shop","Merchant unavailable.");return;}
        Manager.BindEquipment();BeginMerchantLayout(false);float w=body.rect.width;
        MerchantLabel(Rect("Merchant",body,0,0,w,32),merchant.DisplayName+" · "+Manager.Equipment.Coins+" coins",20);
        foreach(bool sell in new[]{false,true}){
            var b=MerchantButton(body,sell?"Sell":"Buy",sell?w/2+4:0,40,w/2-4,()=>{shopSelling=sell;shopKey=null;shopPending=false;shopExpanded.Clear();Rebuild();});
            b.interactable=!shopPending;if(sell==shopSelling)b.image.color=(modularMerchant?activeMerchantLayout.SelectedColor:new Color(.16f,.36f,.44f));
        }
        string[] filters={"All","Weapons","Armor","Accessories","Items"};float tw=(w-16)/5;
        for(int i=0;i<filters.Length;i++){int f=i;var b=MerchantButton(body,filters[i],i*(tw+4),86,tw,()=>{shopFilter=f;shopKey=null;shopExpanded.Clear();Rebuild();},null,32);b.interactable=!shopPending;
            var label=b.GetComponentInChildren<TMP_Text>();if(!modularMerchant){label.enableAutoSizing=true;label.fontSizeMin=9;label.fontSizeMax=14;}if(i==shopFilter)b.image.color=(modularMerchant?activeMerchantLayout.SelectedColor:new Color(.16f,.36f,.44f));}
        var entries=new List<ShopEntry>();
        for(int i=0;i<merchant.Offers.Count;i++){
            var offer=merchant.Offers[i];if(offer==null||(!offer.Equipment&&offer.Item==null))continue;
            int kind=offer.Equipment?(int)offer.Equipment.Kind+1:4;if(shopFilter!=0&&shopFilter!=kind)continue;
            if(!shopSelling)entries.Add(new ShopEntry{Offer=offer,Key="offer:"+i});
            else if(offer.Equipment)foreach(var copy in Manager.Equipment.Owned.Where(c=>c.Definition==offer.Equipment))entries.Add(new ShopEntry{Offer=offer,Copy=copy,Key=copy.InstanceId});
            else if(Manager.Inventory.Count(offer.Item.ItemId)>0)entries.Add(new ShopEntry{Offer=offer,Key="item:"+offer.Item.ItemId});
        }
        var selected=entries.Find(e=>e.Key==shopKey)??entries.FirstOrDefault();shopKey=selected?.Key;
        int columns=Mathf.Max(2,Mathf.FloorToInt(w/96));float size=Mathf.Min(88,(w-12)/columns-8);
        var grid=MerchantList(body,"Shop inventory grid",0,128,w,body.rect.height-198,Mathf.Ceil(entries.Count/(float)columns)*(size+30)+8);
        for(int i=0;i<entries.Count;i++){
            var e=entries[i];float x=4+i%columns*(size+8),y=4+i/columns*(size+30);
            var b=MerchantButton(grid,e.Offer.Name,x,y,size,()=>{shopKey=e.Key;shopExpanded.Clear();Rebuild();},null,size);b.interactable=!shopPending;
            var proxy=e.Offer.Equipment?new BattleActionOption{Name=e.Offer.Name,Image=e.Offer.Equipment.Image}:e.Offer.Item;
            MerchantIcon(b,proxy,size);
            proxy.Description=e.Offer.Equipment?e.Offer.Equipment.Description:e.Offer.Item.Description;
            var pointer=b.gameObject.AddComponent<PlayerInventoryPointer>();
            pointer.Hover=()=>{if(!shopPending)ShowItemTooltip(proxy,b.transform,proxy.Description??"");};pointer.Exit=HideItemTooltip;
            if(e==selected)b.image.color=(modularMerchant?activeMerchantLayout.SelectedColor:new Color(.16f,.36f,.44f));
            string caption=(shopSelling?e.Offer.SellPrice:e.Offer.BuyPrice)+" coins";
            if(e.Copy!=null&&Manager.EquipmentOwner(e.Copy.InstanceId))caption="Equipped";
            MerchantLabel(Rect("Price",grid,x,y+size,size,26),caption,11).alignment=TextAlignmentOptions.Center;
        }
        if(entries.Count==0)MerchantLabel(Rect("Empty shop",grid,12,12,w-24,50),"No items in this category.",16);
        MerchantButton(body,"Leave",0,body.rect.height-48,110,Close).interactable=!shopPending;
        MerchantLabel(Rect("Trade notice",body,120,body.rect.height-58,w-120,56),shopNotice??"",14);
        FinishMerchantLayout(entries.Count,shopPending);
        if(selected!=null)ShopDetails(selected);else Detail("Shop","No items available.");
    }
    void ShopDetails(ShopEntry entry)
    {
        Clear(detailRoot);var offer=entry.Offer;var definition=offer.Equipment;int price=shopSelling?offer.SellPrice:offer.BuyPrice;
        var content=List(detailRoot,"Shop item details",0,0,detailRoot.rect.width,detailRoot.rect.height-106,500);
        float w=content.rect.width-24,y=8;
        y=ProgressionText(content,"Item name",offer.Name,y,w,23,Accent);
        var art=definition?definition.Image:offer.Item.Image;
        if(art){var picture=Rect("Item artwork",content,12,y,96,96).gameObject.AddComponent<UnityEngine.UI.RawImage>();picture.texture=art;picture.raycastTarget=false;y+=108;}
        y=ProgressionText(content,"Description",(definition?definition.Description:offer.Item.Description)??"",y,w,16);
        y=ProgressionText(content,"Trade price",(shopSelling?"Sell":"Buy")+" · "+price+" coins",y,w,16);
        if(definition){
            var copy=entry.Copy??new OwnedEquipment{Definition=definition};
            y=ProgressionText(content,"Gear level","Upgrade "+copy.UpgradeLevel+" / "+definition.Upgrades.Count,y,w,14);
            foreach(CharacterStat stat in Enum.GetValues(typeof(CharacterStat))){int value=EquipmentUpgrades.Bonus(copy,stat);if(value!=0)y=ProgressionText(content,"Stat",AttributePreview.Name(stat)+": "+value,y,w,14);}
            if(definition.Kind==EquipmentKind.Weapon){
                var source=new GridUnit(Member.name,Vector2Int.zero);Member.Configure(source);
                var current=Member.GetComponent<CharacterEquipment>()?.Weapon;
                source.Strength=Member.StatReplacingCopy(CharacterStat.Strength,current,copy);source.Magic=Member.StatReplacingCopy(CharacterStat.Magic,current,copy);source.Soul=Member.StatReplacingCopy(CharacterStat.Soul,current,copy);
                y=ProgressionText(content,"Preview character","Attack values · "+DisplayName(Member),y+8,w,14);
                y=ProgressionText(content,"Weapon mastery","Mastery level "+WeaponMastery.Level(copy)+" · XP "+copy.MasteryExperience+" / "+WeaponMastery.MaximumExperience(definition),y,w,15);
                void Attack(BattleActionOption action){
                    if(action==null)return;
                    float at=y;MerchantAttackButton(content,(shopExpanded.Contains(action)?"▾ ":"▸ ")+action.Name,at,w,()=>{if(!shopExpanded.Add(action))shopExpanded.Remove(action);Rebuild();});y+=40;
                    if(shopExpanded.Contains(action)){y=ProgressionText(content,"Attack description",action.TooltipFor(source),y,w,15);y=DrawAttackPattern(content,action,y,w,24);}
                }
                y=ProgressionText(content,"Base attacks","Level 1 · Starting Attacks",y+8,w,16);
                foreach(var action in definition.Attacks)Attack(action);
                int level=1,last=-1;
                foreach(var milestone in WeaponMastery.Milestones(definition)){
                    if(last!=milestone.RequiredExperience){level++;last=milestone.RequiredExperience;}
                    y=ProgressionText(content,"Mastery reward","Level "+level+" · "+milestone.RequiredExperience+" XP\n"+milestone.Name+"\n"+milestone.Description,y+10,w,16);
                    if(milestone.FinisherActionPoints>0)y=ProgressionText(content,"Mastery bonus","Defeating a hostile with a weapon Attack restores "+milestone.FinisherActionPoints+" AP.",y,w,14);
                    foreach(var action in milestone.Attacks)Attack(action);
                }
            }
        }else y=ProgressionText(content,"Owned count","Owned: "+Manager.Inventory.Count(offer.Item.ItemId),y,w,14);
        content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(content.parent.GetComponent<RectTransform>().rect.height,y+12));
        bool equipped=entry.Copy!=null&&Manager.EquipmentOwner(entry.Copy.InstanceId);
        if(equipped)y=ProgressionText(content,"Equipped restriction","Equipped by "+DisplayName(Manager.EquipmentOwner(entry.Copy.InstanceId))+" · Cannot sell",y,w,14);
        content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(content.parent.GetComponent<RectTransform>().rect.height,y+12));
        ComposeDetails(content,DetailPanelKind.Shop);
        MerchantButton(detailRoot,shopPending?"Confirm "+(shopSelling?"sale":"purchase"):"Review "+(shopSelling?"sale":"purchase"),12,detailRoot.rect.height-94,detailRoot.rect.width-24,()=>{
            if(!shopPending){shopPrice=price;shopPending=true;Rebuild();return;}
            merchant.Trade(Manager,offer,entry.Copy,shopSelling,shopPrice,out shopNotice);shopPending=false;Rebuild();
        }).interactable=!equipped&&merchant.CanServe(Manager)&&(shopSelling||Manager.Equipment.Coins>=price);
        if(shopPending)MerchantButton(detailRoot,"Cancel trade",12,detailRoot.rect.height-48,detailRoot.rect.width-24,()=>{shopPending=false;Rebuild();});
    }
}
