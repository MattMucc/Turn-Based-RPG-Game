using System;
using System.Collections.Generic;
using TMPro;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Keeps the authored button artwork; the list contents come from the active character.
public class BattleMenuUI : MonoBehaviour
{
    [SerializeField] private BattleMenuButton attackButton;
    [SerializeField] private BattleMenuButton skillsButton;
    [SerializeField] private BattleMenuButton itemsButton;
    [SerializeField] private BattleMenuButton runButton;
    [SerializeField] private Sprite blockKeyboardIcon, blockControllerIcon, runKeyboardIcon;
    [SerializeField] private Sprite endTurnKeyboardIcon, endTurnControllerIcon;
    private Sprite runControllerIcon;
    private readonly Sprite[] rootKeyboardIcons = new Sprite[3], rootControllerIcons = new Sprite[3];
    [SerializeField] private Sprite[] favoriteKeyboardIcons = new Sprite[4];
    [SerializeField] private Sprite[] favoriteControllerIcons = new Sprite[4];
    [SerializeField] private Sprite backKeyboardIcon;
    [SerializeField] private Sprite qteKeyboardIcon, qteControllerIcon;
    private bool editingFavorites;
    private BattleActionOption assigningFavorite;
    private readonly List<BattleMenuButton> pool = new();
    private readonly List<Func<bool>> availability = new();
    private readonly List<string> descriptions = new();
    private readonly List<BattleActionOption> previewOptions = new();
    private BattleHud hud;
    private BattleControllerNavigation controllerNavigation;
    private BattleMenuButton remainingTurnButton;
    private readonly List<RaycastResult> raycasts = new();
    private InSceneGridBattle battle;
    private PlayerInputReader input;
    private RectTransform content, viewport;
    private ScrollRect scroll;
    private TMP_Text tooltip, targetCounter;
    private ScrollRect tooltipScroll;
    private GameObject tooltipPanel;
    private Image shortcutDimmer;
    private int rows, highlighted = -1;
    private bool browsing, moving, initialized;
    private BattleActionCategory category;

    private void Start() { if (!battle) Hide(); }
    private void Ensure()
    {
        if (initialized) return;
        initialized = true;
        runControllerIcon = runButton.ControllerIcon;
        if (!blockKeyboardIcon) blockKeyboardIcon = runButton.KeyboardIcon;
        pool.AddRange(new[] { attackButton, skillsButton, itemsButton, runButton });
        for (int i = 0; i < 3; i++) { rootKeyboardIcons[i] = pool[i].KeyboardIcon; rootControllerIcons[i] = pool[i].ControllerIcon; }
        content = (RectTransform)attackButton.transform.parent;
        // A compact, scrollable list at the bottom left; use the original buttons and their layout.
        var holder = new GameObject("Battle command viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport = (RectTransform)holder.transform; viewport.SetParent(transform, false);
        viewport.anchorMin = viewport.anchorMax = new Vector2(0, 0);
        viewport.pivot = Vector2.zero; viewport.anchoredPosition = new Vector2(12, 12); viewport.sizeDelta = new Vector2(180, 240);
        holder.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.65f);
        content.SetParent(viewport, false); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true; layout.childForceExpandWidth = true;
        layout.childControlHeight = true; layout.childForceExpandHeight = false; layout.spacing = 2;
        layout.padding = new RectOffset(4, 4, 4, 4);
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll = holder.GetComponent<ScrollRect>(); scroll.content = content; scroll.viewport = viewport;
        scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 25; scroll.movementType = ScrollRect.MovementType.Clamped;
        tooltip = MakeText("Action details", new Vector2(0, 0), new Vector2(202, 12), new Vector2(280, 260), true);
        tooltipPanel = tooltip.transform.parent.gameObject; tooltipPanel.SetActive(false);
        var tooltipRect = (RectTransform)tooltipPanel.transform;
        tooltipPanel.GetComponent<Image>().raycastTarget = true;
        tooltipPanel.AddComponent<RectMask2D>(); tooltipScroll = tooltipPanel.AddComponent<ScrollRect>();
        tooltipScroll.viewport = tooltipRect; tooltipScroll.content = tooltip.rectTransform;
        tooltipScroll.horizontal = false; tooltipScroll.movementType = ScrollRect.MovementType.Clamped; tooltipScroll.scrollSensitivity = 25;
        tooltip.rectTransform.anchorMin = new Vector2(0, 1); tooltip.rectTransform.anchorMax = Vector2.one;
        tooltip.rectTransform.pivot = new Vector2(0.5f, 1); tooltip.rectTransform.anchoredPosition = new Vector2(0, -10);
        targetCounter = MakeText("Target selection count", Vector2.zero, new Vector2(12, 210), new Vector2(180, 30), true);
        targetCounter.fontSize = 13; targetCounter.alignment = TextAlignmentOptions.Center;
        targetCounter.transform.parent.gameObject.SetActive(false);
        var hudRoot = new GameObject("Battle health and turn order", typeof(RectTransform));
        var hudRect = (RectTransform)hudRoot.transform; hudRect.SetParent(transform, false);
        hudRect.anchorMin = Vector2.zero; hudRect.anchorMax = Vector2.one; hudRect.offsetMin = hudRect.offsetMax = Vector2.zero;
        hud = hudRoot.AddComponent<BattleHud>();
        remainingTurnButton = Instantiate(attackButton, transform);
        remainingTurnButton.name = "Remaining turn";
        remainingTurnButton.Configure("End Turn", () => battle.EndTurn(), true);
        remainingTurnButton.SetIcons(endTurnKeyboardIcon, endTurnControllerIcon);
        var endRect = (RectTransform)remainingTurnButton.transform;
        endRect.anchorMin = endRect.anchorMax = endRect.pivot = Vector2.zero;
        endRect.anchoredPosition = new Vector2(12, 12); endRect.sizeDelta = new Vector2(180, 28);
        remainingTurnButton.gameObject.SetActive(false);
        var dimmer = new GameObject("Shortcut focus dimmer", typeof(RectTransform), typeof(Image));
        dimmer.transform.SetParent(transform, false);
        var dimRect = (RectTransform)dimmer.transform; dimRect.anchorMin = Vector2.zero; dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
        shortcutDimmer = dimmer.GetComponent<Image>(); shortcutDimmer.color = new Color(0, 0, 0, 0.45f);
        shortcutDimmer.raycastTarget = false; dimmer.SetActive(false);
        viewport.SetAsLastSibling(); tooltipPanel.transform.SetAsLastSibling(); targetCounter.transform.parent.SetAsLastSibling();
    }

    private TMP_Text MakeText(string name, Vector2 anchor, Vector2 position, Vector2 size, bool background)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)panel.transform; rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
        var image = panel.GetComponent<Image>(); image.color = new Color(0.025f, 0.035f, 0.055f, 0.92f); image.raycastTarget = false;
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); textObject.transform.SetParent(rect, false);
        var textRect = (RectTransform)textObject.transform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12, 10); textRect.offsetMax = new Vector2(-12, -10);
        var text = textObject.GetComponent<TextMeshProUGUI>(); text.font = attackButton.Label.font; text.fontSize = 14;
        text.color = Color.white; text.outlineColor = Color.black; text.outlineWidth = 0.2f; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    public void Bind(InSceneGridBattle encounter)
    {
        Ensure(); Unbind(); battle = encounter; input = GameManager.Instance ? GameManager.Instance.Input : null;
        battle.PointerOverUI = IsOverUI; battle.ActionResolved += Root;
        Show(); Root(); hud.SetQteIcons(qteKeyboardIcon, qteControllerIcon); hud.Bind(battle, attackButton.Label.font);
        if (!controllerNavigation) controllerNavigation = GetComponent<BattleControllerNavigation>();
        if (!controllerNavigation) controllerNavigation = gameObject.AddComponent<BattleControllerNavigation>();
        controllerNavigation.Bind(battle, this, hud);
    }
    public void Unbind()
    {
        if (battle) { battle.PointerOverUI = null; battle.ActionResolved -= Root; }
        if (controllerNavigation) controllerNavigation.Unbind();
        battle = null; Hide();
    }
    public void Show() { Ensure(); gameObject.SetActive(true); }
    public void Hide() => gameObject.SetActive(false);
    private bool IsOverUI(Vector2 point)
    {
        if (!isActiveAndEnabled) return false;
        if (viewport.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(viewport, point, GetComponentInParent<Canvas>().worldCamera)) return true;
        if (!EventSystem.current) return false;
        raycasts.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, raycasts);
        return raycasts.Exists(r => r.module is GraphicRaycaster);
    }
    private void BeginList()
    {
        rows = 0; highlighted = -1; availability.Clear(); descriptions.Clear(); previewOptions.Clear(); if (battle) battle.HoverAction(null); tooltipPanel.SetActive(false);
        foreach (var button in pool) button.gameObject.SetActive(false);
    }
    private void Row(string label, Action action, string description, Func<bool> available, bool icon = false, BattleActionOption preview = null)
    {
        int index = rows++;
        if (index == pool.Count) pool.Add(Instantiate(attackButton, content));
        var button = pool[index]; button.gameObject.SetActive(true); button.transform.SetSiblingIndex(index);
        var layout = button.GetComponent<LayoutElement>(); if (!layout) layout = button.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = 28;
        button.Configure(label, () => action(), icon);
        button.HoverChanged = entered => { if (entered) ShowTooltip(index); else { tooltipPanel.SetActive(false); if (battle) battle.HoverAction(null); } };
        availability.Add(available); descriptions.Add(description); previewOptions.Add(preview);
    }
    private void FinishList()
    {
        viewport.sizeDelta = new Vector2(180, Mathf.Min(260, rows * 30 + 6));
        scroll.verticalNormalizedPosition = 1;
        UpdateAvailability();
    }
    private void Root()
    {
        if (!battle) return;
        browsing = moving = editingFavorites = false; assigningFavorite = null; battle.GridInputEnabled = true; battle.KeyboardGridInputEnabled = false;
        BeginList();
        Row("Attack", () => Open(BattleActionCategory.Attack), "Choose an attack available to this character. Movement is free on green tiles. Tab / controller LB switches grid navigation on or off.", () => battle.ActionReady, true);
        Row("Skills", () => Open(BattleActionCategory.Skill), "Choose a skill available to this character.", () => battle.ActionReady, true);
        Row("Items", () => Open(BattleActionCategory.Item), "Use a carried item. Costs one action and one item.", () => battle.ActionReady, true);
        for (int i = 0; i < 3; i++) pool[i].SetIcons(rootKeyboardIcons[i], rootControllerIcons[i], true);
        Row("Block", () => battle.Block(), "Spend your action to reduce incoming prototype damage until your next turn. You may still move. Paralyzed prevents Block. Keyboard 4 / controller LT + RB.", () => battle.ActionReady && battle.Combatants[0].CanBlock, true);
        pool[3].SetIcons(blockKeyboardIcon, blockControllerIcon, true);
        Row("Run", () => battle.Finish(false), "Leave this prototype encounter and restore overworld positions. Keyboard 5 / controller LT + B.", () => battle.PlayerReady, true);
        pool[4].SetIcons(runKeyboardIcon, runControllerIcon, true);
        Row("Dash", () => battle.Dash(), "Spend your action to gain 3 movement points. Move normally afterward; unused points carry over.", () => battle.ActionReady);
        Row("End Turn", () => battle.EndTurn(), "T / controller right-stick click (R3): end this turn. Unspent movement carries over; your next turn grants three more points.", () => battle.PlayerReady, true);
        pool[6].SetIcons(endTurnKeyboardIcon, endTurnControllerIcon);
        FinishList();
    }
    private void Open(BattleActionCategory selectedCategory)
    {
        if (!battle.ActionReady) return;
        editingFavorites = false; assigningFavorite = null;
        battle.Loadout.InitializeFavorites();
        category = selectedCategory; browsing = true; moving = false; battle.GridInputEnabled = false; battle.KeyboardGridInputEnabled = false;
        BeginList();
        foreach (var option in battle.Loadout.Options)
        {
            if (option == null || option.Category != category) continue;
            var captured = option;
            Row(option.Name + (category == BattleActionCategory.Item ? $" ×{option.Quantity}" : ""), () => Select(captured), option.Tooltip,
                () => battle.ActionReady && captured.Available && (battle.ChosenAction == null || battle.ChosenAction == captured), icon: option.FavoriteSlot >= 0, preview: captured);
            if (option.FavoriteSlot >= 0) FavoriteIcons(pool[rows - 1], option.FavoriteSlot);
        }
        if (rows == 0) Row("None available", () => { }, "This character has no available options in this category.", () => false);
        if (battle.ChosenAction?.Pattern == BattleTargetPattern.Multiple)
        {
            Row("Undo Hit", () => battle.UndoTarget(), "Remove the most recently assigned hit. Delete / controller X while targeting.",
                () => battle.ActionReady && battle.TargetCount > 0);
            Row("Clear Targets", () => battle.ClearTargets(), "Remove all assigned hits without spending your action.",
                () => battle.ActionReady && battle.TargetCount > 0);
        }
        if (battle.ChosenAction == null) Row("Favorites", EditFavorites, "Choose an ability or item, then assign shortcut 1-4 / LT + A, B, X, Y. All options remain available.", () => battle.PlayerReady);
        Row("Back", Back, "Cancel the selection and return to the command menu. Backspace / controller B. Escape cancels the action while keeping this list open.", () => battle.PlayerReady, true);
        pool[rows - 1].SetIcons(backKeyboardIcon, runControllerIcon);
        FinishList();
    }
    private void FavoriteIcons(BattleMenuButton button, int slot)
    {
        button.SetIcons(slot < favoriteKeyboardIcons.Length ? favoriteKeyboardIcons[slot] : null,
            slot < favoriteControllerIcons.Length ? favoriteControllerIcons[slot] : null, true);
    }
    private void EditFavorites()
    {
        battle.CancelAction(); editingFavorites = true; assigningFavorite = null;
        battle.GridInputEnabled = battle.KeyboardGridInputEnabled = false; BeginList();
        foreach (var option in battle.Loadout.Options)
        {
            if (option == null || option.Category != category) continue;
            var captured = option;
            Row(option.Name, () => AssignPage(captured), "Choose a favorite slot for " + option.Name + ". This does not spend an action.", () => true, option.FavoriteSlot >= 0);
            if (option.FavoriteSlot >= 0) FavoriteIcons(pool[rows - 1], option.FavoriteSlot);
        }
        FavoriteBack(); FinishList();
    }
    private void AssignPage(BattleActionOption option)
    {
        assigningFavorite = option; BeginList();
        for (int i = 0; i < 4; i++)
        {
            int slot = i; var current = battle.Loadout.Favorite(category, slot);
            Row("Slot " + (slot + 1) + ": " + (current?.Name ?? "Empty"), () => AssignSlot(slot),
                "Assign " + option.Name + " here. Replaces this slot's previous shortcut; all abilities remain usable.", () => true, true);
            FavoriteIcons(pool[rows - 1], slot);
        }
        Row("Remove shortcut", () => AssignSlot(-1), "Keep this ability available without a shortcut.", () => true);
        FavoriteBack(); FinishList();
    }
    private void AssignSlot(int slot)
    { if (assigningFavorite == null) return; battle.Loadout.AssignFavorite(assigningFavorite, slot); EditFavorites(); }
    private void FavoriteBack()
    {
        Row("Back", Back, "Backspace / controller B: return to the previous menu.", () => true, true);
        pool[rows - 1].SetIcons(backKeyboardIcon, runControllerIcon);
    }
    public void UseFavorite(int slot)
    {
        if (!browsing || !battle.PlayerReady) return;
        if (editingFavorites) { AssignSlot(slot); return; }
        if (battle.ChosenAction != null) return;
        var option = battle.Loadout.Favorite(category, slot);
        if (option != null && option.Available && battle.ActionReady) Select(option);
    }
    private static int PressedFavorite(Keyboard k, Gamepad pad)
    {
        if (k != null)
        {
            if (k.digit1Key.wasPressedThisFrame) return 0;
            if (k.digit2Key.wasPressedThisFrame) return 1;
            if (k.digit3Key.wasPressedThisFrame) return 2;
            if (k.digit4Key.wasPressedThisFrame) return 3;
        }
        if (pad != null && pad.leftTrigger.isPressed)
        {
            if (pad.buttonSouth.wasPressedThisFrame) return 0;
            if (pad.buttonEast.wasPressedThisFrame) return 1;
            if (pad.buttonWest.wasPressedThisFrame) return 2;
            if (pad.buttonNorth.wasPressedThisFrame) return 3;
        }
        return -1;
    }
    private void Select(BattleActionOption option)
    {
        if (!battle.ChooseAction(option)) return;
        Open(category);
        battle.GridInputEnabled = battle.KeyboardGridInputEnabled = true;
        highlighted = -1; tooltip.text = ControllerGlyphs.Adapt(option.Tooltip); tooltipPanel.SetActive(true);
        for (int i = 0; i < rows; i++) pool[i].SetSelected(descriptions[i] == option.Tooltip);
        UpdateAvailability();
    }
    private void CancelSelection()
    {
        if (!battle.PlayerReady) return;
        if (battle.ChosenAction != null) { battle.CancelAction(); Open(category); }
        else if (moving) Root();
    }
    private void Back()
    {
        if (!battle.PlayerReady) return;
        if (editingFavorites) { if (assigningFavorite != null) EditFavorites(); else Open(category); return; }
        battle.CancelAction(); Root();
    }
    private void ShowTooltip(int index)
    { if (index >= 0 && index < descriptions.Count) { tooltip.text = ControllerGlyphs.Adapt(descriptions[index]); tooltipPanel.SetActive(true); if (battle) battle.HoverAction(previewOptions[index]); } }
    private void FitTooltip()
    {
        var bounds = ((RectTransform)transform).rect;
        var rect = (RectTransform)tooltipPanel.transform;
        float width = Mathf.Clamp(bounds.width - 230, 160, 280);
        float contentHeight = tooltip.GetPreferredValues(tooltip.text, width - 24, 0).y + 20;
        float height = Mathf.Min(contentHeight, Mathf.Clamp(bounds.height - 155, 80, 260));
        rect.sizeDelta = new Vector2(width, height);
        tooltip.rectTransform.sizeDelta = new Vector2(-24, tooltip.GetPreferredValues(tooltip.text, width - 24, 0).y + 20);
    }
    private void UpdateAvailability()
    {
        for (int i = 0; i < rows; i++) pool[i].SetAvailable(availability[i]());
        bool commandsVisible = battle.CurrentActorIndex == 0 && battle.Combatants[0].ActionAvailable && !battle.DefenseActive;
        viewport.gameObject.SetActive(commandsVisible);
        remainingTurnButton.gameObject.SetActive(battle.PlayerReady && !battle.Combatants[0].ActionAvailable);
        remainingTurnButton.SetAvailable(battle.PlayerReady);
        if (!commandsVisible)
        {
            tooltipPanel.SetActive(false); battle.HoverAction(null);
            if (highlighted >= 0) pool[highlighted].SetHighlighted(false);
            highlighted = -1;
        }
        bool showCount = commandsVisible && battle.ChosenAction?.Pattern == BattleTargetPattern.Multiple;
        targetCounter.transform.parent.gameObject.SetActive(showCount);
        if (showCount)
        {
            targetCounter.text = $"{battle.TargetCount}/{battle.TargetLimit} targets selected";
            ((RectTransform)targetCounter.transform.parent).anchoredPosition = new Vector2(12, 16 + viewport.sizeDelta.y);
        }
    }
    public void ControllerRootShortcut(int index)
    {
        if (!battle || !battle.PlayerReady || battle.InspectionOpen || browsing || battle.ChosenAction != null) return;
        if (index >= 0 && index < 5) pool[index].Invoke();
    }
    public bool ControllerBrowsing => browsing;
    public void ControllerEndTurn() { if (CanUseEndTurnShortcut()) battle.EndTurn(); }
    public void ControllerBack()
    {
        if (battle.ChosenAction != null) CancelSelection();
        else if (browsing || moving) Back();
        else battle.TargetFocus.Dismiss();
    }
    public void ClearControllerCommands()
    {
        if (highlighted >= 0 && highlighted < pool.Count) pool[highlighted].SetHighlighted(false);
        highlighted = -1; tooltipPanel.SetActive(false);
        if (remainingTurnButton) remainingTurnButton.SetHighlighted(false);
        if (battle) battle.HoverAction(null);
    }
    public void ControllerCommands(int delta, bool activate)
    {
        if (!battle.PlayerReady) return;
        if (!battle.Combatants[0].ActionAvailable)
        {
            remainingTurnButton.SetHighlighted(true);
            if (activate) ControllerEndTurn();
            return;
        }
        if (!viewport.gameObject.activeInHierarchy || rows == 0) return;
        if (highlighted >= 0 && delta == 0 && !activate) return;
        if (highlighted >= 0) pool[highlighted].SetHighlighted(false);
        highlighted = highlighted < 0 ? 0 : (highlighted + delta + rows) % rows;
        pool[highlighted].SetHighlighted(true); ShowTooltip(highlighted);
        scroll.verticalNormalizedPosition = rows <= 1 ? 1 : 1 - highlighted / (float)(rows - 1);
        if (activate) pool[highlighted].Invoke();
    }
    private bool CanUseEndTurnShortcut()
        => battle && battle.PlayerReady && !battle.InspectionOpen && !browsing && !editingFavorites
            && battle.ChosenAction == null
            && (viewport.gameObject.activeInHierarchy || remainingTurnButton.gameObject.activeInHierarchy);

    private static bool PressedEndTurn(Keyboard keyboard, Gamepad gamepad)
        => (keyboard != null && keyboard.tKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.rightStickButton.wasPressedThisFrame);

    public bool ShouldShowShortcutFocus(bool applicationFocused)
    {
        return battle && battle.PlayerReady && battle.ActionReady && !battle.InspectionOpen
            && battle.ChosenAction == null && !battle.ControllerInputLocked && viewport.gameObject.activeInHierarchy
            && controllerNavigation && controllerNavigation.ControllerActive && Gamepad.current != null
            && Gamepad.current.leftTrigger.isPressed && !Gamepad.current.rightTrigger.isPressed
            && applicationFocused;
    }
    public void UpdateShortcutFocus()
    {
        if (shortcutDimmer) shortcutDimmer.gameObject.SetActive(ShouldShowShortcutFocus(Application.isFocused));
    }
    private void LateUpdate()
    {
        UpdateShortcutFocus();
        if (!battle) return;
        UpdateAvailability();
        bool targeting = battle.ChosenAction != null;
        FitTooltip();
        if (battle.InspectionOpen || (hud && hud.InspectionDismissedFrame == Time.frameCount)) return;
        if (!battle.PlayerReady) return;
        if (controllerNavigation && controllerNavigation.ControllerActive) return;
        var k = Keyboard.current; var pad = Gamepad.current;
        // Match the visible main-menu End Turn control, never an ability submenu or target selection.
        if (CanUseEndTurnShortcut() && PressedEndTurn(k, pad)) { battle.EndTurn(); return; }
        if (!battle.Combatants[0].ActionAvailable)
        {
            if ((k != null && k.tabKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame))
            { moving = !moving; battle.KeyboardGridInputEnabled = moving; }
            return;
        }
        bool trigger = pad != null && pad.leftTrigger.isPressed;
        bool cancel = (k != null && k.backspaceKey.wasPressedThisFrame) || (!trigger && pad != null && pad.buttonEast.wasPressedThisFrame);
        int favorite = PressedFavorite(k, pad);
        if (browsing && favorite >= 0) { UseFavorite(favorite); return; }
        if (k != null && k.escapeKey.wasPressedThisFrame)
        { if (editingFavorites) Back(); else CancelSelection(); return; }
        if (!browsing && !targeting && ((k != null && k.tabKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame)))
        { moving = !moving; battle.KeyboardGridInputEnabled = moving; highlighted = -1; return; }
        if (targeting || moving)
        {
            if (cancel) { Back(); return; }
            if (targeting && ((k != null && k.deleteKey.wasPressedThisFrame) || (pad != null && !trigger && pad.buttonWest.wasPressedThisFrame))) battle.UndoTarget();
            return;
        }
        if (cancel && browsing) { Back(); return; }
        if (trigger) return;
        if (!input) return;
        if (!browsing && highlighted < 0)
        {
            if (input.AttackPressed) { Open(BattleActionCategory.Attack); return; }
            if (input.SkillsPressed) { Open(BattleActionCategory.Skill); return; }
            if (input.ItemsPressed) { Open(BattleActionCategory.Item); return; }
            if (input.BlockPressed) { battle.Block(); return; }
            if (input.RunPressed) { battle.Finish(false); return; }
        }
        if (input.NavUpPressed || input.NavDownPressed)
        {
            if (highlighted >= 0) pool[highlighted].SetHighlighted(false);
            highlighted = highlighted < 0 ? 0 : (highlighted + (input.NavDownPressed ? 1 : rows - 1)) % rows;
            pool[highlighted].SetHighlighted(true); ShowTooltip(highlighted);
            scroll.verticalNormalizedPosition = rows <= 1 ? 1 : 1 - highlighted / (float)(rows - 1);
        }
        else if (input.NavSelectPressed && highlighted >= 0) pool[highlighted].Invoke();
        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f && highlighted >= 0)
        { pool[highlighted].SetHighlighted(false); highlighted = -1; tooltipPanel.SetActive(false); battle.HoverAction(null); }
    }
}
