using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public enum EquipmentSlot { Weapon, Armor, AccessoryOne, AccessoryTwo }
    public enum EquipmentKind { Weapon, Armor, Accessory }
    public enum WeaponType { Shortsword, Greatsword, Staff, Bow }

    [CreateAssetMenu(menuName = "Game/Equipment Definition")]
    public sealed class EquipmentDefinition : ScriptableObject
    {
        [Tooltip("Stable save identity. Do not change after shipping saves.")]
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public Texture Image;
        public EquipmentKind Kind;
        public WeaponType WeaponType;
        public StatValues Bonuses = new();
        [Tooltip("Starting Attacks available while equipped.")]
        public List<BattleActionOption> Attacks = new();
        [Min(1)] public int MasteryExperiencePerAttack = 1;
        public List<WeaponMasteryMilestone> Mastery = new();
        public bool Fits(EquipmentSlot slot) => slot == EquipmentSlot.Weapon ? Kind == EquipmentKind.Weapon : slot == EquipmentSlot.Armor ? Kind == EquipmentKind.Armor : Kind == EquipmentKind.Accessory;
    }
}
