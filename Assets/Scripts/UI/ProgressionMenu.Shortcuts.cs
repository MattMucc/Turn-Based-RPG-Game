using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    void Categories()
    {
        var categories=new[]{BattleActionCategory.Attack,BattleActionCategory.Skill,BattleActionCategory.Magic,BattleActionCategory.Item};
        var labels=new[]{"Attacks","Skills","Spells","Items"};float width=(body.rect.width-28)/4;
        for(int i=0;i<4;i++)
        {
            var c=categories[i];var button=Button(body,labels[i],i*(width+6),0,width,()=>{
                category=c;assigning=null;pendingShortcutSlot=null;shortcutSelection="Shortcut 1";Rebuild();});
            if(category==c)button.image.color=new Color(.17f,.38f,.4f);
        }
    }
    string Binding(int slot) => $"{slot+1} / "+ControllerGlyphs.Adapt("LT")+" + "+(ControllerGlyphs.PlayStation?new[]{"Square","Triangle","Cross","Circle"}[slot]:new[]{"X","Y","A","B"}[slot]);
    void ShortcutBadge(Button button,int slot)
    {
        var root=Rect("Binding icons",button.transform,0,0,92,30);root.anchorMin=root.anchorMax=new Vector2(1,.5f);root.pivot=new Vector2(1,.5f);root.anchoredPosition=new Vector2(-12,0);
        var trigger=Rect("Trigger",root,0,0,30,30).gameObject.AddComponent<Image>();trigger.preserveAspect=true;trigger.raycastTarget=false;
        var plus=Label(Rect("Plus",root,33,2,14,28),"+",16);plus.alignment=TextAlignmentOptions.Center;
        var key=Rect("Key",root,55,0,30,30).gameObject.AddComponent<Image>();key.preserveAspect=true;key.raycastTarget=false;
        var fallback=Label(Rect("Key fallback",root,52,0,40,30),"",14);fallback.alignment=TextAlignmentOptions.Center;
        root.gameObject.AddComponent<PlayerShortcutGlyph>().Bind(slot,key,trigger,plus,fallback);
    }
    void Shortcuts()
    {
        Categories();var loadout=Loadout();float width=body.rect.width-10;
        if(!loadout){Detail("No abilities","This character has no available abilities.");return;}
        for(int i=0;i<4;i++)
        {
            int slot=i;var current=loadout.Favorite(category,slot);
            var button=Button(body,"Shortcut "+(slot+1),0,54+i*46,width,()=>ChooseShortcutSlot(slot),()=>{
                if(assigning!=null) { if(!pendingShortcutSlot.HasValue)PreviewShortcut(slot,assigning); }
                else Detail("Shortcut "+(slot+1),Binding(slot)+"\n\n"+(current?.Name??"Empty")+"\n\nSelect this slot, then choose an ability below.");
            },40);
            var label=button.GetComponentInChildren<TMP_Text>();label.text=$"{slot+1}   {current?.Name??"Empty"}";
            label.rectTransform.sizeDelta=new Vector2(width-126,30);label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Ellipsis;
            if(pendingShortcutSlot==slot)button.image.color=new Color(.17f,.38f,.4f);
            ShortcutBadge(button,slot);
        }
        bool confirmation=assigning!=null&&pendingShortcutSlot.HasValue;
        if(confirmation)
        {
            Button(body,"Confirm shortcut",0,246,width-130,ConfirmShortcut,()=>PreviewShortcut(pendingShortcutSlot.Value,assigning));
            Button(body,"Cancel",width-120,246,120,()=>{assigning=null;pendingShortcutSlot=null;shortcutSelection="Shortcut 1";Rebuild();});
        }
        else
        {
            Label(Rect("Shortcut guidance",body,0,246,width,34),assigning!=null?"Choose a slot above, or press its shortcut.":pendingShortcutSlot.HasValue?"Choose an ability for slot "+(pendingShortcutSlot.Value+1)+".":"Choose an ability or a shortcut slot to edit.",14);
        }
        float listTop=292, listHeight=Mathf.Max(100,body.rect.height-listTop-48);
        var options=loadout.Options.Where(o=>o!=null&&o.Category==category).ToList();
        var list=List(body,"Abilities",0,listTop,width,listHeight,Mathf.Max(listHeight,options.Count*44));
        for(int i=0;i<options.Count;i++)
        {
            var option=options[i];
            var button=Button(list,option.Name,0,i*44,width-12,()=>{
                assigning=option;shortcutSelection=pendingShortcutSlot.HasValue?"Confirm shortcut":"Shortcut "+(option.FavoriteSlot>=0?option.FavoriteSlot+1:1);Rebuild();
            },()=>{
                if(confirmation)return;
                if(pendingShortcutSlot.HasValue)PreviewShortcut(pendingShortcutSlot.Value,option);
                else { var unit=new GridUnit(Member.name,Vector2Int.zero);Member.Configure(unit);Detail(option.Name,option.TooltipFor(unit)); }
            },40);
            var label=button.GetComponentInChildren<TMP_Text>();label.rectTransform.sizeDelta=new Vector2(width-140,30);label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Ellipsis;
            if(option.FavoriteSlot>=0)ShortcutBadge(button,option.FavoriteSlot);
            if(assigning==option)button.image.color=new Color(.17f,.38f,.4f);
        }
        if(options.Count==0)Label(Rect("Empty category",list,12,10,width-40,60),"No abilities learned in this category.",16);
        var remove=Button(body,"Remove selected ability's shortcut",0,listTop+listHeight+8,width,()=>{
            if(assigning==null)return;
            string name=assigning.Name;loadout.AssignFavorite(assigning,-1);assigning=null;pendingShortcutSlot=null;shortcutSelection=name;Rebuild();
        });remove.interactable=assigning!=null&&assigning.FavoriteSlot>=0;
        if(confirmation)PreviewShortcut(pendingShortcutSlot.Value,assigning);
        else Detail("Favorite shortcuts","Four shortcuts per category and character.\n\nChanging a shortcut never removes an ability from your battle menu.\n\nItems remain shared, including depleted items.\n\nSelect an ability and a slot to preview the change before confirming.");
    }
    void ChooseShortcutSlot(int slot)
    {
        pendingShortcutSlot=slot;shortcutSelection=assigning==null?"Shortcut "+(slot+1):"Confirm shortcut";Rebuild();
    }
    void PreviewShortcut(int slot,BattleActionOption option)
    {
        if(option==null)return;var existing=Loadout().Favorite(category,slot);
        string text=Binding(slot)+"\n\nCurrent: "+(existing?.Name??"Empty")+"\nNew: "+option.Name;
        if(existing==option)text+="\n\nThis ability is already assigned here.";
        else
        {
            if(existing!=null)text+="\n\n"+existing.Name+" loses this shortcut but remains available in battle.";
            if(option.FavoriteSlot>=0&&option.FavoriteSlot!=slot)text+=$"\n\nSlot {option.FavoriteSlot+1} becomes empty when this ability moves.";
        }
        var unit=new GridUnit(Member.name,Vector2Int.zero);Member.Configure(unit);
        Detail("Shortcut change",text+"\n\n"+option.TooltipFor(unit));
    }
    void ConfirmShortcut()
    {
        if(!open||InBattle||assigning==null||!pendingShortcutSlot.HasValue)return;
        var loadout=Loadout();if(assigning.Category!=category||!loadout.AssignFavorite(assigning,pendingShortcutSlot.Value))return;
        shortcutSelection="Shortcut "+(pendingShortcutSlot.Value+1);assigning=null;pendingShortcutSlot=null;Rebuild();
    }
    bool HandleShortcutInput(Keyboard keyboard,Gamepad pad)
    {
        int slot=-1;
        if(keyboard!=null){if(keyboard.digit1Key.wasPressedThisFrame)slot=0;else if(keyboard.digit2Key.wasPressedThisFrame)slot=1;else if(keyboard.digit3Key.wasPressedThisFrame)slot=2;else if(keyboard.digit4Key.wasPressedThisFrame)slot=3;}
        if(pad!=null&&pad.leftTrigger.isPressed){if(pad.buttonWest.wasPressedThisFrame)slot=0;else if(pad.buttonNorth.wasPressedThisFrame)slot=1;else if(pad.buttonSouth.wasPressedThisFrame)slot=2;else if(pad.buttonEast.wasPressedThisFrame)slot=3;}
        if(slot<0)return false;ChooseShortcutSlot(slot);return true;
    }
}
