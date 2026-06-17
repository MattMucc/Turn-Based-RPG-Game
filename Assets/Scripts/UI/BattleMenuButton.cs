using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BattleMenuButton : MonoBehaviour
{
    [Header("Icons")]
    [SerializeField] private Sprite keyboardIcon;
    [SerializeField] private Sprite controllerIcon;

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;

    private void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (button)
        {
            if (!label) label = button.transform.Find("Label").GetComponent<TMP_Text>();
            if (!icon) icon = button.transform.Find("Icon").GetComponent<Image>();
        }

        // Disabling navigation means disabling moving focus to this button via tab, D-Pad, gamepad stick, or arrows.
        // This does not affect the mouse being able to click on the button.
        Navigation nav = button.navigation;
        nav.mode = Navigation.Mode.None;
        button.navigation = nav;
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

    /// <summary>
    /// BattleMenuUi subscribes to this to know when a button is clicked with the mouse.
    /// </summary>
    public void AddClickListener(UnityAction action)
    {
        button.onClick.AddListener(action);
    }
}