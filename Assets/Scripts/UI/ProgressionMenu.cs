using System;
using System.Collections.Generic;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Player menu: replaces the original allocation popup while retaining its public entry point.
[DefaultExecutionOrder(-50)]
public sealed partial class ProgressionMenu : MonoBehaviour
{
    public enum Page { Party, Attributes, Shortcuts, Progression, SkillTree, Inventory, Settings }
    GameObject canvasObject, ownedEvents;
    RectTransform shell, body, detailRoot;
    TMP_Text title, characterLabel, detailText, footer;
    RawImage detailImage;
    Image experienceFill;
    EventSystem events;
    bool open, savedCursor, savedNavigation;
    float savedTimeScale, nextNavigation;
    CursorLockMode savedLock;
    GameObject savedSelection;
    int memberIndex, closedFrame = -1;
    Page page;
    BattleActionCategory category = BattleActionCategory.Attack;
    ProgressionEntryKind progressionKind;
    BattleActionOption assigning;
    CharacterStat? pendingStat;
    Vector2 lastMenuSize;
    readonly List<Button> buttons = new();
    readonly Dictionary<BattleCharacterInfo, Texture> portraits = new();
    BattlePortraitCapture capture;
    public bool BlocksWorldInput => open || closedFrame == Time.frameCount;
    public bool IsOpen => open;
    public Page CurrentPage => page;
    bool InBattle => BattleManager.Instance && BattleManager.Instance.InBattle;
    GameManager Manager => GameManager.Instance;
    BattleCharacterInfo Member => Manager && Manager.Progression.Members.Count > 0
        ? Manager.Progression.Members[memberIndex = Mathf.Clamp(memberIndex, 0, Manager.Progression.Members.Count - 1)] : null;
    public static string DisplayName(BattleCharacterInfo member) => !member ? "No character" :
        string.IsNullOrWhiteSpace(member.DisplayName) ? member.name.Replace("(Clone)", "") : member.DisplayName;
    void Awake() { if (PlayerPrefs.HasKey("PlayerMenu.Volume")) AudioListener.volume = PlayerPrefs.GetFloat("PlayerMenu.Volume"); }
    void Update()
    {
        if (!Manager) return;
        if (InBattle) { Close(); return; }
        if (!Application.isFocused) return;
        var k = Keyboard.current; var pad = Gamepad.current;
        bool toggle = (k != null && k.tabKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame);
        if (toggle) { if (open) Close(); else if (!Manager.WorldInputBlocked) Open(); return; }
        if (!open) return;
        if ((shell.rect.size-lastMenuSize).sqrMagnitude>1) Rebuild();
        if ((k != null && (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame)) || (pad != null && pad.buttonEast.wasPressedThisFrame)) { Back(); return; }
        if ((k != null && k.leftBracketKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame)) { Cycle(-1); return; }
        if ((k != null && k.rightBracketKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame)) { Cycle(1); return; }
        Vector2 direction = pad == null ? Vector2.zero : pad.dpad.ReadValue();
        if (direction.sqrMagnitude < .1f && pad != null) direction = pad.leftStick.ReadValue();
        if (k != null) direction += new Vector2((k.rightArrowKey.isPressed ? 1 : 0) - (k.leftArrowKey.isPressed ? 1 : 0), (k.upArrowKey.isPressed ? 1 : 0) - (k.downArrowKey.isPressed ? 1 : 0));
        if (direction.sqrMagnitude < .25f) nextNavigation = 0;
        else if (Time.unscaledTime >= nextNavigation)
        {
            nextNavigation = Time.unscaledTime + .18f;
            var current = events.currentSelectedGameObject ? events.currentSelectedGameObject.GetComponent<Selectable>() : null;
            Selectable next = !current ? null : Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                ? (direction.x > 0 ? current.FindSelectableOnRight() : current.FindSelectableOnLeft())
                : (direction.y > 0 ? current.FindSelectableOnUp() : current.FindSelectableOnDown());
            if (next) events.SetSelectedGameObject(next.gameObject); else if (!current) SelectFirst();
        }
        if ((k != null && k.enterKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame))
        {
            var selected = events.currentSelectedGameObject ? events.currentSelectedGameObject.GetComponent<Button>() : null;
            if (selected && selected.IsInteractable()) selected.onClick.Invoke();
        }
        if (pad != null && Mathf.Abs(pad.rightStick.y.ReadValue()) > .2f) ScrollUnderPointer(pad.rightStick.y.ReadValue());
        footer.text = pad != null && Manager.Input && Manager.Input.CurrentScheme == PlayerInputReader.ControlScheme.Gamepad
            ? ControllerGlyphs.Adapt("D-pad / left stick: navigate    A: select    controller B: back    LB / RB: character    Right stick: scroll    Menu: close")
            : "Arrow keys: navigate    Enter: select    Backspace / Esc: back    [ / ]: character    Tab: close";
    }
    void ScrollUnderPointer(float direction)
    {
        ScrollRect scroll = null;
        if (Mouse.current != null)
        {
            var hits = new List<RaycastResult>(); events.RaycastAll(new PointerEventData(events) { position = Mouse.current.position.ReadValue() }, hits);
            foreach (var hit in hits) { scroll = hit.gameObject.GetComponentInParent<ScrollRect>(); if (scroll) break; }
        }
        if (!scroll && events.currentSelectedGameObject) scroll = events.currentSelectedGameObject.GetComponentInParent<ScrollRect>();
        if (scroll && scroll.content.rect.height > scroll.viewport.rect.height)
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + direction * Time.unscaledDeltaTime * 600 / (scroll.content.rect.height - scroll.viewport.rect.height));
    }
    public void Open()
    {
        if (open || InBattle || !Manager || Manager.IsSwitchingCharacter || Manager.WorldInputBlocked || Manager.Progression.Members.Count == 0) return;
        if (!canvasObject) Build();
        savedCursor = Cursor.visible; savedLock = Cursor.lockState; savedTimeScale = Time.timeScale;
        events = EventSystem.current;
        if (!events) { ownedEvents = new GameObject("Player menu input", typeof(EventSystem), typeof(InputSystemUIInputModule)); events = ownedEvents.GetComponent<EventSystem>(); }
        savedNavigation = events.sendNavigationEvents; savedSelection = events.currentSelectedGameObject; events.sendNavigationEvents = false;
        open = true; canvasObject.SetActive(true); Time.timeScale = 0;
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        if (Manager.PossessedCharacter) Manager.PossessedCharacter.ResetMotion();
        ShowPage(Page.Party);
    }
    public void Close()
    {
        if (!open) return;
        open = false; closedFrame = Time.frameCount; Time.timeScale = savedTimeScale;
        if (canvasObject) canvasObject.SetActive(false);
        if (events) { events.sendNavigationEvents = savedNavigation; events.SetSelectedGameObject(savedSelection ? savedSelection : null); }
        Cursor.visible = savedCursor; Cursor.lockState = savedLock;
    }
    public void Back() { if (pendingStat.HasValue) { pendingStat=null; Rebuild(); } else if (assigning != null) { assigning = null; Rebuild(); } else if (page != Page.Party) ShowPage(Page.Party); else Close(); }
    void Cycle(int direction) { int count = Manager.Progression.Members.Count; memberIndex = (memberIndex + direction + count) % count; assigning = null; pendingStat=null; Rebuild(); }
    public void ShowPage(Page value) { if (!open) return; page = value; assigning = null; pendingStat=null; Rebuild(); }
    void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--) { var child = parent.GetChild(i).gameObject; child.SetActive(false); if (Application.isPlaying) Destroy(child); else DestroyImmediate(child); }
    }
    void Rebuild()
    {
        Canvas.ForceUpdateCanvases(); lastMenuSize=shell.rect.size;
        events.SetSelectedGameObject(null); Clear(body); Clear(detailRoot); buttons.Clear();
        title.text = page == Page.SkillTree ? "SKILL TREE" : page.ToString().ToUpperInvariant();
        characterLabel.text = DisplayName(Member); BuildDetails();
        switch (page) { case Page.Party: Party(); break; case Page.Attributes: Attributes(); break; case Page.Shortcuts: Shortcuts(); break; case Page.Progression: Progression(); break; case Page.SkillTree: SkillTree(); break; case Page.Inventory: Inventory(); break; case Page.Settings: Settings(); break; }
        SelectFirst();
        if(page==Page.Party) {
            var selected=buttons.Find(b=>b.name==DisplayName(Member));
            if(selected)events.SetSelectedGameObject(selected.gameObject);
        }
        if (pendingStat.HasValue) {
            var confirm=buttons.Find(b=>b.name.StartsWith("Confirm"));
            if(confirm && confirm.IsInteractable()) events.SetSelectedGameObject(confirm.gameObject);
            PreviewAttribute(pendingStat.Value);
        }
    }
    void SelectFirst()
    {
        var first = buttons.Find(b => b && b.IsInteractable());
        if (!first && canvasObject) foreach (var button in canvasObject.GetComponentsInChildren<Button>())
            if (button.IsInteractable()) { first = button; break; }
        if (first && events) events.SetSelectedGameObject(first.gameObject);
    }
    void OnDisable() => Close();
    void OnDestroy() { Close(); if (ownedEvents) { if (Application.isPlaying) Destroy(ownedEvents); else DestroyImmediate(ownedEvents); } }
}
