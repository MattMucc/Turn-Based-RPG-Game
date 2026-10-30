using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = new Color(0.55f, 0.55f, 0.55f);
    [SerializeField] private Sprite keyboardIcon;
    [SerializeField] private Sprite controllerIcon;
    [SerializeField] private Button button;
    [SerializeField] private Outline outline;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;
    private PlayerInputReader source;
    private bool selected, highlighted, triggerShortcut;
    private string caption;
    public Action<bool> HoverChanged;
    public Sprite KeyboardIcon => keyboardIcon;
    public Sprite ControllerIcon => controllerIcon;
    public void SetIcons(Sprite keyboard, Sprite controller, bool requiresTrigger = false)
    {
        Ensure(); keyboardIcon = keyboard; controllerIcon = controller; triggerShortcut = requiresTrigger;
        UpdateIcon(source ? source.CurrentScheme : PlayerInputReader.ControlScheme.Keyboard);
    }
    public bool Available => button && button.interactable;
    public TMP_Text Label { get { Ensure(); return label; } }

    private void Awake() => Ensure();
    private void Ensure()
    {
        if (!button) button = GetComponent<Button>();
        if (!outline) outline = GetComponent<Outline>();
        if (!label) label = GetComponentInChildren<TMP_Text>(true);
        if (!icon) icon = transform.Find("Icon")?.GetComponent<Image>();
        if (!button) return;
        button.transition = Selectable.Transition.None;
        var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
    }
    private void Start()
    {
        source = GameManager.Instance ? GameManager.Instance.Input : null;
        if (source) { UpdateIcon(source.CurrentScheme); source.OnControlSchemeChanged += UpdateIcon; }
    }
    private void OnDestroy() { if (source) source.OnControlSchemeChanged -= UpdateIcon; }
    private void UpdateIcon(PlayerInputReader.ControlScheme scheme)
    {
        if (icon) icon.sprite = scheme == PlayerInputReader.ControlScheme.Gamepad ? controllerIcon : keyboardIcon;
        if (label && caption != null) label.text = (triggerShortcut && scheme == PlayerInputReader.ControlScheme.Gamepad ? "LT + " : "") + caption;
    }
    public void Configure(string text, UnityAction action, bool showIcon = false)
    {
        Ensure(); triggerShortcut = false; caption = text; label.outlineWidth = 0; label.fontSize = 14; label.enableAutoSizing = false; label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis; label.text = text; button.onClick.RemoveAllListeners(); button.onClick.AddListener(action);
        // Let the label shrink inside the row instead of retaining its previous RectTransform width.
        var row = GetComponent<HorizontalLayoutGroup>();
        if (row)
        {
            row.childControlWidth = true; row.childForceExpandWidth = false;
            var labelLayout = label.GetComponent<LayoutElement>();
            if (!labelLayout) labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.minWidth = 0; labelLayout.preferredWidth = 0; labelLayout.flexibleWidth = 1;
        }
        if (icon)
        {
            icon.rectTransform.sizeDelta = new Vector2(22, 22);
            var iconLayout = icon.GetComponent<LayoutElement>();
            if (!iconLayout) iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.minWidth = iconLayout.preferredWidth = 22; iconLayout.flexibleWidth = 0;
            icon.gameObject.SetActive(showIcon);
        }
        selected = highlighted = false; SetAvailable(true);
    }
    public void SetAvailable(bool available)
    {
        Ensure(); button.interactable = available;
        button.image.color = !available ? new Color(0.3f, 0.3f, 0.3f, 0.8f) : highlighted || selected ? highlightedColor : normalColor;
        label.color = available ? new Color(0.05f, 0.06f, 0.08f) : new Color(0.12f, 0.12f, 0.12f);
        if (outline) outline.enabled = selected || (highlighted && available);
    }
    public void SetSelected(bool value) { selected = value; SetAvailable(Available); }
    public void SetHighlighted(bool value) { highlighted = value; SetAvailable(Available); }
    public void SetIconAlpha(float alpha) { Ensure(); if (icon) { var c = icon.color; c.a = alpha; icon.color = c; } }
    public void AddClickListener(UnityAction action) { Ensure(); button.onClick.AddListener(action); }
    public void Invoke() { if (Available) button.onClick.Invoke(); }
    public void OnPointerEnter(PointerEventData eventData) { SetHighlighted(true); HoverChanged?.Invoke(true); }
    public void OnPointerExit(PointerEventData eventData) { SetHighlighted(false); HoverChanged?.Invoke(false); }
}
