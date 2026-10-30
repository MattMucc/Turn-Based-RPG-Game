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
    private readonly List<BattleMenuButton> pool = new();
    private readonly List<Func<bool>> availability = new();
    private readonly List<string> descriptions = new();
    private readonly List<RaycastResult> raycasts = new();
    private InSceneGridBattle battle;
    private PlayerInputReader input;
    private RectTransform content, viewport;
    private ScrollRect scroll;
    private TMP_Text tooltip, status;
    private GameObject tooltipPanel;
    private int rows, highlighted = -1;
    private bool browsing, moving, initialized;
    private BattleActionCategory category;

    private void Start() { if (!battle) Hide(); }
    private void Ensure()
    {
        if (initialized) return;
        initialized = true;
        pool.AddRange(new[] { attackButton, skillsButton, itemsButton, runButton });
        content = (RectTransform)attackButton.transform.parent;
        // A compact, scrollable list at the bottom left; use the original buttons and their layout.
        var holder = new GameObject("Battle command viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport = (RectTransform)holder.transform; viewport.SetParent(transform, false);
        viewport.anchorMin = viewport.anchorMax = new Vector2(0, 0);
        viewport.pivot = Vector2.zero; viewport.anchoredPosition = new Vector2(24, 24); viewport.sizeDelta = new Vector2(220, 300);
        holder.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.65f);
        content.SetParent(viewport, false); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true; layout.childForceExpandHeight = false; layout.spacing = 4;
        layout.padding = new RectOffset(6, 6, 6, 6);
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll = holder.GetComponent<ScrollRect>(); scroll.content = content; scroll.viewport = viewport;
        scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 25; scroll.movementType = ScrollRect.MovementType.Clamped;
        tooltip = MakeText("Action details", new Vector2(0, 0), new Vector2(260, 24), new Vector2(350, 340), true);
        tooltipPanel = tooltip.transform.parent.gameObject; tooltipPanel.SetActive(false);
        status = MakeText("Battle targeting", new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(650, 90), true);
        status.alignment = TextAlignmentOptions.Center;
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
        var text = textObject.GetComponent<TextMeshProUGUI>(); text.font = attackButton.Label.font; text.fontSize = 17;
        text.color = Color.white; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    public void Bind(InSceneGridBattle encounter)
    {
        Ensure(); Unbind(); battle = encounter; input = GameManager.Instance ? GameManager.Instance.Input : null;
        battle.PointerOverUI = IsOverUI; battle.ActionResolved += Root;
        Show(); Root();
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
        rows = 0; highlighted = -1; availability.Clear(); descriptions.Clear(); tooltipPanel.SetActive(false);
        foreach (var button in pool) button.gameObject.SetActive(false);
    }
    private void Row(string label, Action action, string description, Func<bool> available, bool icon = false)
    {
        int index = rows++;
        if (index == pool.Count) pool.Add(Instantiate(attackButton, content));
        var button = pool[index]; button.gameObject.SetActive(true); button.transform.SetSiblingIndex(index);
        var layout = button.GetComponent<LayoutElement>(); if (!layout) layout = button.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = layout.preferredHeight = 36;
        button.Configure(label, () => action(), icon);
        button.HoverChanged = entered => { if (entered) ShowTooltip(index); else tooltipPanel.SetActive(false); };
        availability.Add(available); descriptions.Add(description);
    }
    private void FinishList()
    {
        viewport.sizeDelta = new Vector2(220, Mathf.Min(340, rows * 40 + 8));
        scroll.verticalNormalizedPosition = 1;
        UpdateAvailability();
    }
    private void Root()
    {
        if (!battle) return;
        browsing = moving = false; battle.GridInputEnabled = true; battle.KeyboardGridInputEnabled = false;
        BeginList();
        Row("Attack", () => Open(BattleActionCategory.Attack), "Choose an attack available to this character.", () => battle.ActionReady, true);
        Row("Skills", () => Open(BattleActionCategory.Skill), "Choose a skill available to this character.", () => battle.ActionReady, true);
        Row("Items", () => Open(BattleActionCategory.Item), "Use a carried item. Costs one action and one item.", () => battle.ActionReady, true);
        Row("Run", () => battle.Finish(false), "Leave this prototype encounter and restore overworld positions.", () => battle.PlayerReady, true);
        Row("Move", () => { moving = true; battle.KeyboardGridInputEnabled = true; }, "Movement uses movement points, never your action. Select a tile, then confirm. Back returns to commands.", () => battle.PlayerReady);
        Row("Block", () => battle.Block(), "Spend your action to reduce incoming prototype damage until your next turn. You may still move.", () => battle.ActionReady);
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
                () => battle.ActionReady && captured.Available && (battle.ChosenAction == null || battle.ChosenAction == captured));
        }
        if (rows == 0) Row("None available", () => { }, "This character has no available options in this category.", () => false);
        Row("Confirm", () => battle.CommitAction(), "Commit the selected action to the selected target(s). Space / controller Y also confirms while targeting.",
            () => battle.ActionReady && battle.ChosenAction != null && battle.TargetCount > 0);
        Row("Back", Back, "Cancel target selection without spending an action or item. Backspace / controller B.", () => battle.PlayerReady);
        FinishList();
    }
    private void Select(BattleActionOption option)
    {
        if (!battle.ChooseAction(option)) return;
        battle.GridInputEnabled = battle.KeyboardGridInputEnabled = true;
        highlighted = -1; tooltip.text = option.Tooltip; tooltipPanel.SetActive(true);
        for (int i = 0; i < rows; i++) pool[i].SetSelected(descriptions[i] == option.Tooltip);
        UpdateAvailability();
    }
    private void Back()
    {
        if (battle.ChosenAction != null) { battle.CancelAction(); Open(category); }
        else Root();
    }
    private void ShowTooltip(int index)
    { if (index >= 0 && index < descriptions.Count) { tooltip.text = descriptions[index]; tooltipPanel.SetActive(true); } }
    private void UpdateAvailability()
    { for (int i = 0; i < rows; i++) pool[i].SetAvailable(availability[i]()); }
    private void LateUpdate()
    {
        if (!battle) return;
        UpdateAvailability();
        bool targeting = battle.ChosenAction != null;
        status.text = targeting ? $"{battle.ChosenAction.Name} · {battle.TargetCount}/{battle.TargetLimit} enemies selected\n{battle.Message}"
            : battle.PlayerReady ? $"{battle.Movement} movement · {(battle.ActionReady ? "Action ready" : "Action spent")}\n{(moving ? "Select a tile, then confirm. Back returns to commands." : battle.Message)}" : "Enemy turn";
        if (!battle.PlayerReady) return;
        var k = Keyboard.current; var pad = Gamepad.current;
        bool cancel = (input && input.NavCancelPressed) || (k != null && k.escapeKey.wasPressedThisFrame);
        if (targeting || moving)
        {
            if (cancel) { Back(); return; }
            if (targeting && ((k != null && k.spaceKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame))) battle.CommitAction();
            return;
        }
        if (!input) return;
        if (cancel && browsing) { Back(); return; }
        if (!browsing && highlighted < 0)
        {
            if (input.AttackPressed) { Open(BattleActionCategory.Attack); return; }
            if (input.SkillsPressed) { Open(BattleActionCategory.Skill); return; }
            if (input.ItemsPressed) { Open(BattleActionCategory.Item); return; }
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
        { pool[highlighted].SetHighlighted(false); highlighted = -1; tooltipPanel.SetActive(false); }
    }
}
