using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed partial class ProgressionMenu
{
    RectTransform inventoryModal, itemTooltip;
    BattleActionOption inventoryItem;
    int inventoryStep;
    CanvasGroup inventoryBackground;
    BattleActionOption selectedInventoryItem, draggedInventoryItem, droppedInventoryItem;
    PlayerInventoryPointer inventoryDragSource;
    RectTransform inventoryDragGhost;
    ScrollRect inventoryScroll;
    Outline inventoryDropOutline;
    string droppedEmptySlot;
    readonly System.Collections.Generic.Dictionary<PlayerInventoryPointer,BattleActionOption> inventoryRows=new();
    readonly System.Collections.Generic.Dictionary<BattleActionOption,Button> inventoryButtons=new();
    string InventoryDescription(BattleActionOption item)
    {
        if(inventoryEquipment.ContainsKey(item)||inventoryMaterials.ContainsKey(item))return item.Description;
        if(!string.IsNullOrEmpty(item.LearnSpellId)) {
            var spell=SpellDefinition.Find(item.LearnSpellId);
            return spell?$"Teaches {spell.DisplayName} to one party member.\n{spell.Description}\n\nShared stock: {item.Quantity}\nConsumed on use. Mastery belongs to the learner.":"This book's Spell definition is unavailable.";
        }
        string effect = item.Effect == ActionEffect.Heal ? $"Restores up to {item.Healing} HP. Outside combat, also heals knocked-out party members."
            : item.Effect == ActionEffect.RestoreMagic ? $"Restores up to {item.MagicRestored} MP to a living party member." : item.Description;
        return effect + $"\n\nShared stock: {item.Quantity}\n" + (PartyInventory.UsableOutsideCombat(item) ? "Usable on active and reserve party members. Consumed on use." : "Cannot be used outside combat.");
    }
    [SerializeField] InventoryPageLayout inventoryLayout;
    InventoryPageLayout InventoryLayout=>inventoryLayout?inventoryLayout:Resources.Load<InventoryPageLayout>("UI/Inventory/Inventory");
    void Inventory()
    {
        inventoryRows.Clear();inventoryButtons.Clear();
        float width=Mathf.Max(300,body.rect.width);
        var all=InventoryEntries();
        Label(Rect("Inventory heading",body,0,0,width,30),"SHARED INVENTORY",18);
        var modular=InventoryLayout;
        InventoryWorkspaceView workspace=null;
        if(modular&&modular.Workspace){
            var heading=body.Find("Inventory heading");if(heading)heading.gameObject.SetActive(false);
            workspace=InventoryModularUI.Fill(modular.Workspace,body);
            workspace.Heading.text="SHARED INVENTORY";Canvas.ForceUpdateCanvases();InventoryModularUI.FitFilters(workspace);
        }
        float filterWidth=(width-18)/4;
        var filters=workspace?modular.Filters.Distinct().Select(f=>(InventoryCategory)(int)f).ToArray():System.Enum.GetValues(typeof(InventoryCategory)).Cast<InventoryCategory>().ToArray();
        for(int i=0;i<filters.Length;i++) {
            var filter=filters[i];
            Button tab;
            if(workspace){
                var choice=InventoryModularUI.Choice(modular.FilterButton,workspace.Filters.transform,InventoryCategoryName(filter));tab=choice.Select;buttons.Add(tab);
                tab.gameObject.AddComponent<PlayerMenuFocus>();tab.onClick.AddListener(()=>{inventoryCategory=filter;HideItemTooltip();Rebuild();});
            }else tab=Button(body,InventoryCategoryName(filter),(i%4)*(filterWidth+6),40+(i/4)*36,filterWidth,()=>{inventoryCategory=filter;HideItemTooltip();Rebuild();},null,30);
            if(!workspace)tab.GetComponentInChildren<TMPro.TMP_Text>().fontSize=13;
            if(filter==inventoryCategory)tab.image.color=workspace?modular.SelectedColor:new Color(.16f,.36f,.44f);
        }
        var items=all.Where(x=>inventoryCategory==InventoryCategory.All||InventoryKind(x)==inventoryCategory).ToList();
        if(workspace)workspace.Count.text=$"{items.Count} stacks / equipment copies";
        else Label(Rect("Inventory count",body,0,108,width,24),$"{items.Count} stacks / equipment copies",13);
        int columns=Mathf.Max(3,Mathf.FloorToInt((width-8)/80));float size=(width-8)/columns-8;
        if(workspace){width=workspace.Scroll.viewport.rect.width;columns=InventoryModularUI.Columns(workspace.Grid,width);size=workspace.Grid.cellSize.x;}
        var lookup=items.ToDictionary(InventoryEntryKey);
        var slotKeys=Manager.Inventory.SlotOrder.Where(key=>key!=null&&(key.StartsWith("empty:")||lookup.ContainsKey(key))).ToList();
        int slots=Mathf.Max(columns*3,Mathf.CeilToInt(slotKeys.Count/(float)columns)*columns);
        while(slotKeys.Count<slots){string key="empty:"+System.Guid.NewGuid().ToString("N");slotKeys.Add(key);Manager.Inventory.SlotOrder.Add(key);}
        var list=workspace?workspace.Scroll.content:List(body,"Items",0,138,width,Mathf.Max(150,body.rect.height-138),Mathf.CeilToInt(slots/(float)columns)*(size+8));
        inventoryScroll=list.GetComponentInParent<ScrollRect>();
        for(int i=0;i<slots;i++) {
            float x=(i%columns)*(size+8), y=(i/columns)*(size+8);
            if(!lookup.TryGetValue(slotKeys[i],out var item)){
                string key=slotKeys[i];Image empty;
                if(workspace){var tile=Instantiate(modular.Slot,list,false);tile.name="Empty inventory slot";tile.Select.enabled=false;tile.Quantity.gameObject.SetActive(false);tile.Equipped.gameObject.SetActive(false);empty=tile.Select.image;tile.DropOutline.enabled=false;}
                else {empty=Rect("Empty inventory slot",list,x,y,size,size).gameObject.AddComponent<Image>();empty.color=new Color(.04f,.08f,.12f);}
                var edge=empty.GetComponent<Outline>();if(!edge)edge=empty.gameObject.AddComponent<Outline>();if(!workspace){edge.effectColor=Accent;edge.effectDistance=new Vector2(2,-2);}edge.enabled=false;
                var drop=empty.gameObject.AddComponent<PlayerInventoryPointer>();inventoryRows[drop]=null;
                drop.Drop=source=>{if(source==inventoryDragSource)droppedEmptySlot=key;};continue;
            }
            Button button;
            string badge=inventoryEquipment.TryGetValue(item,out var copy)?"+"+copy.UpgradeLevel:item.Quantity.ToString();
            if(workspace){
                var tile=Instantiate(modular.Slot,list,false);button=tile.Select;button.name=item.Name;buttons.Add(button);
                button.gameObject.AddComponent<PlayerMenuFocus>();button.onClick.AddListener(()=>SelectInventoryItem(item));
                DrawInventoryIcon(tile.Icon,item,0,0,Mathf.Min(tile.Icon.rect.width,tile.Icon.rect.height));
                tile.Quantity.text=badge;tile.Equipped.gameObject.SetActive(copy!=null&&Manager.EquipmentOwner(copy.InstanceId));
            }else{
                button=Button(list,item.Name,x,y,size,()=>SelectInventoryItem(item),null,size);
                button.transform.Find("Label").gameObject.SetActive(false);DrawInventoryIcon(button.transform,item,8,8,size-16);
                var quantity=Label(Rect("Quantity or upgrade",button.transform,4,size-23,size-8,20),badge,13);quantity.alignment=TMPro.TextAlignmentOptions.BottomRight;
                if(copy!=null&&Manager.EquipmentOwner(copy.InstanceId))Label(Rect("Equipped marker",button.transform,5,3,22,20),"E",13).color=Accent;
            }
            var pointer=button.gameObject.AddComponent<PlayerInventoryPointer>();
            inventoryRows[pointer]=item;inventoryButtons[item]=button;
            var outline=button.GetComponent<Outline>();if(!outline)outline=button.gameObject.AddComponent<Outline>();if(!workspace){outline.effectColor=Accent;outline.effectDistance=new Vector2(2,-2);}outline.enabled=false;
            pointer.Context=()=>OpenItemContext(item);pointer.PointerContext=data=>OpenItemContextAt(item,data.position,data.pressEventCamera);pointer.Hover=()=>ShowItemTooltip(item,button.transform);pointer.Exit=HideItemTooltip;
            pointer.Select=()=>SelectInventoryItem(item);
            pointer.Begin=data=>BeginInventoryDrag(pointer,item,data);pointer.Move=MoveInventoryDrag;pointer.End=data=>EndInventoryDrag();pointer.Cancel=CancelInventoryDrag;
            pointer.Drop=source=>{if(source==inventoryDragSource&&inventoryRows.ContainsKey(source))droppedInventoryItem=item;};
        }
        if(workspace)InventoryModularUI.SizeGrid(workspace.Grid,slots,width);
        if(selectedInventoryItem!=null&&items.Contains(selectedInventoryItem))SelectInventoryItem(selectedInventoryItem);
        else {selectedInventoryItem=null;Detail("Party inventory",items.Count==0?"No items in this category.":"Items, equipment and materials shared by the party.");}
    }
    void SelectInventoryItem(BattleActionOption item)
    {
        if(inventoryModal||inventoryDragSource||!inventoryButtons.ContainsKey(item))return;
        selectedInventoryItem=item;
        foreach(var row in inventoryButtons)if(row.Value)row.Value.image.color=row.Key==item?(InventoryLayout?InventoryLayout.SelectedColor:new Color(.16f,.36f,.44f)):(InventoryLayout&&InventoryLayout.Slot?InventoryLayout.Slot.Select.image.color:new Color(.08f,.17f,.25f));
        InventoryDetails(item);
    }
    void RestoreInventorySelection()
    {
        if(selectedInventoryItem!=null&&inventoryButtons.TryGetValue(selectedInventoryItem,out var button)&&button)events.SetSelectedGameObject(button.gameObject);
        else if(inventoryButtons.Count>0)events.SetSelectedGameObject(inventoryButtons.First().Value.gameObject);else SelectFirst();
    }
    void BeginInventoryDrag(PlayerInventoryPointer source,BattleActionOption item,PointerEventData data)
    {
        if(inventoryModal||!open||page!=Page.Inventory){source.CancelDrag();return;}
        CancelInventoryDrag();HideItemTooltip();inventoryDragSource=source;draggedInventoryItem=item;
        inventoryDragGhost=Rect("Dragged inventory item",shell,0,0,64,64);
        inventoryDragGhost.gameObject.AddComponent<Image>().color=new Color(.08f,.17f,.25f,.9f);
        var group=inventoryDragGhost.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        DrawInventoryIcon(inventoryDragGhost,item,6,6,52);
        MoveInventoryDrag(data);
    }
    void MoveInventoryDrag(PointerEventData data)
    {
        if(!inventoryDragSource)return;
        if(inventoryDragGhost&&RectTransformUtility.ScreenPointToLocalPointInRectangle(shell,data.position,data.pressEventCamera,out var p))
            inventoryDragGhost.anchoredPosition=new Vector2(p.x-shell.rect.xMin+12,p.y-shell.rect.yMax-12);
        if(inventoryDropOutline)inventoryDropOutline.enabled=false;
        var hovered=data.pointerCurrentRaycast.gameObject?data.pointerCurrentRaycast.gameObject.GetComponentInParent<PlayerInventoryPointer>():null;
        inventoryDropOutline=hovered&&inventoryRows.ContainsKey(hovered)&&hovered!=inventoryDragSource?hovered.GetComponent<Outline>():null;
        if(inventoryDropOutline)inventoryDropOutline.enabled=true;
        if(inventoryScroll&&inventoryScroll.content.rect.height>inventoryScroll.viewport.rect.height&&RectTransformUtility.ScreenPointToLocalPointInRectangle(inventoryScroll.viewport,data.position,data.pressEventCamera,out var local)){
            var rect=inventoryScroll.viewport.rect;
            if(local.x>=rect.xMin&&local.x<=rect.xMax){float direction=local.y>rect.yMax-30?1:local.y<rect.yMin+30?-1:0;
                inventoryScroll.verticalNormalizedPosition=Mathf.Clamp01(inventoryScroll.verticalNormalizedPosition+direction*Time.unscaledDeltaTime*450/(inventoryScroll.content.rect.height-rect.height));}
        }
    }
    void EndInventoryDrag()
    {
        var from=draggedInventoryItem;var to=droppedInventoryItem;var empty=droppedEmptySlot;
        float scroll=inventoryScroll?inventoryScroll.verticalNormalizedPosition:1;
        CancelInventoryDrag();
        if(open&&page==Page.Inventory&&!inventoryModal&&(empty!=null?MoveInventoryToEmpty(from,empty):SwapInventoryEntries(from,to))){
            selectedInventoryItem=from;Rebuild();if(inventoryScroll)inventoryScroll.verticalNormalizedPosition=scroll;
        }
    }
    void CancelInventoryDrag()
    {
        if(inventoryDragSource)inventoryDragSource.CancelDrag();inventoryDragSource=null;draggedInventoryItem=droppedInventoryItem=null;droppedEmptySlot=null;
        if(inventoryDropOutline)inventoryDropOutline.enabled=false;inventoryDropOutline=null;
        if(inventoryDragGhost){inventoryDragGhost.gameObject.SetActive(false);if(Application.isPlaying)Destroy(inventoryDragGhost.gameObject);else DestroyImmediate(inventoryDragGhost.gameObject);}inventoryDragGhost=null;
    }
    void ShowItemTooltip(BattleActionOption item,Transform source,string description=null)
    {
        if(inventoryModal||inventoryDragSource) return;
        HideItemTooltip();
        var hints=TooltipHintsLayout.Default;
        if(hints&&hints.InventoryHover){
            var view=Instantiate(hints.InventoryHover,shell,false);itemTooltip=(RectTransform)view.transform;itemTooltip.name="Item hover tooltip";
            view.Text.text=$"<b>{item.Name}</b>\n{description??InventoryDescription(item)}";view.Fit(new Vector2(shell.rect.width-16,shell.rect.height-36));
            var point=shell.InverseTransformPoint(source.position);float px=point.x-shell.rect.xMin+100,py=shell.rect.yMax-point.y+44;
            itemTooltip.anchoredPosition=new Vector2(Mathf.Clamp(px,8,Mathf.Max(8,shell.rect.width-itemTooltip.rect.width-8)),-Mathf.Clamp(py,8,Mathf.Max(8,shell.rect.height-itemTooltip.rect.height-8)));return;
        }
        itemTooltip=Rect("Item hover tooltip",shell,0,0,300,190);
        var local=shell.InverseTransformPoint(source.position);
        float x=local.x-shell.rect.xMin+100, y=shell.rect.yMax-local.y+44;
        itemTooltip.anchoredPosition=new Vector2(Mathf.Clamp(x,8,shell.rect.width-308),-Mathf.Clamp(y,8,shell.rect.height-198));
        var image=itemTooltip.gameObject.AddComponent<Image>();image.color=Panel;image.raycastTarget=false;
        var text=Label(Rect("Tooltip text",itemTooltip,12,10,276,170),$"<b>{item.Name}</b>\n{description??InventoryDescription(item)}",14);
        float height=Mathf.Min(shell.rect.height-36,Mathf.Max(80,text.GetPreferredValues(text.text,276,0).y+20));
        itemTooltip.sizeDelta=new Vector2(300,height);text.rectTransform.sizeDelta=new Vector2(276,height-20);
        itemTooltip.anchoredPosition=new Vector2(Mathf.Clamp(x,8,shell.rect.width-308),-Mathf.Clamp(y,8,shell.rect.height-height-8));
    }
    void HideItemTooltip() { if(itemTooltip){itemTooltip.gameObject.SetActive(false);if(Application.isPlaying)Destroy(itemTooltip.gameObject);else DestroyImmediate(itemTooltip.gameObject);} itemTooltip=null; }
    void OpenItemContext(BattleActionOption item)
    {
        if(!inventoryButtons.TryGetValue(item,out var button)||!button)return;
        // Keyboard/controller navigation anchors the popup to the selected item, never the stale mouse position.
        var rect=(RectTransform)button.transform;
        var screen=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(rect.rect.xMax,rect.rect.yMax,0)));
        OpenItemContextAt(item,screen,null);
    }
    void CloseInventoryModal()
    {
        if(inventoryModal){inventoryModal.gameObject.SetActive(false);if(Application.isPlaying)Destroy(inventoryModal.gameObject);else DestroyImmediate(inventoryModal.gameObject);}
        inventoryModal=null;
        if(inventoryBackground){inventoryBackground.interactable=true;inventoryBackground.blocksRaycasts=true;}
    }
    void InventoryBack() { if(inventoryStep>=4){BackInventoryEquipment();return;} if(inventoryStep>0){inventoryStep=inventoryStep==3?2:0;DrawInventoryModal();}else{CloseInventoryModal();Rebuild();} }
    void DrawInventoryModal(BattleCharacterInfo target=null)
    {
        CloseInventoryModal(); HideItemTooltip();
        inventoryBackground=shell.GetComponent<CanvasGroup>();if(!inventoryBackground)inventoryBackground=shell.gameObject.AddComponent<CanvasGroup>();
        inventoryBackground.interactable=false;inventoryBackground.blocksRaycasts=false;
        inventoryModal=CreateInteractionOverlay();
        var dismiss=inventoryModal.GetComponent<Button>();if(!dismiss)dismiss=inventoryModal.gameObject.AddComponent<Button>();dismiss.onClick.AddListener(InventoryBack);var nav=dismiss.navigation;nav.mode=Navigation.Mode.None;dismiss.navigation=nav;
        if(inventoryStep==0){DrawInventoryContextPopup();return;}
        var panel=Rect("Item context",inventoryModal,0,0,440,inventoryStep>=4?500:inventoryStep==2?460:330);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;
        panel.gameObject.AddComponent<Image>().color=Panel;
        Label(Rect("Item name",panel,18,14,404,36),inventoryItem.Name,22);
        int start=buttons.Count;
        if(inventoryStep>=4) DrawInventoryEquipment(panel);
         else if(inventoryStep==1) {
            var content=List(panel,"Item examination",18,60,404,204,204);
            float height=ProgressionText(content,"Item description",InventoryDescription(inventoryItem),4,372,16);
            content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(204,height+8));
        }
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
        ComposeInventoryInteraction(panel,start);
        // Explicit local navigation prevents focus escaping to the dimmed page.
        var choices=buttons.Skip(start).Where(b=>b&&b.IsInteractable()).ToList();
        for(int i=0;i<choices.Count;i++){var n=choices[i].navigation;n.mode=Navigation.Mode.Explicit;n.selectOnUp=choices[(i+choices.Count-1)%choices.Count];n.selectOnDown=choices[(i+1)%choices.Count];choices[i].navigation=n;}
        if(choices.Count>0)events.SetSelectedGameObject(choices[0].gameObject);
    }
}
