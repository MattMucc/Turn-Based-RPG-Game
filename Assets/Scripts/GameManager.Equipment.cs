using System;
using System.Linq;
using Game.GridPrototype;

public partial class GameManager
{
    public PartyEquipment Equipment = new();
    System.Collections.Generic.List<MaterialStack> initialMaterials;
    public void RestoreInitialMaterials(){BindEquipment();Equipment.Materials=initialMaterials.Select(m=>new MaterialStack{Id=m.Id,Name=m.Name,Quantity=m.Quantity}).ToList();}
    public bool TryUpgradeEquipment(string instance,int expectedLevel,out string error)
    {
        error="";
        if((BattleManager.Instance&&BattleManager.Instance.InBattle)||IsSwitchingCharacter||(saves&&saves.Busy)){error="Upgrades are only available outside combat and saving.";return false;}
        BindEquipment();var item=Equipment.Find(instance);
        if(!EquipmentUpgrades.Requirements(Equipment,item,expectedLevel,out _,out error))return false;
        var owner=EquipmentOwner(instance);if(owner)owner.MaterializeVitals();
        if(!EquipmentUpgrades.TryApply(Equipment,item,expectedLevel,out error))return false;
        if(owner)owner.ClampStoredVitals();return true;
    }
    System.Collections.Generic.List<OwnedEquipment> initialEquipment;
    readonly System.Collections.Generic.Dictionary<CharacterEquipment,string[]> initialSlots = new();
    public void BindEquipment()
    {
        if(initialMaterials==null)initialMaterials=Equipment.Materials.Select(m=>new MaterialStack{Id=m.Id,Name=m.Name,Quantity=m.Quantity}).ToList();
        if(initialEquipment==null)initialEquipment=Equipment.Owned.Select(e=>new OwnedEquipment{InstanceId=e.InstanceId,Definition=e.Definition,MasteryExperience=e.MasteryExperience,UpgradeLevel=e.UpgradeLevel}).ToList();
        foreach(var member in Progression.Members)if(member){var profile=member.GetComponent<CharacterEquipment>();if(profile){if(!initialSlots.ContainsKey(profile))initialSlots.Add(profile,profile.CaptureSlots());profile.Bind(Equipment);}}
    }
    public void RestoreInitialEquipment(){BindEquipment();Equipment.Owned=initialEquipment.Select(e=>new OwnedEquipment{InstanceId=e.InstanceId,Definition=e.Definition,MasteryExperience=e.MasteryExperience,UpgradeLevel=e.UpgradeLevel}).ToList();foreach(var pair in initialSlots)if(pair.Key)pair.Key.RestoreSlots(pair.Value);}
    public BattleCharacterInfo EquipmentOwner(string instance)
    {
        if(string.IsNullOrEmpty(instance))return null;
        return Progression.Members.FirstOrDefault(m=>m&&m.GetComponent<CharacterEquipment>()&&m.GetComponent<CharacterEquipment>().CaptureSlots().Contains(instance));
    }
    public bool TryEquip(BattleCharacterInfo member,EquipmentSlot slot,string instance,out string error)
    {
        error="";
        if((BattleManager.Instance&&BattleManager.Instance.InBattle)||IsSwitchingCharacter||(saves&&saves.Busy)) {error="Equipment can only change outside combat and saving.";return false;}
        if(!member||!Progression.Members.Contains(member)||!Enum.IsDefined(typeof(EquipmentSlot),slot)){error="Invalid character or slot.";return false;}
        BindEquipment();var equipment=member.GetComponent<CharacterEquipment>();
        if(!equipment){error="No equipment profile on this character.";return false;}
        if(!string.IsNullOrEmpty(instance)) {
            var item=Equipment.Find(instance);
            if(item==null||!equipment.Accepts(slot,item.Definition)){error="This character cannot equip that item in this slot.";return false;}
            var owner=EquipmentOwner(instance);
            if(owner&&!(owner==member&&equipment.InstanceAt(slot)==instance)){error="Already equipped by "+ProgressionMenu.DisplayName(owner)+". Unequip it first.";return false;}
        }
        if(equipment.InstanceAt(slot)==instance)return true;
        equipment.ApplySlot(slot,instance);return true;
    }
}
