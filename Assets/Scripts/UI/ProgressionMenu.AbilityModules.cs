using System;
using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;
public sealed partial class ProgressionMenu
{
    [SerializeField] AbilitiesPageLayout abilitiesLayout;
    AbilitiesPageLayout AbilitiesLayout=>abilitiesLayout?abilitiesLayout:Resources.Load<AbilitiesPageLayout>("UI/Abilities/Abilities");
    AbilitiesWorkspaceView modularAbilities;
    Button RegisterAbilityButton(Button button,string name,Action action,Action hover=null){
        button.name=name;buttons.Add(button);button.onClick.AddListener(()=>action());
        var focus=button.GetComponent<PlayerMenuFocus>();if(!focus)focus=button.gameObject.AddComponent<PlayerMenuFocus>();focus.Preview=hover;return button;
    }
    bool BuildModularAbilityShell(){
        modularAbilities=null;var layout=AbilitiesLayout;if(!layout||!layout.Workspace)return false;
        modularAbilities=InventoryModularUI.Fill(layout.Workspace,body);
        foreach(var category in layout.Categories.Distinct()){
            var selected=category;string name=selected==BattleActionCategory.Attack?"Attacks":selected==BattleActionCategory.Magic?"Spells":selected==BattleActionCategory.Skill?"Skills":"Items";
            var choice=InventoryModularUI.Choice(layout.CategoryButton,modularAbilities.Categories,name);
            RegisterAbilityButton(choice.Select,name,()=>{abilityCategory=selected;abilitySelection=null;abilitySearch="";abilityFamily=null;Rebuild();});
            if(selected==abilityCategory)choice.Select.image.color=layout.SelectedColor;
        }
        return true;
    }
    void DrawModularAbilityEntries(List<AbilityEntry> entries){
        var layout=AbilitiesLayout;
        Clear(modularAbilities.Families.content);Clear(modularAbilities.Entries.content);
        bool families=abilityCategory==BattleActionCategory.Magic&&entries.Count>0;
        AbilitiesModularUI.FamilyMode(modularAbilities,layout.Workspace,families);
        if(families){
            var groups=entries.GroupBy(AbilityFamily).OrderBy(g=>g.First().Spell?g.First().Spell.DisplayName:g.First().Action.Name).ToList();
            if(!groups.Any(g=>g.Key==abilityFamily))abilityFamily=groups[0].Key;
            foreach(var group in groups){
                string key=group.Key;var entry=group.First();string name=entry.Spell?entry.Spell.DisplayName:entry.Action.Name;
                var row=InventoryModularUI.Choice(layout.FamilyButton,modularAbilities.Families.content,name);
                RegisterAbilityButton(row.Select,"Spell family "+name,()=>{abilityFamily=key;abilitySelection=null;DrawAbilityEntries();RestoreAbilitySelection();});
                if(key==abilityFamily)row.Select.image.color=layout.SelectedColor;
            }
            entries=abilityEntries.Where(e=>AbilityFamily(e)==abilityFamily).ToList();
        }
        foreach(var entry in entries){
            var view=Instantiate(layout.AbilityRow,modularAbilities.Entries.content,false);
            string name=entry.Action.Name+(!entry.Unlocked?" · Locked":abilityCategory==BattleActionCategory.Magic?" · Unlocked":"");
            view.Title.text=name;view.Source.text=entry.Source;
            var button=RegisterAbilityButton(view.Select,name,()=>ShowAbility(entry),()=>ShowAbility(entry));
            abilityCards.Add((entry,button));
            var pointer=button.gameObject.AddComponent<PlayerInventoryPointer>();
            pointer.Begin=data=>{if(AvailableShortcutAction(entry)==null||partyModal){pointer.CancelDrag();return;}draggedAbility=entry;};
            pointer.End=_=>draggedAbility=null;pointer.Cancel=()=>draggedAbility=null;
        }
        Canvas.ForceUpdateCanvases();AbilitiesModularUI.SizeList(modularAbilities.Families);AbilitiesModularUI.SizeList(modularAbilities.Entries);
        shownAbility=null;
        if(entries.Count>0)ShowAbility(entries.FirstOrDefault(e=>e.Key==abilitySelection)??entries[0]);
        else{
            abilitySelection=null;Clear(detailRoot);BuildDetails();
            string categoryName=abilityCategory==BattleActionCategory.Magic?"spells":abilityCategory==BattleActionCategory.Item?"items":abilityCategory==BattleActionCategory.Skill?"skills":"attacks";
            Detail(string.IsNullOrWhiteSpace(abilitySearch)?"No "+categoryName+" available":"No matches","No abilities match this collection.");RefreshAbilityShortcuts();
        }
    }
    void DrawModularFavorites(BattleActionLoadout loadout){
        var layout=AbilitiesLayout;
        for(int i=0;i<4;i++){
            int slot=i;var current=loadout.Favorite(abilityCategory,slot);
            var view=Instantiate(layout.FavoriteSlot,abilityShortcutRoot,false);view.Name.text=current?.Name??"Empty";
            var button=RegisterAbilityButton(view.Select,"Shortcut "+(slot+1),()=>ChooseAbilityShortcutSlot(slot));
            view.gameObject.AddComponent<PlayerShortcutGlyph>().Bind(slot,view.Key,view.Trigger,view.Plus,view.Fallback);
            var pointer=button.gameObject.AddComponent<PlayerInventoryPointer>();
            pointer.Drop=source=>{if(draggedAbility==null)return;var chosen=draggedAbility;draggedAbility=null;ShowAbility(chosen);ChooseAbilityShortcutSlot(slot);};
        }
        Canvas.ForceUpdateCanvases();AbilitiesModularUI.FitFavorites(abilityShortcutRoot);
    }
}
