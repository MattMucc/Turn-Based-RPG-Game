using System;
using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    string abilitySearch = "", abilityFamily;
    TMP_InputField abilitySearchField;
    RectTransform abilityListRoot, abilityShortcutRoot;
    List<AbilityEntry> abilityEntries = new();
    AbilityEntry draggedAbility;

    void BuildAbilityWorkspace(List<AbilityEntry> entries, float width)
    {
        abilityEntries = entries;
        var search = Rect("Ability search", body, 0, 52, width, 36);
        search.gameObject.AddComponent<Image>().color = new Color(.04f, .08f, .12f);
        abilitySearchField = search.gameObject.AddComponent<TMP_InputField>();
        var viewport = Rect("Search viewport", search, 10, 4, width - 20, 28);
        viewport.gameObject.AddComponent<RectMask2D>();
        var text = Label(Rect("Search text", viewport, 0, 0, width - 20, 28), "", 16);
        var placeholder = Label(Rect("Search placeholder", viewport, 0, 0, width - 20, 28), abilityCategory == BattleActionCategory.Magic ? "Search spell families or tiers…" : "Search abilities…", 14);
        placeholder.color = new Color(.6f, .7f, .75f);
        abilitySearchField.textViewport = viewport; abilitySearchField.textComponent = text; abilitySearchField.placeholder = placeholder;
        abilitySearchField.lineType = TMP_InputField.LineType.SingleLine;
        abilitySearchField.SetTextWithoutNotify(abilitySearch);
        abilitySearchField.onValueChanged.AddListener(value => { abilitySearch = value; DrawAbilityEntries(); });
        abilityListRoot = Rect("Ability browser", body, 0, 100, width, Mathf.Max(100, body.rect.height - 250));
        abilityShortcutRoot = Rect("Favorite shortcuts", body, 0, body.rect.height - 136, width, 136);
        DrawAbilityEntries();
        RefreshAbilityShortcuts();
    }

    string AbilityFamily(AbilityEntry entry) => entry.Spell ? entry.Spell.Id : entry.Key;
    void DrawAbilityEntries()
    {
        Clear(abilityListRoot); abilityCards.Clear(); buttons.RemoveAll(b => !b || !b.gameObject.activeInHierarchy);
        var entries = abilityEntries.Where(e => string.IsNullOrWhiteSpace(abilitySearch) ||
            (e.Action.Name + " " + e.Source).IndexOf(abilitySearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        float width = abilityListRoot.rect.width, height = abilityListRoot.rect.height, left = 0;
        if (abilityCategory == BattleActionCategory.Magic && entries.Count > 0)
        {
            var families = entries.GroupBy(AbilityFamily).OrderBy(g => g.First().Spell ? g.First().Spell.DisplayName : g.First().Action.Name).ToList();
            if (!families.Any(g => g.Key == abilityFamily)) abilityFamily = families[0].Key;
            float familyWidth = width * .42f;
            var familyList = List(abilityListRoot, "Spell families", 0, 0, familyWidth, height, families.Count * 48);
            int index = 0;
            foreach (var family in families)
            {
                string key = family.Key; var entry = family.First();
                string name = entry.Spell ? entry.Spell.DisplayName : entry.Action.Name;
                var b = Button(familyList, name, 0, index++ * 48, familyWidth - 12, () => { abilityFamily = key; abilitySelection = null; DrawAbilityEntries(); RestoreAbilitySelection(); }, null, 42);
                b.gameObject.name = "Spell family " + name;
                var label = b.GetComponentInChildren<TMP_Text>(); label.enableAutoSizing = true; label.fontSizeMin = 11; label.fontSizeMax = 16;
                if (key == abilityFamily) b.image.color = new Color(.16f, .36f, .44f);
            }
            // A matching family always shows all its tiers, including locked versions.
            entries = abilityEntries.Where(e => AbilityFamily(e) == abilityFamily).ToList();
            left = familyWidth + 12; width -= left;
        }
        var list = List(abilityListRoot, "Ability entries", left, 0, width, height, entries.Count * 72);
        foreach (var entry in entries)
        {
            var button = Button(list, entry.Action.Name + " · " + (entry.Unlocked ? "Unlocked" : "Locked"), 0, abilityCards.Count * 72,
                width - 10, () => ShowAbility(entry), () => ShowAbility(entry), 66);
            var label = button.GetComponentInChildren<TMP_Text>();
            label.text += "\n<size=12>" + entry.Source + "</size>"; label.enableAutoSizing = true; label.fontSizeMin = 11; label.fontSizeMax = 16;
            abilityCards.Add((entry, button));
            var pointer = button.gameObject.AddComponent<PlayerInventoryPointer>();
            pointer.Begin = data => { if (AvailableShortcutAction(entry) == null || partyModal) { pointer.CancelDrag(); return; } draggedAbility = entry; };
            pointer.End = _ => draggedAbility = null; pointer.Cancel = () => draggedAbility = null;
        }
        shownAbility = null;
        if (entries.Count > 0) ShowAbility(entries.FirstOrDefault(e => e.Key == abilitySelection) ?? entries[0]);
        else
        {
            abilitySelection = null; Clear(detailRoot); BuildDetails();
            string categoryName = abilityCategory == BattleActionCategory.Magic ? "spells" : abilityCategory == BattleActionCategory.Item ? "items" : abilityCategory == BattleActionCategory.Skill ? "skills" : "attacks";
            Detail(string.IsNullOrWhiteSpace(abilitySearch) ? "No " + categoryName + " available" : "No matches", "No abilities match this collection.");
            RefreshAbilityShortcuts();
        }
    }

    BattleActionOption AvailableShortcutAction(AbilityEntry entry)
    {
        if (entry == null || !entry.Unlocked) return null;
        var loadout = Loadout(); if (!loadout) return null;
        if (entry.Weapon != null)
        {
            if (Member.GetComponent<CharacterEquipment>()?.Weapon != entry.Weapon) return null;
            return loadout.Options.Find(a => a != null && a.Category == abilityCategory && a.EquipmentActionId == entry.Weapon.Definition.Id + ":" + entry.Action.Name);
        }
        if (entry.Spell) return loadout.Options.Find(a => a != null && a.SpellId == entry.Spell.Id && a.SpellTier == entry.Action.SpellTier);
        return loadout.Options.Contains(entry.Action) ? entry.Action : null;
    }

    void RefreshAbilityShortcuts()
    {
        if (!abilityShortcutRoot || page != Page.Abilities) return;
        Clear(abilityShortcutRoot); buttons.RemoveAll(b => !b || !b.gameObject.activeInHierarchy);
        var loadout = Loadout(); if (!loadout) return;
        float width = abilityShortcutRoot.rect.width, half = (width - 8) / 2;
        Label(Rect("Shortcut heading", abilityShortcutRoot, 0, 0, width, 24), "FAVORITE SHORTCUTS", 14).color = Accent;
        for (int i = 0; i < 4; i++)
        {
            int slot = i; var current = loadout.Favorite(abilityCategory, slot);
            var b = Button(abilityShortcutRoot, "Shortcut " + (slot + 1), (i % 2) * (half + 8), 30 + (i / 2) * 48, half,
                () => ChooseAbilityShortcutSlot(slot), null, 42);
            var label = b.GetComponentInChildren<TMP_Text>(); label.text = current?.Name ?? "Empty";
            label.rectTransform.sizeDelta = new Vector2(Mathf.Max(44, half - 110), 32);
            label.enableAutoSizing = true; label.fontSizeMin = 10; label.fontSizeMax = 14;
            label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis;
            ShortcutBadge(b, slot);
            var pointer = b.gameObject.AddComponent<PlayerInventoryPointer>();
            pointer.Drop = source => { if (draggedAbility == null) return; var chosen = draggedAbility; draggedAbility = null; ShowAbility(chosen); ChooseAbilityShortcutSlot(slot); };
        }
    }

    void ChooseAbilityShortcutSlot(int slot)
    {
        if (!open || InBattle || partyModal || slot < 0 || slot > 3) return;
        var entry = abilityEntries.FirstOrDefault(e => e.Key == abilitySelection);
        var action = AvailableShortcutAction(entry); var loadout = Loadout();
        if (!loadout) return;
        var member = Member; var selectedCategory = abilityCategory; var existing = loadout.Favorite(selectedCategory, slot);
        var panel = PartyDialog("Shortcut " + (slot + 1), 350); var confirmation = partyModal;
        string text = Binding(slot) + "\n\nCurrent: " + (existing?.Name ?? "Empty") + "\nNew: " + (action?.Name ?? "Unavailable");
        if (action == null) text += "\n\nOnly learned abilities from the current loadout can be assigned.";
        else if (existing != null && existing != action) text += "\n\n" + existing.Name + " loses this shortcut but remains available in battle.";
        if (action != null && action.FavoriteSlot >= 0 && action.FavoriteSlot != slot) text += $"\nSlot {action.FavoriteSlot + 1} becomes empty.";
        Label(Rect("Shortcut comparison", panel, 18, 60, 404, 180), text, 16);
        Button(panel, "Confirm shortcut", 18, 246, 250, () =>
        {
            if (!open || InBattle || partyModal != confirmation || !partyModal || Member != member || abilityCategory != selectedCategory) return;
            if (AvailableShortcutAction(entry) != action || loadout.Favorite(selectedCategory, slot) != existing) { ClosePartyModal(); return; }
            if (!loadout.AssignFavorite(action, slot)) return;
            ClosePartyModal(); RefreshAbilityShortcuts();
        }).interactable = action != null;
        Button(panel, "Clear slot", 280, 246, 142, () =>
        {
            if (!open || InBattle || partyModal != confirmation || !partyModal || Member != member || loadout.Favorite(selectedCategory, slot) != existing) return;
            if (existing != null) loadout.AssignFavorite(existing, -1);
            ClosePartyModal(); RefreshAbilityShortcuts();
        }).interactable = existing != null;
        Button(panel, "Cancel", 18, 294, 404, ClosePartyModal); FocusPartyDialog();
    }
}
