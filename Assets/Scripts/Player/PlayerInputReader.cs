using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset input;
    [SerializeField] private string outOfCombatMapName = "World";
    [SerializeField] private string battleMapName = "Battle";

    private InputActionMap inputMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction worldAttackAction;
    private InputAction interactAction;

    // Control Scheme
    public enum ControlScheme { Keyboard, Gamepad }
    public ControlScheme CurrentScheme { get; private set; } = ControlScheme.Keyboard;
    public event Action<ControlScheme> OnControlSchemeChanged;

    // Input Actions
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

    private void Awake()
    {
        if (!input)
        {
            Debug.LogError($"PlayerInputReader: No InputActionAsset assigned! Disabling script...");
            enabled = false;
            return;
        }

        inputMap = input.FindActionMap(outOfCombatMapName, true);
        moveAction = inputMap.FindAction("Move", true);
        lookAction = inputMap.FindAction("Look", true);
        sprintAction = inputMap.FindAction("Sprint", true);
        jumpAction = inputMap.FindAction("Jump", true);
        crouchAction = inputMap.FindAction("Crouch", true);
        worldAttackAction = inputMap.FindAction("Attack", true);
        interactAction = inputMap.FindAction("Interact", true);
    }

    private void OnEnable()
    {
        inputMap?.Enable();
        InputSystem.onActionChange += OnActionChange;
    }

    private void OnDisable()
    {
        InputSystem.onActionChange -= OnActionChange;
        inputMap?.Disable();
    }

    private void Update()
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

    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed) return;

        InputAction action = obj as InputAction;
        if (action == null) return;

        // Only responds to GAMEPLAY actions, not UI actions
        if (action.actionMap != inputMap) return;

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