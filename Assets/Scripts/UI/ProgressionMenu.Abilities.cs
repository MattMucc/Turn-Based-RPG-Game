using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    sealed class AbilityEntry
    {
        public string Key, Source;
        public BattleActionOption Action;
        public OwnedEquipment Weapon;
        public WeaponMasteryMilestone Milestone;
        public bool Unlocked => Milestone == null || Weapon.MasteryExperience >= Milestone.RequiredExperience;
    }
    BattleActionCategory abilityCategory;
    string abilitySelection;
    AbilityEntry shownAbility;
    readonly List<(AbilityEntry entry, Button button)> abilityCards = new();

    void AbilitiesPage()
    {
        abilityCards.Clear();
        Manager.BindEquipment();
        float width = Mathf.Max(300, body.rect.width), tabWidth = (width - 16) / 3;
        var categories = new[] { BattleActionCategory.Attack, BattleActionCategory.Skill, BattleActionCategory.Magic };
        var labels = new[] { "Attacks", "Skills", "Spells" };
        for (int i = 0; i < categories.Length; i++)
        {
            var selected = categories[i];
            var tab = Button(body, labels[i], i * (tabWidth + 8), 0, tabWidth,
                () => { abilityCategory = selected; abilitySelection = null; Rebuild(); });
            if (selected == abilityCategory) tab.image.color = new Color(.16f, .36f, .44f);
        }
        var entries = new List<AbilityEntry>();
        var loadout = Loadout();
        if (loadout) foreach (var action in loadout.Options.Where(a => a != null && a.Category == abilityCategory))
        {
            // Owned weapons are expanded below, including their locked milestones. Keep unarmed actions.
            if (abilityCategory == BattleActionCategory.Attack && !string.IsNullOrEmpty(action.EquipmentActionId) &&
                Manager.Equipment.Owned.Any(w => w?.Definition && w.Definition.Id + ":" + action.Name == action.EquipmentActionId)) continue;
            entries.Add(new AbilityEntry { Key = "learned:" + action.Name, Action = action, Source = "Available to " + DisplayName(Member) });
        }
        if (abilityCategory == BattleActionCategory.Attack)
        {
            int index = 0;
            foreach (var copy in Manager.Equipment.Owned.Where(w => w?.Definition && w.Definition.Kind == EquipmentKind.Weapon))
            {
                index++;
                var seen = new HashSet<string>();
                string source = copy.Definition.DisplayName + " · Copy " + index;
                void Add(BattleActionOption action, WeaponMasteryMilestone milestone)
                {
                    if (action == null || action.Category != BattleActionCategory.Attack || !seen.Add(action.Name)) return;
                    entries.Add(new AbilityEntry { Key = copy.InstanceId + ":" + action.Name, Action = action, Weapon = copy, Milestone = milestone, Source = source });
                }
                foreach (var action in copy.Definition.Attacks ?? new()) Add(action, null);
                foreach (var milestone in WeaponMastery.Milestones(copy.Definition))
                    foreach (var action in milestone.Attacks ?? new()) Add(action, milestone);
            }
        }
        Label(Rect("Ability collection", body, 0, 46, width, 40), abilityCategory == BattleActionCategory.Attack
            ? "Character attacks and all owned weapons · select to inspect" : "Abilities available to " + DisplayName(Member), 14);
        var list = List(body, "Ability entries", 0, 92, width, Mathf.Max(150, body.rect.height - 92), entries.Count * 78);
        foreach (var entry in entries)
        {
            var button = Button(list, entry.Action.Name + " · " + (entry.Unlocked ? "Unlocked" : "Locked"), 0,
                abilityCards.Count * 78, width - 8, () => ShowAbility(entry), () => ShowAbility(entry), 70);
            var label = button.transform.Find("Label").GetComponent<TMPro.TMP_Text>();
            label.text += "\n<size=12>" + entry.Source + "</size>";
            label.enableAutoSizing = true; label.fontSizeMin = 12; label.fontSizeMax = 16;
            abilityCards.Add((entry, button));
        }
        if (entries.Count == 0)
            Detail("No " + labels[System.Array.IndexOf(categories, abilityCategory)].ToLowerInvariant() + " available",
                "This character has no abilities in this category yet. Learned abilities will appear here automatically.");
        else ShowAbility(entries.FirstOrDefault(e => e.Key == abilitySelection) ?? entries[0]);
    }

    void RestoreAbilitySelection()
    {
        var card = abilityCards.Find(c => c.entry.Key == abilitySelection);
        if (card.button) events.SetSelectedGameObject(card.button.gameObject);
    }

    void ShowAbility(AbilityEntry entry)
    {
        abilitySelection = entry.Key;
        if (shownAbility == entry) return;
        shownAbility = entry;
        foreach (var card in abilityCards) card.button.image.color = card.entry == entry ? new Color(.16f, .36f, .44f) : new Color(.08f, .17f, .25f);
        Clear(detailRoot);
        var content = List(detailRoot, "Ability details", 0, 0, detailRoot.rect.width, detailRoot.rect.height, 300);
        float width = content.rect.width - 24, y = 12;
        var source = new GridUnit(Member.name, Vector2Int.zero); Member.Configure(source);
        Texture art = entry.Action.Image;
        string status = entry.Source;
        if (entry.Weapon != null)
        {
            var equipment = Member.GetComponent<CharacterEquipment>();
            var current = equipment ? equipment.CopyAt(EquipmentSlot.Weapon) : null;
            source.Strength = Member.StatReplacingCopy(CharacterStat.Strength, current, entry.Weapon);
            source.Magic = Member.StatReplacingCopy(CharacterStat.Magic, current, entry.Weapon);
            if (!art) art = entry.Weapon.Definition.Image;
            var owner = Manager.EquipmentOwner(entry.Weapon.InstanceId);
            status += "\n" + (owner ? "Equipped by " + DisplayName(owner) : "Unequipped");
            status += "\n" + (equipment && equipment.Accepts(EquipmentSlot.Weapon, entry.Weapon.Definition)
                ? "Preview using this weapon on " + DisplayName(Member) : "This character cannot equip this weapon; values are a hypothetical comparison.");
            status += "\nMastery level " + WeaponMastery.Level(entry.Weapon);
            if (entry.Milestone != null)
                status += "\n" + (entry.Unlocked ? "Unlocked" : "Locked") + " · " + entry.Milestone.Name +
                    "\nRequires " + entry.Milestone.RequiredExperience + " cumulative weapon XP\n" + entry.Milestone.Description;
            else status += "\nBase weapon Attack · Available while equipped";
        }
        y = ProgressionText(content, "Ability name", entry.Action.Name, y, width, 23, Accent) + 8;
        if (art)
        {
            var image = Rect("Ability artwork", content, (content.rect.width - 112) / 2, y, 112, 112).gameObject.AddComponent<RawImage>();
            image.texture = art; image.raycastTarget = false; y += 124;
        }
        y = ProgressionText(content, "Ability availability", status, y, width, 14) + 8;
        if (entry.Weapon != null && WeaponMastery.MaximumExperience(entry.Weapon.Definition) > 0)
        {
            int maximum = WeaponMastery.MaximumExperience(entry.Weapon.Definition);
            y = ProgressionText(content, "Ability mastery XP", $"Weapon XP {entry.Weapon.MasteryExperience} / {maximum}", y, width, 14);
            ProgressionBar(content, 12, y, width, entry.Weapon.MasteryExperience / (float)maximum); y += 22;
        }
        y = ProgressionText(content, "Ability description", entry.Action.TooltipFor(source), y, width, 16) + 8;
        y = DrawAttackPattern(content, entry.Action, y, width, 26);
        content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(((RectTransform)content.parent).rect.height, y + 12));
    }
}
