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
    private bool selected, highlighted;
    public Action<bool> HoverChanged;
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
    { if (icon) icon.sprite = scheme == PlayerInputReader.ControlScheme.Gamepad ? controllerIcon : keyboardIcon; }
    public void Configure(string text, UnityAction action, bool showIcon = false)
    {
        Ensure(); label.text = text; button.onClick.RemoveAllListeners(); button.onClick.AddListener(action);
        if (icon) icon.gameObject.SetActive(showIcon);
        selected = highlighted = false; SetAvailable(true);
    }
    public void SetAvailable(bool available)
    {
        Ensure(); button.interactable = available;
        button.image.color = !available ? new Color(0.3f, 0.3f, 0.3f, 0.8f) : highlighted || selected ? highlightedColor : normalColor;
        label.color = available ? Color.white : new Color(0.55f, 0.55f, 0.55f);
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
