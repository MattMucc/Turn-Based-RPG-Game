using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset input;
    [SerializeField] private string outOfCombatMapName = "World";
    [SerializeField] private string battleMapName = "Battle";

    // Overworld Actions
    private InputActionMap inputMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction worldAttackAction;
    private InputAction interactAction;

    // Battle Menu Actions
    private InputActionMap battleInputMap;
    private InputAction attackAction;
    private InputAction skillsAction;
    private InputAction itemsAction;
    private InputAction runAction;
    private InputAction navUpAction;
    private InputAction navDownAction;
    private InputAction navLeftAction;
    private InputAction navRightAction;
    private InputAction navSelectAction;
    private InputAction navCancelAction;
    private bool inBattle = false;

    // Control Scheme
    public enum ControlScheme { Keyboard, Gamepad }
    public ControlScheme CurrentScheme { get; private set; } = ControlScheme.Keyboard;
    public event Action<ControlScheme> OnControlSchemeChanged;

    // Overworld Inputs
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool SprintPressed { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool CrouchPressed { get; private set; }
    public bool CrouchHeld { get; private set; }
    public bool WorldAttackPressed { get; private set; }
    public bool WorldAttackHeld { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool InteractHeld { get; private set; }

    // Battle Menu Inputs
    public bool AttackPressed { get; private set; }
    public bool SkillsPressed { get; private set; }
    public bool ItemsPressed { get; private set; }
    public bool RunPressed { get; private set; }

    public bool NavUpPressed { get; private set; }
    public bool NavDownPressed { get; private set; }
    public bool NavLeftPressed { get; private set; }
    public bool NavRightPressed { get; private set; }
    public bool NavSelectPressed { get; private set; }
    public bool NavCancelPressed { get; private set; }

    private void Awake()
    {
        if (!input)
        {
            Debug.LogError($"PlayerInputReader: No InputActionAsset assigned! Disabling script...");
            enabled = false;
            return;
        }

        inputMap = input.FindActionMap(outOfCombatMapName, true);
        if (inputMap != null)
        {
            moveAction = inputMap.FindAction("Move", true);
            lookAction = inputMap.FindAction("Look", true);
            sprintAction = inputMap.FindAction("Sprint", true);
            jumpAction = inputMap.FindAction("Jump", true);
            crouchAction = inputMap.FindAction("Crouch", true);
            worldAttackAction = inputMap.FindAction("Attack", true);
            interactAction = inputMap.FindAction("Interact", true);
        }

        battleInputMap = input.FindActionMap(battleMapName, false);
        if (battleInputMap != null)
        {
            attackAction = battleInputMap.FindAction("Attack", true);
            skillsAction = battleInputMap.FindAction("Skills", true);
            itemsAction = battleInputMap.FindAction("Items", true);
            runAction = battleInputMap.FindAction("Run", true);

            navUpAction = battleInputMap.FindAction("NavigateUp", true);
            navDownAction = battleInputMap.FindAction("NavigateDown", true);
            navLeftAction = battleInputMap.FindAction("NavigateLeft", true);
            navRightAction = battleInputMap.FindAction("NavigateRight", true);
            navSelectAction = battleInputMap.FindAction("NavigateSelect", true);
            navCancelAction = battleInputMap.FindAction("NavigateCancel", true);
        }
    }

    private void OnEnable()
    {
        if (inBattle)
            battleInputMap?.Enable();
        else
            inputMap?.Enable();

        InputSystem.onActionChange += OnActionChange;
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= OnActionChange;
        inputMap?.Disable();
        battleInputMap?.Disable();
    }

    private void Update()
    {
        // Overworld Controls
        if (!inBattle)
        {
            // Values
            Move = moveAction.ReadValue<Vector2>();
            Look = lookAction.ReadValue<Vector2>();

            // Pressed
            SprintPressed = sprintAction.WasPressedThisFrame();
            JumpPressed = jumpAction.WasPressedThisFrame();
            CrouchPressed = crouchAction.WasPressedThisFrame();
            WorldAttackPressed = worldAttackAction.WasPressedThisFrame();
            InteractPressed = interactAction.WasPressedThisFrame();

            // Held
            SprintHeld = sprintAction.IsPressed();
            JumpHeld = jumpAction.IsPressed();
            CrouchHeld = crouchAction.IsPressed();
            WorldAttackHeld = worldAttackAction.IsPressed();
            InteractHeld = interactAction.IsPressed();
        }
        else
        {
            // Shortcuts
            AttackPressed = attackAction.WasPressedThisFrame();
            SkillsPressed = skillsAction.WasPressedThisFrame();
            ItemsPressed = itemsAction.WasPressedThisFrame();
            RunPressed = runAction.WasPressedThisFrame();

            // Navigation
            NavUpPressed = navUpAction.WasPressedThisFrame();
            NavDownPressed = navDownAction.WasPressedThisFrame();
            NavLeftPressed = navLeftAction.WasPressedThisFrame();
            NavRightPressed = navRightAction.WasPressedThisFrame();
            NavSelectPressed = navSelectAction.WasPressedThisFrame();
            NavCancelPressed = navCancelAction.WasPressedThisFrame();
        }

        DetectSchemeSwitch();
    }

    public void SwitchToBattle()
    {
        if (battleInputMap == null)
        {
            Debug.LogWarning($"[PlayerInputReader] Battle Action Map not found! Set it up in the Input Actions asset first.");
            return;
        }

        inputMap?.Disable();
        battleInputMap?.Enable();
        inBattle = true;
    }

    public void SwitchToWorld()
    {
        battleInputMap?.Disable();
        inputMap?.Enable();
        inBattle = false;
    }

    private void DetectSchemeSwitch()
    {
        // Mouse moved, switch to keyboard
        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f)
        {
            TrySwitchScheme(ControlScheme.Keyboard);
            return;
        }

        // Any keyboard activity, switch to keyboard
        if (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame)
        {
            TrySwitchScheme(ControlScheme.Keyboard);
            return;
        }

        // Any gamepad activity, switch to gamepad
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            TrySwitchScheme(ControlScheme.Gamepad);
    }

    private void TrySwitchScheme(ControlScheme newScheme)
    {
        if (CurrentScheme == newScheme) return;

        CurrentScheme = newScheme;
        OnControlSchemeChanged?.Invoke(CurrentScheme);
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed) return;

        InputAction action = obj as InputAction;
        if (action == null) return;

        // Only responds to specific input maps
        if (action.actionMap != inputMap && action.actionMap != battleInputMap) return;

        InputDevice device = action.activeControl?.device;
        if (device == null) return;

        ControlScheme newScheme = device is Gamepad ? ControlScheme.Gamepad : (device is Keyboard || device is Mouse) ? ControlScheme.Keyboard : CurrentScheme;
        if (newScheme != CurrentScheme)
        {
            CurrentScheme = newScheme;
            OnControlSchemeChanged?.Invoke(CurrentScheme);
        }
    }
}