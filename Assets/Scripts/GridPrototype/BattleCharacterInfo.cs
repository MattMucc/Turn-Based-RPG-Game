using UnityEngine;

namespace Tenebrasia.GridPrototype
{
    // Optional authored metadata. Unassigned stats do not pretend to be combat formulas.
    public sealed class BattleCharacterInfo : MonoBehaviour
    {
        public Texture Portrait;
        public string DisplayName;
        [Min(0)] public int Level;
        public string Race, Role;
        public int Vitality = -1, Strength = -1, Magic = -1, PhysicalDefense = -1, MagicalDefense = -1, Initiative = -1;
        [TextArea] public string Resistances;
        [TextArea] public string Statuses;
        public static string Value(int value) => value < 0 ? "Not assigned" : value.ToString();
        public string Details(GridUnit unit, string fallbackName)
        {
            string name = string.IsNullOrWhiteSpace(DisplayName) ? fallbackName : DisplayName;
            string statuses = string.IsNullOrWhiteSpace(Statuses) ? "None" : Statuses;
            if (unit.Blocking) statuses = "Blocking" + (statuses == "None" ? "" : "\n" + statuses);
            return $"<b>{name}</b>\nHealth  {unit.Health} / {unit.MaxHealth}\nLevel  {(Level > 0 ? Level.ToString() : "Not assigned")}\nRace  {Assigned(Race)}\nRole  {Assigned(Role)}\n\n<b>Stats</b>\nVitality  {Value(Vitality)}\nStrength  {Value(Strength)}\nMagic  {Value(Magic)}\nPhysical Defense  {Value(PhysicalDefense)}\nMagical Defense  {Value(MagicalDefense)}\nInitiative  {Value(Initiative)}\n\n<b>Statuses</b>\n{statuses}\n\n<b>Resistances</b>\n{Assigned(Resistances)}";
        }
        private static string Assigned(string value) => string.IsNullOrWhiteSpace(value) ? "Not assigned" : value;
    }
}
