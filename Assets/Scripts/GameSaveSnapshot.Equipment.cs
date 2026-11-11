using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.GridPrototype;

public static partial class GameSaveSnapshot
{
    static void ValidateEquipment(GameSaveData data,GameManager manager,Dictionary<string,SaveIdentity> ids)
    {
        if(data.HasCoins&&data.Coins<0)throw new InvalidDataException("Invalid coin balance.");
        if(!data.HasEquipment)return;
        var catalog=manager.Equipment.Catalog;
        if(data.Equipment==null||data.Equipment.Any(e=>e==null||e.MasteryExperience<0||string.IsNullOrEmpty(e.InstanceId)||string.IsNullOrEmpty(e.DefinitionId))||data.Equipment.Select(e=>e.InstanceId).Distinct().Count()!=data.Equipment.Count)
            throw new InvalidDataException("Invalid saved equipment collection.");
        foreach(var entry in data.Equipment)if(catalog.Count(d=>d&&d.Id==entry.DefinitionId)!=1)throw new InvalidDataException("A saved equipment definition is missing or duplicated.");
        if(data.HasMaterialUpgrades){
            if(data.Materials==null||data.Materials.Any(m=>m==null||string.IsNullOrWhiteSpace(m.Id)||m.Quantity<0)||data.Materials.Select(m=>m.Id).Distinct().Count()!=data.Materials.Count)throw new InvalidDataException("Invalid upgrade materials.");
            foreach(var entry in data.Equipment){var definition=catalog.Single(d=>d&&d.Id==entry.DefinitionId);if(entry.UpgradeLevel<0||entry.UpgradeLevel>definition.Upgrades.Count)throw new InvalidDataException("Invalid equipment upgrade level.");}
        }
        var used=new HashSet<string>();
        foreach(var saved in data.Characters){
            var profile=ids[saved.Id].GetComponent<CharacterEquipment>();
            if(saved.EquipmentSlots==null||saved.EquipmentSlots.Length==0){if(profile)throw new InvalidDataException("Missing character equipment slots.");continue;}
            if(!profile||saved.EquipmentSlots.Length!=4)throw new InvalidDataException("Incompatible equipment profile.");
            for(int i=0;i<4;i++){
                var id=saved.EquipmentSlots[i];if(string.IsNullOrEmpty(id))continue;
                var item=data.Equipment.Find(e=>e.InstanceId==id);
                if(item==null||!used.Add(id)||!profile.Accepts((EquipmentSlot)i,catalog.Single(d=>d&&d.Id==item.DefinitionId)))throw new InvalidDataException("Invalid equipment ownership or weapon access.");
            }
        }
    }
    static void RestoreEquipment(GameSaveData data,GameManager manager,Dictionary<string,SaveIdentity> ids)
    {
        manager.Equipment.Coins=data.HasCoins?data.Coins:0;
        if(data.HasMaterialUpgrades)manager.Equipment.Materials=data.Materials.Select(m=>new MaterialStack{Id=m.Id,Name=m.Name,Quantity=m.Quantity}).ToList();else manager.RestoreInitialMaterials();
        if(!data.HasEquipment){manager.RestoreInitialEquipment();return;}
        manager.Equipment.Owned=data.Equipment.Select(e=>new OwnedEquipment{InstanceId=e.InstanceId,MasteryExperience=e.MasteryExperience,UpgradeLevel=data.HasMaterialUpgrades?e.UpgradeLevel:0,Definition=manager.Equipment.Catalog.Single(d=>d&&d.Id==e.DefinitionId)}).ToList();
        manager.BindEquipment();
        foreach(var saved in data.Characters)if(saved.EquipmentSlots!=null&&saved.EquipmentSlots.Length==4)ids[saved.Id].GetComponent<CharacterEquipment>().RestoreSlots(saved.EquipmentSlots);
    }
}
