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
        public SpellDefinition Spell;
        public LearnedSpell Learned;
        public SpellTier Tier;
        public bool Unlocked => Tier != null ? Learned.Experience >= Tier.RequiredExperience : Milestone == null || Weapon.MasteryExperience >= Milestone.RequiredExperience;
    }
    BattleActionCategory abilityCategory;
    string abilitySelection;
    AbilityEntry shownAbility;
    readonly List<(AbilityEntry entry, Button button)> abilityCards = new();

    void AbilitiesPage()
    {
        abilityCards.Clear();
        Manager.BindEquipment();
        float width = Mathf.Max(300, body.rect.width), tabWidth = (width - 24) / 4;
        var categories = new[] { BattleActionCategory.Attack, BattleActionCategory.Skill, BattleActionCategory.Magic, BattleActionCategory.Item };
        var labels = new[] { "Attacks", "Skills", "Spells", "Items" };
        if(!BuildModularAbilityShell())for (int i = 0; i < categories.Length; i++)
        {
            var selected = categories[i];
            var tab = Button(body, labels[i], i * (tabWidth + 8), 0, tabWidth,
                () => { abilityCategory = selected; abilitySelection = null; abilitySearch = ""; abilityFamily = null; Rebuild(); });
            if (selected == abilityCategory) tab.image.color = new Color(.16f, .36f, .44f);
        }
        var entries = new List<AbilityEntry>();
        var loadout = Loadout();
        if (loadout) foreach (var action in loadout.Options.Where(a => a != null && a.Category == abilityCategory))
        {
            if(abilityCategory==BattleActionCategory.Magic&&!string.IsNullOrEmpty(action.SpellId))continue;
            // The equipped weapon is expanded below, including locked milestones. Keep shared attacks.
            if (abilityCategory == BattleActionCategory.Attack && !string.IsNullOrEmpty(action.EquipmentActionId)) continue;
            entries.Add(new AbilityEntry { Key = "learned:" + action.Name, Action = action, Source = "Available to " + DisplayName(Member) });
        }
        if(loadout&&abilityCategory==BattleActionCategory.Magic)foreach(var learned in loadout.LearnedSpells) {
            var spell=SpellDefinition.Find(learned.Id);if(!spell)continue;
            foreach(var tier in spell.OrderedTiers) {
                var action=spell.CreateAction(tier);
                entries.Add(new AbilityEntry{Key="spell:"+spell.Id+":"+action.SpellTier,Action=action,Spell=spell,Learned=learned,Tier=tier,Source=spell.DisplayName+" · "+DisplayName(Member)});
            }
        }
        if (abilityCategory == BattleActionCategory.Attack)
        {
            var equipped=Member.GetComponent<CharacterEquipment>()?.Weapon;
            foreach (var copy in Manager.Equipment.Owned.Where(w => w==equipped && w?.Definition && w.Definition.Kind == EquipmentKind.Weapon))
            {
                var seen = new HashSet<string>();
                string source = copy.Definition.DisplayName;
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
        BuildAbilityWorkspace(entries, width);
    }

    void RestoreAbilitySelection()
    {
        var card = abilityCards.Find(c => c.entry.Key == abilitySelection);
        if (abilitySearchField && abilitySearchField.isFocused) return;
        if (card.button) events.SetSelectedGameObject(card.button.gameObject);
    }

    void ShowAbility(AbilityEntry entry)
    {
        abilitySelection = entry.Key;
        if (shownAbility == entry) return;
        shownAbility = entry;
        RefreshAbilityShortcuts();
        foreach (var card in abilityCards) card.button.image.color = card.entry == entry ? (modularAbilities?AbilitiesLayout.SelectedColor:new Color(.16f,.36f,.44f)) : (modularAbilities?AbilitiesLayout.AbilityRow.Select.image.color:new Color(.08f,.17f,.25f));
        Clear(detailRoot);
        var content = List(detailRoot, "Ability details", 0, 0, detailRoot.rect.width, detailRoot.rect.height, 300);
        float width = content.rect.width - 24, y = 12;
        var source = new GridUnit(Member.name, Vector2Int.zero); Member.Configure(source);
        Texture art = entry.Action.Image;
        string status = entry.Source;
        if(entry.Spell) {
            status+="\n"+(entry.Unlocked?"Learned tier":"Locked tier")+" · Requires "+entry.Tier.RequiredExperience+" cumulative Spell XP";
            status+=entry.Spell.MaximumExperience>0?"\nMastery belongs to this character. Successful casts earn XP once per cast.":"\nSingle-tier Spell · No further mastery unlocks.";
        }
        if (entry.Weapon != null)
        {
            var equipment = Member.GetComponent<CharacterEquipment>();
            var current = equipment ? equipment.CopyAt(EquipmentSlot.Weapon) : null;
            source.Soul = Member.StatReplacingCopy(CharacterStat.Soul, current, entry.Weapon);
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
        if(entry.Spell&&entry.Spell.MaximumExperience>0) {
            y=ProgressionText(content,"Spell mastery XP",$"Spell XP {entry.Learned.Experience} / {entry.Spell.MaximumExperience}",y,width,14);
            ProgressionBar(content,12,y,width,entry.Learned.Experience/(float)entry.Spell.MaximumExperience);y+=22;
        }
        if (entry.Weapon != null && WeaponMastery.MaximumExperience(entry.Weapon.Definition) > 0)
        {
            int maximum = WeaponMastery.MaximumExperience(entry.Weapon.Definition);
            y = ProgressionText(content, "Ability mastery XP", $"Weapon XP {entry.Weapon.MasteryExperience} / {maximum}", y, width, 14);
            ProgressionBar(content, 12, y, width, entry.Weapon.MasteryExperience / (float)maximum); y += 22;
        }
        y = ProgressionText(content, "Ability description", entry.Action.TooltipFor(source), y, width, 16) + 8;
        y = DrawAttackPattern(content, entry.Action, y, width, 26);
        if (entry.Weapon != null) foreach (var milestone in WeaponMastery.Milestones(entry.Weapon.Definition))
            y = ProgressionText(content, "Mastery milestone", (entry.Weapon.MasteryExperience >= milestone.RequiredExperience ? "Unlocked" : "Locked") +
                $" · {milestone.RequiredExperience} XP · {milestone.Name}\n{milestone.Description}", y + 10, width, 14);
        if (entry.Spell) foreach (var tier in entry.Spell.OrderedTiers)
            y = ProgressionText(content, "Spell milestone", entry.Spell.CreateAction(tier).Name + $" · {tier.RequiredExperience} XP" +
                (entry.Learned.Experience >= tier.RequiredExperience ? " · Learned" : " · Locked"), y + 6, width, 14);
        content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(((RectTransform)content.parent).rect.height, y + 12));
        ComposeDetails(content,DetailPanelKind.Abilities);
    }
}
