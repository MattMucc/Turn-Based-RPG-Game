using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    RectTransform inventoryModal, itemTooltip;
    BattleActionOption inventoryItem;
    int inventoryStep;
    CanvasGroup inventoryBackground;
    string InventoryDescription(BattleActionOption item)
    {
        if(!string.IsNullOrEmpty(item.LearnSpellId)) {
            var spell=SpellDefinition.Find(item.LearnSpellId);
            return spell?$"Teaches {spell.DisplayName} to one party member.\n{spell.Description}\n\nShared stock: {item.Quantity}\nChoose Use, then a learner. Consumes one book on confirmation. Mastery belongs to that character.":"This book's Spell definition is unavailable.";
        }
        string effect = item.Effect == ActionEffect.Heal ? $"Restores up to {item.Healing} HP. Outside combat, also heals knocked-out party members."
            : item.Effect == ActionEffect.RestoreMagic ? $"Restores up to {item.MagicRestored} MP to a living party member." : item.Description;
        return effect + $"\n\nShared stock: {item.Quantity}\n" + (PartyInventory.UsableOutsideCombat(item) ? "Choose Use to select an active or reserve party member. One item is consumed on confirmation." : "Cannot be used outside combat.");
    }
    void Inventory()
    {
        Label(Rect("Inventory title",body,0,0,500,44),"SHARED ITEMS",20);
        var items=Manager.Inventory.Items.Where(x=>x!=null).ToList();
        var list=List(body,"Items",0,54,500,Mathf.Max(240,body.rect.height-54),items.Count*48);
        for(int i=0;i<items.Count;i++) {
            var item=items[i];
            var button=Button(list,$"{item.Name}   ×{item.Quantity}",0,i*48,486,()=>OpenItemContext(item),()=>Detail(item.Name,InventoryDescription(item)),44);
            var pointer=button.gameObject.AddComponent<PlayerInventoryPointer>();
            pointer.Context=()=>OpenItemContext(item); pointer.Hover=()=>ShowItemTooltip(item,button.transform); pointer.Exit=HideItemTooltip;
        }
        Detail("Party inventory","Items are shared by the entire party.\n\nRight-click an item for Examine and Use. You can also select it with Enter or the bottom face button.");
    }
    void ShowItemTooltip(BattleActionOption item,Transform source)
    {
        if(inventoryModal) return;
        HideItemTooltip();
        itemTooltip=Rect("Item hover tooltip",shell,0,0,300,190);
        var local=shell.InverseTransformPoint(source.position);
        float x=local.x-shell.rect.xMin+100, y=shell.rect.yMax-local.y+44;
        itemTooltip.anchoredPosition=new Vector2(Mathf.Clamp(x,8,shell.rect.width-308),-Mathf.Clamp(y,8,shell.rect.height-198));
        var image=itemTooltip.gameObject.AddComponent<Image>();image.color=Panel;image.raycastTarget=false;
        Label(Rect("Tooltip text",itemTooltip,12,10,276,170),$"<b>{item.Name}</b>\n{InventoryDescription(item)}",14);
    }
    void HideItemTooltip() { if(itemTooltip){itemTooltip.gameObject.SetActive(false);if(Application.isPlaying)Destroy(itemTooltip.gameObject);else DestroyImmediate(itemTooltip.gameObject);} itemTooltip=null; }
    void OpenItemContext(BattleActionOption item) { inventoryItem=item; inventoryStep=0; DrawInventoryModal(); }
    void CloseInventoryModal()
    {
        if(inventoryModal){inventoryModal.gameObject.SetActive(false);if(Application.isPlaying)Destroy(inventoryModal.gameObject);else DestroyImmediate(inventoryModal.gameObject);}
        inventoryModal=null;
        if(inventoryBackground){inventoryBackground.interactable=true;inventoryBackground.blocksRaycasts=true;}
    }
    void InventoryBack() { if(inventoryStep>0){inventoryStep=inventoryStep==3?2:0;DrawInventoryModal();}else{CloseInventoryModal();Rebuild();} }
    void DrawInventoryModal(BattleCharacterInfo target=null)
    {
        CloseInventoryModal(); HideItemTooltip();
        inventoryBackground=shell.GetComponent<CanvasGroup>();if(!inventoryBackground)inventoryBackground=shell.gameObject.AddComponent<CanvasGroup>();
        inventoryBackground.interactable=false;inventoryBackground.blocksRaycasts=false;
        inventoryModal=Rect("Inventory interaction",canvasObject.transform,0,0,0,0);inventoryModal.anchorMin=Vector2.zero;inventoryModal.anchorMax=Vector2.one;inventoryModal.offsetMin=inventoryModal.offsetMax=Vector2.zero;
        var shade=inventoryModal.gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.6f);
        var dismiss=inventoryModal.gameObject.AddComponent<Button>();dismiss.onClick.AddListener(InventoryBack);var nav=dismiss.navigation;nav.mode=Navigation.Mode.None;dismiss.navigation=nav;
        var panel=Rect("Item context",inventoryModal,0,0,440,inventoryStep==2?460:330);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;
        panel.gameObject.AddComponent<Image>().color=Panel;
        Label(Rect("Item name",panel,18,14,404,36),inventoryItem.Name,22);
        int start=buttons.Count;
        if(inventoryStep==0) {
            Button(panel,"Examine",18,64,404,()=>{inventoryStep=1;DrawInventoryModal();});
            if(PartyInventory.UsableOutsideCombat(inventoryItem))Button(panel,"Use",18,110,404,()=>{inventoryStep=2;DrawInventoryModal();}).interactable=inventoryItem.Quantity>0;
        } else if(inventoryStep==1) Label(Rect("Item description",panel,18,60,404,200),InventoryDescription(inventoryItem),16);
        else if(inventoryStep==2) {
            Label(Rect("Choose target",panel,18,54,404,30),"Choose a party member",17);
            var list=List(panel,"Party targets",18,90,404,304,Manager.Progression.Members.Count*48);
            int i=0;foreach(var member in Manager.Progression.Members) {
                if(!string.IsNullOrEmpty(inventoryItem.LearnSpellId)) {
                    var learner=member;bool known=member.GetComponent<BattleActionLoadout>()?.KnowsSpell(inventoryItem.LearnSpellId)==true;
                    Button(list,$"{DisplayName(member)}   {(known?"Already learned":"Learn spell")}",0,i++*48,390,()=>{inventoryStep=3;DrawInventoryModal(learner);},null,44).interactable=Manager.Inventory.CanLearn(inventoryItem,member);
                    continue;
                }
                var captured=member; int recovery=Manager.Inventory.PreviewRecovery(inventoryItem,member,out int current,out int maximum);
                Button(list,$"{DisplayName(member)}   {current} / {maximum}   (+{recovery})",0,i++*48,390,()=>{inventoryStep=3;DrawInventoryModal(captured);},null,44);
            }
        } else if(!string.IsNullOrEmpty(inventoryItem.LearnSpellId)) {
            var spell=SpellDefinition.Find(inventoryItem.LearnSpellId);
            Label(Rect("Learning preview",panel,18,60,404,132),$"{DisplayName(target)} will learn {(spell?spell.DisplayName:"Unknown spell")}.\nHigher tiers unlock through this character's successful casts.\nConsumes 1 {inventoryItem.Name} ({inventoryItem.Quantity} carried)",17);
            bool committed=false;
            Button(panel,"Confirm learning",18,204,404,()=>{
                if(committed||!inventoryModal||inventoryStep!=3||InBattle||!Manager.Progression.Members.Contains(target))return;
                committed=Manager.Inventory.TryLearn(inventoryItem,target);CloseInventoryModal();Rebuild();
            }).interactable=Manager.Inventory.CanLearn(inventoryItem,target);
        } else {
            int recovery=Manager.Inventory.PreviewRecovery(inventoryItem,target,out int current,out int maximum);
            string resource=inventoryItem.Effect==ActionEffect.Heal?"HP":"MP";
            Label(Rect("Recovery preview",panel,18,60,404,132),$"{DisplayName(target)}\n{resource}: {current} / {maximum} → {current+recovery} / {maximum}\nRecovers {recovery} {resource}\nConsumes 1 {inventoryItem.Name} ({inventoryItem.Quantity} carried)",17);
            bool committed=false;
            Button(panel,"Confirm use",18,204,404,()=>{
                if(committed || !inventoryModal || inventoryStep!=3)return;
                int latest=Manager.Inventory.PreviewRecovery(inventoryItem,target,out int now,out int max);
                if(latest!=recovery || now!=current || max!=maximum){DrawInventoryModal(target);return;}
                committed=true;
                if(!InBattle && Manager.Progression.Members.Contains(target))Manager.Inventory.TryUseOutsideCombat(inventoryItem,target);
                CloseInventoryModal();Rebuild();
            }).interactable=recovery>0;
        }
        Button(panel,"Back",18,panel.sizeDelta.y-52,404,InventoryBack);
        // Explicit local navigation prevents focus escaping to the dimmed page.
        var choices=buttons.Skip(start).Where(b=>b&&b.IsInteractable()).ToList();
        for(int i=0;i<choices.Count;i++){var n=choices[i].navigation;n.mode=Navigation.Mode.Explicit;n.selectOnUp=choices[(i+choices.Count-1)%choices.Count];n.selectOnDown=choices[(i+1)%choices.Count];choices[i].navigation=n;}
        if(choices.Count>0)events.SetSelectedGameObject(choices[0].gameObject);
    }
}
