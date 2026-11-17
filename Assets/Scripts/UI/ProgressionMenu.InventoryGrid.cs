using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    enum InventoryCategory { All, Consumables, Weapons, Armor, Accessories, Materials, SpellBooks, Other }
    InventoryCategory inventoryCategory;
    readonly Dictionary<string,BattleActionOption> inventoryProxies=new();
    readonly Dictionary<BattleActionOption,OwnedEquipment> inventoryEquipment=new();
    readonly Dictionary<BattleActionOption,MaterialStack> inventoryMaterials=new();
    static string InventoryCategoryName(InventoryCategory category)=>category==InventoryCategory.SpellBooks?"Spell Books":category.ToString();
    InventoryCategory InventoryKind(BattleActionOption item)
    {
        if(inventoryEquipment.TryGetValue(item,out var copy))return copy.Definition.Kind==EquipmentKind.Weapon?InventoryCategory.Weapons:copy.Definition.Kind==EquipmentKind.Armor?InventoryCategory.Armor:InventoryCategory.Accessories;
        if(inventoryMaterials.ContainsKey(item))return InventoryCategory.Materials;
        if(!string.IsNullOrEmpty(item.LearnSpellId))return InventoryCategory.SpellBooks;
        return item.Effect==ActionEffect.Heal||item.Effect==ActionEffect.RestoreMagic||item.Effect==ActionEffect.Damage?InventoryCategory.Consumables:InventoryCategory.Other;
    }
    string InventoryEntryKey(BattleActionOption item)
    {
        if(inventoryEquipment.TryGetValue(item,out var copy))return "equipment:"+copy.InstanceId;
        if(inventoryMaterials.TryGetValue(item,out var material))return "material:"+material.Id;
        return "item:"+(string.IsNullOrEmpty(item.ItemId)?item.Name:item.ItemId);
    }
    List<BattleActionOption> InventoryEntries()
    {
        Manager.BindEquipment();inventoryEquipment.Clear();inventoryMaterials.Clear();
        var entries=Manager.Inventory.Items.Where(i=>i!=null).ToList();
        BattleActionOption Proxy(string key) {
            if(!inventoryProxies.TryGetValue(key,out var result))inventoryProxies[key]=result=new BattleActionOption{Category=BattleActionCategory.Attack,Effect=ActionEffect.Support};
            return result;
        }
        foreach(var copy in Manager.Equipment.Owned.Where(c=>c!=null&&c.Definition)) {
            var item=Proxy("equipment:"+copy.InstanceId);var definition=copy.Definition;
            item.Name=definition.DisplayName;item.Image=definition.Image;item.Quantity=1;
            var owner=Manager.EquipmentOwner(copy.InstanceId);
            item.Description=definition.Description+$"\n\n{definition.Kind} · Upgrade +{copy.UpgradeLevel} / {definition.Upgrades.Count}\n"+(owner?"Equipped by "+DisplayName(owner):"Unequipped");
            foreach(CharacterStat stat in System.Enum.GetValues(typeof(CharacterStat))){int bonus=EquipmentUpgrades.Bonus(copy,stat);if(bonus!=0)item.Description+=$"\n{AttributePreview.Name(stat)}: {bonus:+0;-0}";}
            if(definition.Kind==EquipmentKind.Weapon)item.Description+=$"\nMastery level {WeaponMastery.Level(copy)} · XP {copy.MasteryExperience} / {WeaponMastery.MaximumExperience(definition)}";
            item.Description+="\n\nUpgrades require a blacksmith.";
            inventoryEquipment[item]=copy;entries.Add(item);
        }
        foreach(var material in Manager.Equipment.Materials.Where(m=>m!=null)) {
            var item=Proxy("material:"+material.Id);item.Name=string.IsNullOrEmpty(material.Name)?material.Id:material.Name;item.Quantity=material.Quantity;
            item.Description=$"A crafting material used in blacksmith upgrades.\n\nShared stock: {material.Quantity}";
            inventoryMaterials[item]=material;entries.Add(item);
        }
        var order=Manager.Inventory.SlotOrder??=new();
        // Append newly acquired entries; existing order is saved with the shared inventory.
        foreach(var item in entries){string key=InventoryEntryKey(item);if(!order.Contains(key))order.Add(key);}
        return entries.OrderBy(i=>order.IndexOf(InventoryEntryKey(i))).ToList();
    }
    bool SwapInventoryEntries(BattleActionOption first,BattleActionOption second)
    {
        if(first==null||second==null||first==second||!inventoryButtons.ContainsKey(first)||!inventoryButtons.ContainsKey(second))return false;
        var order=Manager.Inventory.SlotOrder;int a=order.IndexOf(InventoryEntryKey(first)),b=order.IndexOf(InventoryEntryKey(second));
        if(a<0||b<0)return false;
        (order[a],order[b])=(order[b],order[a]);
        // Preserve the existing item-list order too, without moving ownership or consuming stock.
        if(!Manager.Inventory.SwapItems(first,second))Manager.Inventory.NotifyChanged();
        return true;
    }
    bool MoveInventoryToEmpty(BattleActionOption item,string empty)
    {
        if(item==null||!inventoryButtons.ContainsKey(item)||!empty.StartsWith("empty:"))return false;
        var order=Manager.Inventory.SlotOrder;int a=order.IndexOf(InventoryEntryKey(item)),b=order.IndexOf(empty);
        if(a<0||b<0)return false;
        (order[a],order[b])=(order[b],order[a]);Manager.Inventory.NotifyChanged();return true;
    }
    void DrawInventoryIcon(Transform parent,BattleActionOption item,float x,float y,float size)
    {
        var rect=Rect("Item artwork",parent,x,y,size,size);
        if(item.Image){var image=rect.gameObject.AddComponent<RawImage>();image.texture=item.Image;image.raycastTarget=false;}
        else {var icon=rect.gameObject.AddComponent<InventorySlotIcon>();icon.Kind=InventoryCategoryName(InventoryKind(item));icon.Mana=item.Effect==ActionEffect.RestoreMagic;icon.raycastTarget=false;}
    }
}
