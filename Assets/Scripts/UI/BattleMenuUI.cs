using UnityEngine;
using UnityEngine.InputSystem;

public class BattleMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private BattleMenuButton attackButton;
    [SerializeField] private BattleMenuButton skillsButton;
    [SerializeField] private BattleMenuButton itemsButton;
    [SerializeField] private BattleMenuButton runButton;

    private PlayerInputReader input;
    private BattleMenuButton[] buttons;

    private enum InputMode { Shortcut, Navigation }
    private InputMode currentMode = InputMode.Shortcut;
    private int highlightedIndex = 0;

    private void Start()
    {
        if (!input) input = GameManager.Instance.Input;

        buttons = new BattleMenuButton[] { attackButton, skillsButton, itemsButton, runButton };

        attackButton.AddClickListener(OnAttackPressed);
        skillsButton.AddClickListener(OnSkillsPressed);
        itemsButton.AddClickListener(OnItemsPressed);
        runButton.AddClickListener(OnRunPressed);

        // Hidden until BattleManager calls Show()
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (currentMode == InputMode.Shortcut)
        {
            if (input.NavUpPressed || input.NavDownPressed)
            {
                EnterNavigationMode();
                return;
            }
        }

        if (currentMode == InputMode.Shortcut)
            ShortcutInputs();
        else
            NavigationInputs();
    }

    private void EnterNavigationMode()
    {
        currentMode = InputMode.Navigation;
        highlightedIndex = 0;
        UpdateHighlight();
        SetAllIconAlpha(0.5f);
    }

    private void ExitNavigationMode()
    {
        currentMode = InputMode.Shortcut;
        buttons[highlightedIndex].SetHighlighted(false);
        SetAllIconAlpha(1f);
    }

    private void ShortcutInputs()
    {
        if (input.AttackPressed) OnAttackPressed();
        if (input.SkillsPressed) OnSkillsPressed();
        if (input.ItemsPressed) OnItemsPressed();
        if (input.RunPressed) OnRunPressed();
    }

    private void NavigationInputs()
    {
        // Move highlight up
        if (input.NavUpPressed)
        {
            buttons[highlightedIndex].SetHighlighted(false);
            highlightedIndex = (highlightedIndex - 1 + buttons.Length) % buttons.Length;
            UpdateHighlight();
        }

        // Move highlight down
        if (input.NavDownPressed)
        {
            buttons[highlightedIndex].SetHighlighted(false);
            highlightedIndex = (highlightedIndex + 1 + buttons.Length) % buttons.Length;
            UpdateHighlight();
        }

        // Select highlighted button
        if (input.NavSelectPressed)
            SelectHighlighted();

        // Exit navigation mode (controller)
        if (input.NavCancelPressed)
            ExitNavigationMode();

        // Exit navigation mode (mouse)
        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f)
            ExitNavigationMode();
    }

    private void SelectHighlighted()
    {
        switch (highlightedIndex)
        {
            case 0:
                OnAttackPressed();
                break;
            case 1:
                OnSkillsPressed();
                break;
            case 2:
                OnItemsPressed();
                break;
            case 3:
                OnRunPressed();
                break;
        }
    }

    private void UpdateHighlight()
    {
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].SetHighlighted(i == highlightedIndex);
    }

    private void SetAllIconAlpha(float alpha)
    {
        foreach (BattleMenuButton button in buttons)
            button.SetIconAlpha(alpha);
    }

    private void OnAttackPressed()
    {
        // Perform attack
        Debug.Log("Pressed Attack button!");
    }

    private void OnSkillsPressed()
    {
        // Open SkillsMenu
        // Close BattleMenu
    }

    private void OnItemsPressed()
    {
        // Open ItemsMenu
        // Close BattleMenu
    }

    private void OnRunPressed()
    {
        // End battle
        // Hide all menus
        // Give enemy movement back
        // Give player movement back
        // Give player rotation back
    }

    public void Show()
    {
        gameObject.SetActive(true);
        currentMode = InputMode.Shortcut;
        SetAllIconAlpha(1f);
    }

    public void Hide() => gameObject.SetActive(false);
}