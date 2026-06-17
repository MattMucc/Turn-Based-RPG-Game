using UnityEngine;

public class BattleMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private BattleMenuButton attackButton;
    [SerializeField] private BattleMenuButton skillsButton;
    [SerializeField] private BattleMenuButton itemsButton;
    [SerializeField] private BattleMenuButton runButton;

    private PlayerInputReader input;

    private void Start()
    {
        if (!input) input = GameManager.Instance.Input;

        attackButton.AddClickListener(OnAttackPressed);
        skillsButton.AddClickListener(OnSkillsPressed);
        itemsButton.AddClickListener(OnItemsPressed);
        runButton.AddClickListener(OnRunPressed);

        // Hidden until BattleManager calls Show()
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (input.AttackPressed) OnAttackPressed();
        if (input.SkillsPressed) OnSkillsPressed();
        if (input.ItemsPressed) OnItemsPressed();
        if (input.RunPressed) OnRunPressed();
    }

    private void OnAttackPressed()
    {
        // Perform attack
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

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}