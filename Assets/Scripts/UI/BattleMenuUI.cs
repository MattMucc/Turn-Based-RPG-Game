using System;
using System.Collections.Generic;
using TMPro;
using Tenebrasia.GridPrototype;
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
    private Sprite runControllerIcon;
    private readonly List<BattleMenuButton> pool = new();
    private readonly List<Func<bool>> availability = new();
    private readonly List<string> descriptions = new();
    private readonly List<BattleActionOption> previewOptions = new();
    private BattleHud hud;
    private readonly List<RaycastResult> raycasts = new();
    private InSceneGridBattle battle;
    private PlayerInputReader input;
    private RectTransform content, viewport;
    private ScrollRect scroll;
    private TMP_Text tooltip, targetCounter;
    private ScrollRect tooltipScroll;
    private GameObject tooltipPanel;
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
        Show(); Root(); hud.Bind(battle, attackButton.Label.font);
    }
    public void Unbind()
    {
        if (battle) { battle.PointerOverUI = null; battle.ActionResolved -= Root; }
        battle = null; Hide();
    }
    public void Show() { Ensure(); gameObject.SetActive(true); }
    public void Hide() => gameObject.SetActive(false);
    private bool IsOverUI(Vector2 point)
    {
        if (!isActiveAndEnabled) return false;
        if (RectTransformUtility.RectangleContainsScreenPoint(viewport, point, GetComponentInParent<Canvas>().worldCamera)) return true;
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
        browsing = moving = false; battle.GridInputEnabled = true; battle.KeyboardGridInputEnabled = false;
        BeginList();
        Row("Attack", () => Open(BattleActionCategory.Attack), "Choose an attack available to this character. Movement is free on green tiles. Tab / controller LB switches grid navigation on or off.", () => battle.ActionReady, true);
        Row("Skills", () => Open(BattleActionCategory.Skill), "Choose a skill available to this character.", () => battle.ActionReady, true);
        Row("Items", () => Open(BattleActionCategory.Item), "Use a carried item. Costs one action and one item.", () => battle.ActionReady, true);
        Row("Block", () => battle.Block(), "Spend your action to reduce incoming prototype damage until your next turn. You may still move. Keyboard 4 / controller RB.", () => battle.ActionReady, true);
        pool[3].SetIcons(blockKeyboardIcon, blockControllerIcon);
        Row("Run", () => battle.Finish(false), "Leave this prototype encounter and restore overworld positions. Keyboard 5 / controller B.", () => battle.PlayerReady, true);
        pool[4].SetIcons(runKeyboardIcon, runControllerIcon);
        Row("End Turn", () => battle.EndTurn(), "End this turn. Unspent movement carries over; your next turn grants three more points.", () => battle.PlayerReady);
        FinishList();
    }
    private void Open(BattleActionCategory selectedCategory)
    {
        if (!battle.ActionReady) return;
        category = selectedCategory; browsing = true; moving = false; battle.GridInputEnabled = false; battle.KeyboardGridInputEnabled = false;
        BeginList();
        foreach (var option in battle.Loadout.Options)
        {
            if (option == null || option.Category != category || !option.Available) continue;
            var captured = option;
            Row(option.Name + (category == BattleActionCategory.Item ? $" ×{option.Quantity}" : ""), () => Select(captured), option.Tooltip,
                () => battle.ActionReady && captured.Available && (battle.ChosenAction == null || battle.ChosenAction == captured), preview: captured);
        }
        if (rows == 0) Row("None available", () => { }, "This character has no available options in this category.", () => false);
        if (battle.ChosenAction?.Pattern == BattleTargetPattern.Multiple)
        {
            Row("Undo Hit", () => battle.UndoTarget(), "Remove the most recently assigned hit. Delete / controller X while targeting.",
                () => battle.ActionReady && battle.TargetCount > 0);
            Row("Clear Targets", () => battle.ClearTargets(), "Remove all assigned hits without spending your action.",
                () => battle.ActionReady && battle.TargetCount > 0);
        }
        Row("Back", Back, "Cancel the selection and return to the command menu. Backspace / controller B. Escape cancels the action while keeping this list open.", () => battle.PlayerReady);
        FinishList();
    }
    private void Select(BattleActionOption option)
    {
        if (!battle.ChooseAction(option)) return;
        Open(category);
        battle.GridInputEnabled = battle.KeyboardGridInputEnabled = true;
        highlighted = -1; tooltip.text = option.Tooltip; tooltipPanel.SetActive(true);
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
        battle.CancelAction(); Root();
    }
    private void ShowTooltip(int index)
    { if (index >= 0 && index < descriptions.Count) { tooltip.text = descriptions[index]; tooltipPanel.SetActive(true); if (battle) battle.HoverAction(previewOptions[index]); } }
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
        bool showCount = battle.ChosenAction?.Pattern == BattleTargetPattern.Multiple;
        targetCounter.transform.parent.gameObject.SetActive(showCount);
        if (showCount)
        {
            targetCounter.text = $"{battle.TargetCount}/{battle.TargetLimit} targets selected";
            ((RectTransform)targetCounter.transform.parent).anchoredPosition = new Vector2(12, 16 + viewport.sizeDelta.y);
        }
    }
    private void LateUpdate()
    {
        if (!battle) return;
        UpdateAvailability();
        bool targeting = battle.ChosenAction != null;
        FitTooltip();
        if (battle.InspectionOpen || (hud && hud.InspectionDismissedFrame == Time.frameCount)) return;
        if (!battle.PlayerReady) return;
        var k = Keyboard.current; var pad = Gamepad.current;
        bool cancel = input && input.NavCancelPressed;
        if (k != null && k.escapeKey.wasPressedThisFrame)
        { CancelSelection(); return; }
        if (!browsing && !targeting && ((k != null && k.tabKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame)))
        { moving = !moving; battle.KeyboardGridInputEnabled = moving; highlighted = -1; return; }
        if (targeting || moving)
        {
            if (cancel) { Back(); return; }
            if (targeting && ((k != null && k.deleteKey.wasPressedThisFrame) || (pad != null && pad.buttonWest.wasPressedThisFrame))) battle.UndoTarget();
            return;
        }
        if (!input) return;
        if (cancel && browsing) { Back(); return; }
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
