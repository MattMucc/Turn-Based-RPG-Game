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
    enum PartyTab { Formation, Equipment, Attributes }
    PartyTab partyTab;
    RectTransform partyContent, partyModal, partyDragGhost;
    BattleCharacterInfo partyDragging;
    readonly Dictionary<PlayerInventoryPointer, BattleCharacterInfo> partyPortraits = new();
    readonly Dictionary<CharacterStat, int> partyAllocation = new();
    string partyFocus, partyNotice;
    bool partyEquipmentContext;
    int PartyAllocationCost => partyAllocation.Values.Sum();

    void Party()
    {
        float width = body.rect.width;
        for (int i = 0; i < 3; i++)
        {
            var tab = (PartyTab)i;
            var button = Button(body, tab.ToString(), i * (width / 3), 0, width / 3 - 6,
                () => ChangePartyTab(tab), null, 36);
            if (tab == partyTab) button.image.color = new Color(.18f, .34f, .39f);
        }
        partyContent = Rect("Party workspace", body, 0, 54, width, body.rect.height - 54);
        switch (partyTab)
        {
            case PartyTab.Formation: PartyFormation(); break;
            case PartyTab.Equipment: PartyEquipment(); break;
            case PartyTab.Attributes: PartyAttributes(); break;
        }
    }

    void ChangePartyTab(PartyTab tab)
    {
        if (tab == partyTab) return;
        if (GuardPartyAllocation(() => ChangePartyTab(tab))) return;
        ClosePartyModal(); partyTab = tab; partyFocus = null; partyNotice = null; Rebuild();
    }

    bool GuardPartyAllocation(Action next)
    {
        if (PartyAllocationCost == 0) return false;
        var panel = PartyDialog("Discard allocation preview?", 210);
        Label(Rect("Discard explanation", panel, 18, 60, 404, 58), "Your Attribute Points have not been spent.", 17);
        Button(panel, "Discard changes", 18, 128, 250, () => { partyAllocation.Clear(); ClosePartyModal(); next(); });
        Button(panel, "Keep editing", 280, 128, 142, ClosePartyModal);
        FocusPartyDialog(); return true;
    }

    void SelectPartyMember(BattleCharacterInfo member)
    {
        if (!member || partyDragging || inventoryModal || partyModal) return;
        if (member != Member && GuardPartyAllocation(() => SelectPartyMember(member))) return;
        memberIndex = Manager.Progression.Members.IndexOf(member);
        partyFocus = DisplayName(member); partyNotice = null; Rebuild();
    }

    void PartyFormation()
    {
        float width = partyContent.rect.width;
        var active = Manager.GetBattleParty();
        var reserve = Manager.Progression.Members.Where(m => m && !active.Contains(m)).ToList();
        float cardWidth = (width - 16) / 3, cardHeight = 154;
        var list = List(partyContent, "Formation cards", 0, 0, width, Mathf.Max(200, partyContent.rect.height - 62), 376);
        int row = 0;
        foreach (var group in new[] { active, reserve })
        {
            float y = row * 188;
            Label(Rect(row == 0 ? "Active heading" : "Reserve heading", list, 0, y, width, 26),
                (row == 0 ? "ACTIVE" : "RESERVES") + "   " + group.Count + " / 3", 14).color = Accent;
            for (int i = 0; i < group.Count; i++)
            {
                var member = group[i];
                var card = Button(list, DisplayName(member), i * (cardWidth + 8), y + 30, cardWidth - 4,
                    () => SelectPartyMember(member), null, cardHeight);
                if (member == Member) card.image.color = new Color(.18f, .34f, .39f);
                var text = card.transform.Find("Label").GetComponent<TMP_Text>();
                text.rectTransform.anchoredPosition = new Vector2(8, -68);
                text.rectTransform.sizeDelta = new Vector2(cardWidth - 24, 22);
                text.enableAutoSizing = true; text.fontSizeMin = 11; text.fontSizeMax = 16;
                var picture = Rect("Portrait", card.transform, 8, 8, 52, 52).gameObject.AddComponent<RawImage>();
                picture.texture = Portrait(member); picture.raycastTarget = false;
                Label(Rect("Role", card.transform, 68, 10, cardWidth - 80, 24),
                    active.IndexOf(member) == 0 ? "CAPTAIN" : row == 0 ? "ACTIVE" : "RESERVE", 10).color = new Color(.9f, .78f, .5f);
                Label(Rect("Level", card.transform, 68, 36, cardWidth - 80, 22), "Lv " + member.Level, 13);
                var unit = new GridUnit(member.name, Vector2Int.zero); member.Configure(unit);
                int health = member.StoredHealth < 0 ? unit.MaxHealth : Mathf.Clamp(member.StoredHealth, 0, unit.MaxHealth);
                Label(Rect("Vitals", card.transform, 8, 94, cardWidth - 24, 42),
                    $"HP {health} / {unit.MaxHealth}\nMP {unit.MagicPoints} / {unit.MaxMagicPoints}" + (health == 0 ? " · KO" : ""), 12);
                var track = Rect("XP track", card.transform, 8, 143, cardWidth - 24, 4);
                track.gameObject.AddComponent<Image>().color = new Color(.04f, .08f, .11f);
                float progress = member.Level >= Manager.Progression.LevelCap ? 1 :
                    Manager.Progression.ProgressFor(member) / (float)Manager.Progression.Cost(Mathf.Max(1, member.Level));
                Rect("XP fill", track, 0, 0, (cardWidth - 24) * Mathf.Clamp01(progress), 4).gameObject.AddComponent<Image>().color = Accent;
                var pointer = card.gameObject.AddComponent<PlayerInventoryPointer>(); partyPortraits[pointer] = member;
                pointer.Context = () => OpenPartyActions(member);
                pointer.Hover = () => { if (!partyModal && !inventoryModal && !partyDragging) ShowMember(member); };
                pointer.Exit = () => { if (!partyModal && !inventoryModal && !partyDragging) ShowMember(Member); };
                pointer.Begin = data => BeginPartyDrag(member, data);
                pointer.Move = MovePartyDrag;
                pointer.End = _ => CancelPartyDrag(); pointer.Cancel = CancelPartyDrag;
                pointer.Drop = source => { if (partyPortraits.TryGetValue(source, out var from)) StagePartySwap(from, member); };
            }
            row++;
        }
        Button(partyContent, "Character actions", 0, partyContent.rect.height - 48, 200, () => OpenPartyActions(Member));
        if (!string.IsNullOrEmpty(partyNotice)) Label(Rect("Party notice", partyContent, 212, partyContent.rect.height - 48, width - 212, 46), partyNotice, 13);
        ShowMember(Member);
    }

    void OpenPartyActions(BattleCharacterInfo member)
    {
        if (!member || !open || InBattle || inventoryModal || partyDragging) return;
        memberIndex = Manager.Progression.Members.IndexOf(member); ShowMember(member);
        var panel = PartyDialog(DisplayName(member), 310);
        Button(panel, "Examine", 18, 62, 404, () => { ClosePartyModal(); ShowMember(member); });
        Button(panel, "Swap with…", 18, 108, 404, () => ChoosePartyReplacement(member));
        Button(panel, "Make captain", 18, 154, 404, () =>
        {
            if (open && !InBattle) Manager.TrySetCaptain(member);
            ClosePartyModal(); partyFocus = DisplayName(member); Rebuild();
        }).interactable = Manager.BattleParty.Contains(member) && Manager.BattleParty.IndexOf(member) != 0;
        Button(panel, "Close", 18, 232, 404, ClosePartyModal); FocusPartyDialog();
    }

    void ChoosePartyReplacement(BattleCharacterInfo member)
    {
        var candidates = Manager.BattleParty.Contains(member) ? Manager.ReserveParty : Manager.BattleParty;
        var panel = PartyDialog("Swap " + DisplayName(member), 320);
        int i = 0;
        foreach (var candidate in candidates.ToArray())
        {
            var target = candidate;
            Button(panel, DisplayName(target), 18, 62 + i++ * 48, 404, () => StagePartySwap(member, target));
        }
        Button(panel, "Cancel", 18, 250, 404, ClosePartyModal); FocusPartyDialog();
    }

    void StagePartySwap(BattleCharacterInfo first, BattleCharacterInfo second)
    {
        CancelPartyDrag();
        if (!open || InBattle || !first || !second || first == second) return;
        bool firstActive = Manager.BattleParty.Contains(first), secondActive = Manager.BattleParty.Contains(second);
        if (firstActive == secondActive) { partyNotice = "Swap an active member with a reserve."; Rebuild(); return; }
        var outgoing = firstActive ? first : second; var incoming = firstActive ? second : first;
        int slot = Manager.BattleParty.IndexOf(outgoing);
        if (!Manager.ReserveParty.Contains(incoming)) return;
        var panel = PartyDialog("Confirm party swap", 250); var expectedDialog = partyModal;
        Label(Rect("Swap preview", panel, 18, 62, 404, 96),
            DisplayName(outgoing) + " → Reserves\n" + DisplayName(incoming) + " → Active" + (slot == 0 ? " · Captain" : ""), 18);
        Button(panel, "Confirm swap", 18, 172, 250, () =>
        {
            if (!open || InBattle || !partyModal || partyModal != expectedDialog) return;
            if (slot >= Manager.BattleParty.Count || Manager.BattleParty[slot] != outgoing || !Manager.ReserveParty.Contains(incoming))
            { ClosePartyModal(); partyNotice = "Party changed. Choose the swap again."; Rebuild(); return; }
            bool changed = Manager.TrySetActiveMember(slot, incoming);
            ClosePartyModal(); if (changed) memberIndex = Manager.Progression.Members.IndexOf(incoming);
            partyFocus = DisplayName(Member); partyNotice = changed ? "Party updated." : "Party cannot be changed right now."; Rebuild();
        });
        Button(panel, "Cancel", 280, 172, 142, ClosePartyModal); FocusPartyDialog();
    }

    RectTransform PartyDialog(string name, float height)
    {
        ClosePartyModal(); HideItemTooltip();
        var group = shell.GetComponent<CanvasGroup>(); if (!group) group = shell.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false; group.blocksRaycasts = false;
        partyModal = Rect("Party interaction", canvasObject.transform, 0, 0, 0, 0);
        partyModal.anchorMin = Vector2.zero; partyModal.anchorMax = Vector2.one; partyModal.offsetMin = partyModal.offsetMax = Vector2.zero;
        partyModal.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .65f);
        var panel = Rect("Party dialog", partyModal, 0, 0, 440, height);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f); panel.anchoredPosition = Vector2.zero;
        panel.gameObject.AddComponent<Image>().color = Panel;
        Label(Rect("Dialog title", panel, 18, 14, 404, 38), name, 21);
        return panel;
    }
    void FocusPartyDialog()
    { var first = partyModal ? partyModal.GetComponentsInChildren<Button>().FirstOrDefault(b => b.IsInteractable()) : null; if (first) events.SetSelectedGameObject(first.gameObject); }
    void ClosePartyModal()
    {
        if (partyModal) { partyModal.gameObject.SetActive(false); if (Application.isPlaying) Destroy(partyModal.gameObject); else DestroyImmediate(partyModal.gameObject); }
        partyModal = null;
        if (shell) { var group = shell.GetComponent<CanvasGroup>(); if (group && !inventoryModal) { group.interactable = true; group.blocksRaycasts = true; } }
        if (events && page == Page.Party) RestorePartySelection();
        if (events && page == Page.Abilities && !inventoryModal) RestoreAbilitySelection();
    }
    void BeginPartyDrag(BattleCharacterInfo member, PointerEventData data)
    {
        if (partyModal || inventoryModal) return;
        if (partyDragGhost) { partyDragGhost.gameObject.SetActive(false); if (Application.isPlaying) Destroy(partyDragGhost.gameObject); else DestroyImmediate(partyDragGhost.gameObject); }
        partyDragging = member;
        partyDragGhost = Rect("Dragged party portrait", shell, 0, 0, 60, 60);
        var image = partyDragGhost.gameObject.AddComponent<RawImage>(); image.texture = Portrait(member); image.raycastTarget = false;
        MovePartyDrag(data);
    }
    void MovePartyDrag(PointerEventData data)
    { if (partyDragGhost && RectTransformUtility.ScreenPointToLocalPointInRectangle(shell, data.position, data.pressEventCamera, out var p)) partyDragGhost.localPosition = p + new Vector2(12, -12); }
    void CancelPartyDrag()
    {
        partyDragging = null;
        if (partyDragGhost) { partyDragGhost.gameObject.SetActive(false); if (Application.isPlaying) Destroy(partyDragGhost.gameObject); else DestroyImmediate(partyDragGhost.gameObject); }
        partyDragGhost = null;
        foreach (var pointer in partyPortraits.Keys) if (pointer) pointer.CancelDrag();
    }

    void PartyEquipment()
    {
        Manager.BindEquipment(); var profile = Member.GetComponent<CharacterEquipment>();
        if (!profile) { ShowMember(Member); Label(Rect("No equipment", partyContent, 0, 0, partyContent.rect.width, 60), "No equipment slots available."); return; }
        InventoryEntries(); float width=partyContent.rect.width;
        float size=Mathf.Clamp(width*.19f,60,88), centerWidth=Mathf.Max(80,width-2*size-32);
        var picture=Rect("Equipped character",partyContent,size+16,20,centerWidth,260).gameObject.AddComponent<RawImage>();
        picture.texture=Portrait(Member);picture.raycastTarget=false;
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            var currentSlot=slot;var copy=profile.CopyAt(slot);
            bool right=slot==EquipmentSlot.Weapon||slot==EquipmentSlot.Armor;
            bool lower=slot==EquipmentSlot.Weapon||slot==EquipmentSlot.AccessoryTwo;
            float x=right?width-size:0,y=lower?160:40;
            var button=Button(partyContent,SlotName(slot),x,y,size,
                ()=>OpenPartyEquipmentPicker(currentSlot),()=>{equipmentSlot=currentSlot;PreviewEquipment(profile.InstanceAt(currentSlot));},size);
            button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
            if(copy!=null)DrawInventoryIcon(button.transform,inventoryEquipment.First(p=>p.Value==copy).Key,6,6,size-12);
            else {var empty=Label(Rect("Empty slot",button.transform,4,4,size-8,size-8),"Empty",12);empty.alignment=TextAlignmentOptions.Center;}
            Label(Rect("Slot caption",partyContent,x,y+size+4,size,22),SlotName(slot),12).alignment=TextAlignmentOptions.Center;
            var pointer=button.gameObject.AddComponent<PlayerInventoryPointer>();pointer.Context=()=>OpenPartyEquipmentActions(currentSlot);
        }
        Label(Rect("Weapon access",partyContent,0,288,width,48),"Weapon types: "+string.Join(", ",profile.AllowedWeaponTypes),14);
        equipmentSlot = EquipmentSlot.Weapon; PreviewEquipment(profile.InstanceAt(equipmentSlot));
    }
    void OpenPartyEquipmentActions(EquipmentSlot slot)
    {
        var profile = Member.GetComponent<CharacterEquipment>(); if (!profile) return;
        equipmentSlot = slot;
        var panel = PartyDialog(SlotName(slot), 270);
        Button(panel, "Examine", 18, 62, 404, () => { ClosePartyModal(); PreviewEquipment(profile.InstanceAt(slot)); });
        Button(panel, "Change equipment", 18, 108, 404, () => OpenPartyEquipmentPicker(slot));
        Button(panel, "Unequip", 18, 154, 404, () => BeginPartyEquipmentConfirmation(slot, profile.CopyAt(slot), true)).interactable = profile.CopyAt(slot) != null;
        Button(panel, "Close", 18, 210, 404, ClosePartyModal); FocusPartyDialog();
    }
    void OpenPartyEquipmentPicker(EquipmentSlot slot)
    {
        if (!open || InBattle) return;
        equipmentSlot = slot; partyFocus = SlotName(slot); Manager.BindEquipment(); var profile = Member.GetComponent<CharacterEquipment>(); if (!profile) return;
        var available = Manager.Equipment.Owned.Where(c => c != null && c.Definition && profile.Accepts(slot, c.Definition)).ToList();
        var panel = PartyDialog("Choose " + SlotName(slot), 500);
        var list = List(panel, "Compatible equipment", 18, 64, 404, 352, (available.Count + 1) * 58);
        var current = profile.CopyAt(slot);
        Button(list, "Unequip", 0, 0, 388, () => BeginPartyEquipmentConfirmation(slot, current, true)).interactable = current != null;
        int i = 1;
        foreach (var copy in available)
        {
            var entry = copy; var owner = Manager.EquipmentOwner(copy.InstanceId);
            string suffix = owner ? " · " + DisplayName(owner) : " · available";
            var b = Button(list, entry.Definition.DisplayName + $" +{entry.UpgradeLevel}" + suffix, 0, i++ * 58, 388,
                () => BeginPartyEquipmentConfirmation(slot, entry, false), () => PreviewEquipment(entry.InstanceId), 50);
            b.interactable = !owner;
        }
        Button(panel, "Cancel", 18, 438, 404, ClosePartyModal); FocusPartyDialog();
    }
    void BeginPartyEquipmentConfirmation(EquipmentSlot slot, OwnedEquipment copy, bool unequip)
    {
        if (copy == null || !open || InBattle) return;
        InventoryEntries(); inventoryItem = inventoryEquipment.First(p => p.Value == copy).Key;
        inventoryEquipMember = Member; inventoryEquipSlot = slot; inventoryUnequip = unequip;
        inventoryEquipNotice = null; inventoryStep = 6; ClosePartyModal(); partyEquipmentContext = true; DrawInventoryModal();
    }

    void PartyAttributes()
    {
        float width = partyContent.rect.width;
        Label(Rect("Allocation balance", partyContent, 0, 0, width, 34), $"{Mathf.Max(0, Member.UnspentPoints - PartyAllocationCost)} ATTRIBUTE POINTS AVAILABLE", 18);
        var list = List(partyContent, "Attribute rows", 0, 42, width, Mathf.Max(210, partyContent.rect.height - 110), 7 * 46);
        int i = 0;
        foreach (CharacterStat stat in Enum.GetValues(typeof(CharacterStat)))
        {
            var captured = stat; int staged = partyAllocation.GetValueOrDefault(stat);
            Label(Rect("Stat " + stat, list, 8, i * 46 + 6, width - 180, 32), AttributePreview.Name(stat), 16);
            Label(Rect("Stat value " + stat, list, width - 170, i * 46 + 6, 60, 32), ((long)Member.Stat(stat) + staged).ToString(), 16).color = staged > 0 ? new Color(.5f, .89f, .67f) : Ink;
            Button(list, "−", width - 108, i * 46, 42, () => StagePartyPoint(captured, -1), null, 38).gameObject.name = "Decrease " + stat;
            buttons.Last().interactable = staged > 0;
            Button(list, "+", width - 60, i * 46, 42, () => StagePartyPoint(captured, 1), null, 38).gameObject.name = "Increase " + stat;
            buttons.Last().interactable = PartyAllocationCost < Member.UnspentPoints && (long)Member.Stat(stat) + staged < int.MaxValue;
            i++;
        }
        Button(partyContent, "Apply allocation", 0, partyContent.rect.height - 48, width - 150, ConfirmPartyAllocation).interactable = PartyAllocationCost > 0;
        Button(partyContent, "Reset preview", width - 142, partyContent.rect.height - 48, 142, () => { partyAllocation.Clear(); partyFocus = "Increase Vitality"; Rebuild(); });
        PreviewPartyAllocation();
    }
    void StagePartyPoint(CharacterStat stat, int amount)
    {
        if (!open || InBattle || partyModal) return;
        int staged = partyAllocation.GetValueOrDefault(stat);
        if (amount > 0 && (PartyAllocationCost >= Member.UnspentPoints || (long)Member.Stat(stat) + staged >= int.MaxValue)) return;
        if (amount < 0 && staged == 0) return;
        partyAllocation[stat] = staged + amount; partyFocus = (amount > 0 ? "Increase " : "Decrease ") + stat; Rebuild();
    }
    void PreviewPartyAllocation()
    {
        var before = new GridUnit(Member.name, Vector2Int.zero); Member.Configure(before);
        var after = AttributePreview.AfterPoints(Member, partyAllocation);
        string text = $"Attribute Points: {Member.UnspentPoints} → {Member.UnspentPoints - PartyAllocationCost}\n\n";
        foreach (CharacterStat stat in Enum.GetValues(typeof(CharacterStat)))
            text += $"{AttributePreview.Name(stat)}: {Member.Stat(stat)} → {(long)Member.Stat(stat) + partyAllocation.GetValueOrDefault(stat)}\n";
        text += $"\nMaximum HP: {before.MaxHealth} → {after.MaxHealth}\nMaximum MP: {before.MaxMagicPoints} → {after.MaxMagicPoints}";
        var loadout = Loadout();
        if (loadout) foreach (var action in loadout.Options.Where(a => a != null && a.Effect == ActionEffect.Damage && a.StatScaling > 0))
        {
            int a = action.CalculateDamage(before, null, action.Damage), b = action.CalculateDamage(after, null, action.Damage);
            if (a != b) text += $"\n{action.Name}: {a} → {b} damage";
        }
        Detail(DisplayName(Member), text, Portrait(Member));
    }
    void ConfirmPartyAllocation()
    {
        if (PartyAllocationCost <= 0) return;
        var member = Member; var allocation = new Dictionary<CharacterStat, int>(partyAllocation);
        int expectedPoints = member.UnspentPoints;
        string expectedStats = string.Join(",", Enum.GetValues(typeof(CharacterStat)).Cast<CharacterStat>().Select(s => member.Stat(s)));
        var panel = PartyDialog("Apply allocation?", 390); var confirmation = partyModal;
        Label(Rect("Allocation confirmation", panel, 18, 62, 404, 230), DisplayName(member) + "\n\n" +
            string.Join("\n", allocation.Where(p => p.Value > 0).Select(p => AttributePreview.Name(p.Key) + " +" + p.Value)) +
            "\n\nCost: " + PartyAllocationCost + " Attribute Points", 17);
        Button(panel, "Confirm allocation", 18, 318, 250, () =>
        {
            if (!open || InBattle || !partyModal || partyModal != confirmation || Member != member) return;
            string currentStats = string.Join(",", Enum.GetValues(typeof(CharacterStat)).Cast<CharacterStat>().Select(s => member.Stat(s)));
            if (member.UnspentPoints != expectedPoints || currentStats != expectedStats || !member.SpendPoints(allocation))
            { ClosePartyModal(); partyAllocation.Clear(); partyNotice = "Stats changed. Review the allocation again."; Rebuild(); return; }
            partyAllocation.Clear(); ClosePartyModal(); partyFocus = "Increase Vitality"; Rebuild();
        });
        Button(panel, "Cancel", 280, 318, 142, ClosePartyModal); FocusPartyDialog();
    }

    void RestorePartySelection()
    {
        if (page != Page.Party || !events || partyModal || inventoryModal) return;
        var selected = buttons.Find(b => b && b.gameObject.activeInHierarchy && b.IsInteractable() && b.name == partyFocus);
        if (!selected && partyTab == PartyTab.Formation) selected = buttons.Find(b => b && b.name == DisplayName(Member));
        if (selected) events.SetSelectedGameObject(selected.gameObject);
    }
}
