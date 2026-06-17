using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Button Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = new Color(0.55f, 0.55f, 0.55f);

    [Header("Icons")]
    [SerializeField] private Sprite keyboardIcon;
    [SerializeField] private Sprite controllerIcon;

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Outline outline;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;

    private void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (button)
        {
            if (!outline) outline = GetComponent<Outline>();
            if (!label) label = button.transform.Find("Label").GetComponent<TMP_Text>();
            if (!icon) icon = button.transform.Find("Icon").GetComponent<Image>();

            button.image.color = normalColor;
            button.transition = Selectable.Transition.None;
        }

        // Disabling navigation means disabling moving focus to this button via tab, D-Pad, gamepad stick, or arrows.
        // This does not affect the mouse being able to click on the button.
        // I have created my own UI navigation, so don't worry about disabling this.
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;

        outline.enabled = false;
    }

    private void Start()
    {
        UpdateIcon(GameManager.Instance.Input.CurrentScheme);
        GameManager.Instance.Input.OnControlSchemeChanged += UpdateIcon;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance.Input)
            GameManager.Instance.Input.OnControlSchemeChanged -= UpdateIcon;
    }

    private void UpdateIcon(PlayerInputReader.ControlScheme scheme)
    {
        icon.sprite = scheme == PlayerInputReader.ControlScheme.Gamepad ? controllerIcon : keyboardIcon;
    }

    public void SetHighlighted(bool highlighted)
    {
        button.image.color = highlighted ? highlightedColor : normalColor;
        outline.enabled = highlighted;
    }

    public void SetIconAlpha(float alpha)
    {
        Color color = icon.color;
        color.a = alpha;
        icon.color = color;
    }

    /// <summary>
    /// BattleMenuUi subscribes to this to know when a button is clicked with the mouse.
    /// </summary>
    public void AddClickListener(UnityAction action)
    {
        button.onClick.AddListener(action);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlighted(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlighted(false);
    }
}