using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.GridPrototype
{
    [Serializable] public sealed class OwnedEquipment
    {
        public string InstanceId;
        public EquipmentDefinition Definition;
        [Min(0)] public int MasteryExperience;
        [Min(0)] public int UpgradeLevel;
    }
    [Serializable] public sealed class PartyEquipment
    {
        public List<EquipmentDefinition> Catalog = new();
        public List<OwnedEquipment> Owned = new();
        public List<MaterialStack> Materials = new();
        [Min(0)] public int Coins;
        public OwnedEquipment Find(string id) => string.IsNullOrEmpty(id) ? null : Owned.Find(e => e != null && e.InstanceId == id);
    }
    public sealed class CharacterEquipment : MonoBehaviour
    {
        public WeaponType[] AllowedWeaponTypes = Array.Empty<WeaponType>();
        [SerializeField] string weapon, armor, accessoryOne, accessoryTwo;
        PartyEquipment inventory;
        public int Revision { get; private set; }
        public string InstanceAt(EquipmentSlot slot) => slot switch { EquipmentSlot.Weapon => weapon, EquipmentSlot.Armor => armor, EquipmentSlot.AccessoryOne => accessoryOne, _ => accessoryTwo };
        public EquipmentDefinition At(EquipmentSlot slot) => inventory?.Find(InstanceAt(slot))?.Definition;
        public OwnedEquipment CopyAt(EquipmentSlot slot) => inventory?.Find(InstanceAt(slot));
        public OwnedEquipment Weapon => inventory?.Find(weapon);
        public void Bind(PartyEquipment shared) { if (ReferenceEquals(inventory, shared)) return; inventory = shared; Revision++; GetComponent<BattleActionLoadout>()?.EquipmentChanged(false); }
        public bool Accepts(EquipmentSlot slot, EquipmentDefinition item) => item && item.Fits(slot) && (slot != EquipmentSlot.Weapon || AllowedWeaponTypes.Contains(item.WeaponType));
        public int Bonus(CharacterStat stat)
        {
            long total = 0;
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot))) total += EquipmentUpgrades.Bonus(CopyAt(slot),stat);
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, total));
        }
        // Called by the guarded party equipment service, or while restoring a validated save.
        public void RestoreSlots(string[] ids)
        {
            weapon=ids[0]; armor=ids[1]; accessoryOne=ids[2]; accessoryTwo=ids[3]; Revision++;
            GetComponent<BattleActionLoadout>()?.EquipmentChanged(false);
        }
        public string[] CaptureSlots() => Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().Select(InstanceAt).ToArray();
        public void ApplySlot(EquipmentSlot slot, string id)
        {
            var info=GetComponent<BattleCharacterInfo>();
            // Materialize full-but-uninitialized pools before maxima change; equipping never heals.
            if(info)info.MaterializeVitals();
            var ids=CaptureSlots(); ids[(int)slot]=id;
            RestoreSlots(ids);
            GetComponent<BattleActionLoadout>()?.EquipmentChanged(slot==EquipmentSlot.Weapon);
            if(info)info.ClampStoredVitals();
        }
    }
}
