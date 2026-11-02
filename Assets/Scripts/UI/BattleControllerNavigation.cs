using System.Collections.Generic;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Owns gamepad input without moving the OS mouse or creating a synthetic Mouse device.
[DefaultExecutionOrder(-10)]
public sealed class BattleControllerNavigation : MonoBehaviour
{
    public float CursorSpeed = 850;
    public bool ControllerActive { get; private set; }
    public BattleNavigationState State { get; } = new BattleNavigationState();
    public Vector2 Pointer { get; private set; }
    private InSceneGridBattle battle;
    private BattleMenuUI menu;
    private BattleHud hud;
    private GridCameraController cameraControl;
    private RectTransform cursor;
    private TMP_Text badge;
    private Canvas canvas;
    private GameObject hoveredUI;
    private readonly List<RaycastResult> hits = new();
    private Vector2Int lastDirection;
    private float repeatRemaining;
    private bool savedNavigation, savedCursor, ownsEventSystem;
    private EventSystem ownedEventSystem;
    private BattleControllerArea previousArea = BattleControllerArea.Commands;
    private bool wasInspecting;
    private BattleActionOption lastAction;

    public void Bind(InSceneGridBattle source, BattleMenuUI commands, BattleHud display)
    {
        Unbind(); battle = source; menu = commands; hud = display;
        cameraControl = source.BattleCamera.GetComponent<GridCameraController>();
        canvas = GetComponentInParent<Canvas>();
        Pointer = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (!cursor)
        {
            var obj = new GameObject("Controller cursor", typeof(RectTransform));
            obj.transform.SetParent(transform, false); cursor = (RectTransform)obj.transform;
            cursor.sizeDelta = new Vector2(22, 22);
            var text = obj.AddComponent<TextMeshProUGUI>(); text.text = "+"; text.fontSize = 30;
            text.alignment = TextAlignmentOptions.Center; text.color = Color.white;
            text.outlineColor = Color.black; text.outlineWidth = 0.25f; text.raycastTarget = false;
            var hint = new GameObject("Controller navigation mode", typeof(RectTransform));
            hint.transform.SetParent(transform, false);
            var rect = (RectTransform)hint.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(12, -12); rect.sizeDelta = new Vector2(280, 150);
            badge = hint.AddComponent<TextMeshProUGUI>(); badge.fontSize = 13; badge.raycastTarget = false;
            badge.alignment = TextAlignmentOptions.TopLeft; badge.outlineColor = Color.black; badge.outlineWidth = 0.2f;
        }
        cursor.SetAsLastSibling(); SetActiveController(false);
    }
    public void Unbind()
    {
        SetActiveController(false); ClearHover();
        battle = null; menu = null; hud = null; cameraControl = null;
        if (cursor) cursor.gameObject.SetActive(false);
        if (badge) badge.gameObject.SetActive(false);
    }
    private void SetActiveController(bool active)
    {
        if (active && !ControllerActive)
        {
            savedCursor = Cursor.visible;
            if (Application.isPlaying) Cursor.visible = false;
            ownedEventSystem = EventSystem.current;
            if (ownedEventSystem)
            {
                savedNavigation = ownedEventSystem.sendNavigationEvents;
                ownedEventSystem.sendNavigationEvents = false; ownsEventSystem = true;
            }
        }
        else if (!active && ControllerActive)
        {
            if (Application.isPlaying) Cursor.visible = savedCursor;
            if (ownsEventSystem && ownedEventSystem) ownedEventSystem.sendNavigationEvents = savedNavigation;
            ownsEventSystem = false; ownedEventSystem = null;
            ClearHover();
            if (hud) hud.ClearControllerPortrait();
            if (menu) menu.ClearControllerCommands();
        }
        ControllerActive = active;
        if (battle) battle.ExternalControllerActive = active;
        if (cameraControl) cameraControl.ControllerOwnsInput = active;
        if (cursor) cursor.gameObject.SetActive(active && State.Mode == BattleControllerMode.Cursor);
        if (badge) badge.gameObject.SetActive(active);
    }
    private static bool MouseActivity()
    {
        var mouse = Mouse.current;
        return mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 0.5f || mouse.leftButton.wasPressedThisFrame
            || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame || mouse.scroll.ReadValue() != Vector2.zero);
    }
    private static bool PadActivity(Gamepad pad, Vector2 left, Vector2 right)
        => left != Vector2.zero || right != Vector2.zero || pad.dpad.ReadValue() != Vector2.zero
        || pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame
        || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame
        || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame
        || pad.leftTrigger.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame
        || pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame
        || pad.selectButton.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;

    private Vector2Int Direction(Gamepad pad, float dt)
    {
        Vector2 raw = pad.dpad.ReadValue();
        var direction = new Vector2Int(Mathf.RoundToInt(raw.x), Mathf.RoundToInt(raw.y));
        if (direction == Vector2Int.zero) { lastDirection = direction; repeatRemaining = 0; return direction; }
        if (direction != lastDirection) { lastDirection = direction; repeatRemaining = 0.3f; return direction; }
        repeatRemaining -= dt;
        if (repeatRemaining > 0) return Vector2Int.zero;
        repeatRemaining = 0.12f; return direction;
    }
    private void Navigate(BattleControllerArea area)
    {
        ClearHover(); menu.ClearControllerCommands(); hud.ClearControllerPortrait();
        State.Navigate(area);
        if (area == BattleControllerArea.Commands) menu.ControllerCommands(0, false);
        if (area == BattleControllerArea.TurnOrder) hud.ControllerPortraitStep(0, false);
        if (area == BattleControllerArea.Battlefield) battle.ControllerGridStep(Vector2Int.zero);
    }
    private void Update()
    {
        if (!battle || !Application.isFocused) return;
        ProcessController(Gamepad.current, Time.unscaledDeltaTime);
    }
    public void ProcessController(Gamepad pad, float deltaTime)
    {
        if (!battle) return;
        if (pad == null) { SetActiveController(false); return; }
        var left = BattleNavigationState.FilterStick(pad.leftStick.ReadValue());
        var right = BattleNavigationState.FilterStick(pad.rightStick.ReadValue());
        if (MouseActivity() || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame))
        { SetActiveController(false); return; }
        if (PadActivity(pad, left, right)) SetActiveController(true);
        if (!ControllerActive) return;
        float dt = Mathf.Clamp(deltaTime, 0, 0.1f);
        if (battle.ControllerInputLocked)
        {
            ClearHover(); cursor.gameObject.SetActive(false); badge.gameObject.SetActive(false);
            lastDirection = Vector2Int.zero; return;
        }
        badge.gameObject.SetActive(true);
        if (pad.selectButton.wasPressedThisFrame && !battle.InspectionOpen)
        {
            ClearHover(); menu.ClearControllerCommands(); hud.ClearControllerPortrait();
            State.ToggleFlight(); lastDirection = Vector2Int.zero;
        }
        if (battle.InspectionOpen && !wasInspecting) { previousArea = State.Area; wasInspecting = true; }
        if (!battle.InspectionOpen && wasInspecting)
        { wasInspecting = false; if (State.Mode == BattleControllerMode.Navigation) Navigate(previousArea); }
        if (pad.buttonEast.wasPressedThisFrame && !pad.leftTrigger.isPressed)
        {
            ClearHover();
            if (battle.InspectionOpen) hud.CloseInspection();
            else { menu.ControllerBack(); if (State.Mode == BattleControllerMode.Navigation) Navigate(BattleControllerArea.Commands); }
            DrawMode(); return;
        }
        if (pad.leftStickButton.wasPressedThisFrame && !battle.InspectionOpen)
        { battle.FocusCharacter(battle.CurrentActorIndex); DrawMode(); return; }

        bool cameraModifier = pad.rightTrigger.isPressed && !battle.InspectionOpen;
        if (cameraModifier)
        {
            ClearHover(); battle.TargetFocus.Hover(null);
            cameraControl.ControllerCamera(left, right, State.Mode == BattleControllerMode.FreeFlight, true, dt);
            DrawMode(); return; // Camera modifier never also clicks, changes panels, or spends an action.
        }
        Vector2Int direction = Direction(pad, dt);
        if (direction != Vector2Int.zero)
        {
            if (battle.InspectionOpen) Navigate(BattleControllerArea.Examine);
            else if (pad.leftTrigger.isPressed)
            {
                Navigate(direction.y > 0 ? BattleControllerArea.TurnOrder : direction.x > 0
                    ? BattleControllerArea.Battlefield : BattleControllerArea.Commands);
                DrawMode(); return;
            }
            else if (State.Mode != BattleControllerMode.Navigation)
                Navigate(battle.ChosenAction != null ? BattleControllerArea.Battlefield : BattleControllerArea.Commands);
        }
        if (State.Mode == BattleControllerMode.Navigation && battle.ChosenAction != null && battle.ChosenAction != lastAction)
            Navigate(BattleControllerArea.Battlefield);
        lastAction = battle.ChosenAction;
        if (left != Vector2.zero && direction == Vector2Int.zero && !cameraModifier && State.Mode == BattleControllerMode.Navigation)
        { ClearHover(); menu.ClearControllerCommands(); hud.ClearControllerPortrait(); State.UseCursor(); }

        bool confirm = pad.buttonSouth.wasPressedThisFrame;
        if (pad.leftTrigger.isPressed && !battle.InspectionOpen)
        {
            int slot = confirm ? 0 : pad.buttonEast.wasPressedThisFrame ? 1
                : pad.buttonWest.wasPressedThisFrame ? 2 : pad.buttonNorth.wasPressedThisFrame ? 3 : -1;
            if (menu.ControllerBrowsing)
            {
                if (slot >= 0) { menu.UseFavorite(slot); DrawMode(); return; }
            }
            else
            {
                int command = pad.buttonWest.wasPressedThisFrame ? 0 : pad.buttonNorth.wasPressedThisFrame ? 1
                    : confirm ? 2 : pad.rightShoulder.wasPressedThisFrame ? 3 : pad.buttonEast.wasPressedThisFrame ? 4 : -1;
                if (command >= 0) { menu.ControllerRootShortcut(command); DrawMode(); return; }
            }
        }
        if (pad.rightStickButton.wasPressedThisFrame && !cameraModifier && !pad.leftTrigger.isPressed
            && !battle.InspectionOpen && (State.Mode != BattleControllerMode.Navigation || State.Area == BattleControllerArea.Commands))
        { menu.ControllerEndTurn(); DrawMode(); return; }

        if (State.Mode == BattleControllerMode.FreeFlight && !battle.InspectionOpen)
        {
            ClearHover(); battle.TargetFocus.Hover(null);
            cameraControl.ControllerCamera(left, right, true, cameraModifier, dt);
        }
        else if (State.Mode == BattleControllerMode.Cursor)
        {
            if (!cameraModifier) Pointer += left * CursorSpeed * dt;
            Pointer = new Vector2(Mathf.Clamp(Pointer.x, 0, Screen.width - 1), Mathf.Clamp(Pointer.y, 0, Screen.height - 1));
            var over = RaycastUI(Pointer);
            Hover(over);
            bool scrolling = !cameraModifier && ScrollHovered(over, right, dt);
            if (!battle.InspectionOpen && !scrolling)
                cameraControl.ControllerCamera(cameraModifier ? left : Vector2.zero, right, false, cameraModifier, dt);
            if (!cameraModifier)
            {
                if (over && confirm && !pad.leftTrigger.isPressed) ClickUI(over);
                else battle.ControllerPoint(Pointer, over != null, confirm && !pad.leftTrigger.isPressed);
            }
        }
        else
        {
            ClearHover();
            if (battle.InspectionOpen)
            {
                if (confirm) hud.ControllerInspectSubmit();
                hud.ControllerInspectScroll(right.y != 0 ? right.y : pad.dpad.ReadValue().y, dt);
            }
            else
            {
                int step = direction.y != 0 ? -direction.y : direction.x;
                if (State.Area == BattleControllerArea.Commands) menu.ControllerCommands(step, confirm);
                else if (State.Area == BattleControllerArea.TurnOrder) hud.ControllerPortraitStep(step, confirm);
                else if (State.Area == BattleControllerArea.Battlefield && (direction != Vector2Int.zero || confirm)) battle.ControllerGridStep(direction, confirm);
                if (pad.buttonWest.wasPressedThisFrame && battle.ChosenAction != null && !pad.leftTrigger.isPressed) battle.UndoTarget();
                cameraControl.ControllerCamera(cameraModifier ? left : Vector2.zero, right, false, cameraModifier, dt);
            }
        }
        DrawMode();
    }
    private void DrawMode()
    {
        if (!cursor || !badge) return;
        cursor.gameObject.SetActive(ControllerActive && State.Mode == BattleControllerMode.Cursor && !battle.ControllerInputLocked);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, Pointer,
            canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null, out var position);
        cursor.localPosition = position;
        string area = battle.InspectionOpen ? "Examine" : State.Area == BattleControllerArea.TurnOrder ? "Turn order" : State.Area.ToString();
        badge.text = State.Mode == BattleControllerMode.Cursor ? "Cursor  •  View: free flight\nLT + D-pad: panels  •  L3: focus"
            : State.Mode == BattleControllerMode.FreeFlight ? "Free flight  •  View: cursor\nRT + right stick: height  •  L3: focus"
            : area + "  •  LT + D-pad: panels\nLeft stick: cursor  •  View: free flight";
        badge.text = ControllerGlyphs.Adapt(badge.text);
    }
    private GameObject RaycastUI(Vector2 position)
    {
        if (!EventSystem.current) return null;
        hits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
        foreach (var hit in hits) if (hit.module is GraphicRaycaster) return hit.gameObject;
        return null;
    }
    private PointerEventData PointerData() => new PointerEventData(EventSystem.current)
    { position = Pointer, pointerId = -30, button = PointerEventData.InputButton.Left };
    private void Hover(GameObject target)
    {
        if (target == hoveredUI) return;
        ClearHover(); hoveredUI = target;
        if (hoveredUI && EventSystem.current) ExecuteEvents.ExecuteHierarchy(hoveredUI, PointerData(), ExecuteEvents.pointerEnterHandler);
    }
    private void ClearHover()
    {
        if (hoveredUI && EventSystem.current) ExecuteEvents.ExecuteHierarchy(hoveredUI, PointerData(), ExecuteEvents.pointerExitHandler);
        hoveredUI = null;
    }
    private void ClickUI(GameObject target)
    {
        if (!EventSystem.current) return;
        ExecuteEvents.ExecuteHierarchy(target, PointerData(), ExecuteEvents.pointerClickHandler);
    }
    public const float ScrollSpeed = 3f;
    public static bool ScrollHovered(GameObject target, Vector2 stick, float seconds)
    {
        var scroll = target ? target.GetComponentInParent<ScrollRect>() : null;
        if (!scroll || !scroll.isActiveAndEnabled) return false;
        // Consume the stick over the whole scroll window, even at its endpoint.
        if (stick != Vector2.zero) scroll.velocity = Vector2.zero;
        if (scroll.vertical) scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + stick.y * seconds * ScrollSpeed);
        if (scroll.horizontal) scroll.horizontalNormalizedPosition = Mathf.Clamp01(scroll.horizontalNormalizedPosition + stick.x * seconds * ScrollSpeed);
        return true;
    }
    private void OnApplicationFocus(bool focused)
    {
        if (!focused) { ClearHover(); if (cursor) cursor.gameObject.SetActive(false); lastDirection = Vector2Int.zero; }
    }
    private void OnDisable() { SetActiveController(false); }
    private void OnDestroy() { Unbind(); }
}
