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
    public enum Page { Party, Attributes, Shortcuts, Progression, SkillTree, Inventory, Settings, Saves }
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
    int? pendingShortcutSlot;
    string shortcutSelection;
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
    void Awake() { if (PlayerPrefs.HasKey("PlayerMenu.Volume")) AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("PlayerMenu.Volume")); }
    void Update()
    {
        TickDisplayPreview();
        if (!Manager) return;
        if(canvasObject){var group=canvasObject.GetComponent<CanvasGroup>();if(!group)group=canvasObject.AddComponent<CanvasGroup>();group.interactable=!(Manager.GetComponent<SaveGameService>()&&Manager.GetComponent<SaveGameService>().Busy);}
        if(Manager.GetComponent<SaveGameService>()&&Manager.GetComponent<SaveGameService>().Busy)return;
        if (InBattle) { Close(); return; }
        if (!Application.isFocused) return;
        var k = Keyboard.current; var pad = Gamepad.current;
        bool toggle = (k != null && k.tabKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame);
        if (toggle) { if (open) Close(); else if (!Manager.WorldInputBlocked) Open(); return; }
        if (!open) return;
        if ((shell.rect.size-lastMenuSize).sqrMagnitude>1 && !inventoryModal) Rebuild();
        if(page==Page.Shortcuts && HandleShortcutInput(k,pad)) return;
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
        if (pad != null && pad.rightStick.ReadValue().sqrMagnitude > .04f) ScrollUnderPointer(pad.rightStick.ReadValue());
        footer.text = pad != null && Manager.Input && Manager.Input.CurrentScheme == PlayerInputReader.ControlScheme.Gamepad
            ? ControllerGlyphs.Adapt("D-pad / left stick: navigate    A: select    controller B: back    LB / RB: character    Right stick: scroll    Menu: close")
            : "Arrow keys: navigate    Enter: select    Backspace / Esc: back    [ / ]: character    Tab: close";
    }
    void ScrollUnderPointer(Vector2 direction)
    {
        ScrollRect scroll = null;
        if (Mouse.current != null)
        {
            var hits = new List<RaycastResult>(); events.RaycastAll(new PointerEventData(events) { position = Mouse.current.position.ReadValue() }, hits);
            foreach (var hit in hits) { scroll = hit.gameObject.GetComponentInParent<ScrollRect>(); if (scroll) break; }
        }
        if (!scroll && events.currentSelectedGameObject) scroll = events.currentSelectedGameObject.GetComponentInParent<ScrollRect>();
        if (scroll && scroll.vertical && scroll.content.rect.height > scroll.viewport.rect.height)
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + direction.y * Time.unscaledDeltaTime * 600 / (scroll.content.rect.height - scroll.viewport.rect.height));
        if (scroll && scroll.horizontal && scroll.content.rect.width > scroll.viewport.rect.width)
            scroll.horizontalNormalizedPosition = Mathf.Clamp01(scroll.horizontalNormalizedPosition + direction.x * Time.unscaledDeltaTime * 600 / (scroll.content.rect.width - scroll.viewport.rect.width));
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
        CancelDisplayChange(); resetSettingsPending=false; CloseInventoryModal(); HideItemTooltip(); open = false; closedFrame = Time.frameCount; Time.timeScale = savedTimeScale;
        if (canvasObject) canvasObject.SetActive(false);
        if (events) { events.sendNavigationEvents = savedNavigation; events.SetSelectedGameObject(savedSelection ? savedSelection : null); }
        Cursor.visible = savedCursor; Cursor.lockState = savedLock;
    }
    public void Back() { if(savePendingSlot>=0){savePendingSlot=-1;Rebuild();return;} if(BackFromSettings())return; if (inventoryModal) { InventoryBack(); return; } if (pendingShortcutSlot.HasValue) { pendingShortcutSlot=null; Rebuild(); } else if (pendingStat.HasValue) { pendingStat=null; Rebuild(); } else if (assigning != null) { assigning = null; Rebuild(); } else if (page != Page.Party) ShowPage(Page.Party); else Close(); }
    void Cycle(int direction) { if (inventoryModal || displayPreview.Active || resetSettingsPending) return; int count = Manager.Progression.Members.Count; memberIndex = (memberIndex + direction + count) % count; progressionSelection=null; selectedSkillNode=null; assigning = null; pendingStat=null; pendingShortcutSlot=null;shortcutSelection=null; Rebuild(); }
    public void ShowPage(Page value) { if (!open) return; CancelDisplayChange(); resetSettingsPending=false; settingsSelection=null; CloseInventoryModal(); HideItemTooltip(); page = value; savePendingSlot=-1; progressionSelection=null; selectedSkillNode=null; assigning = null; pendingStat=null; pendingShortcutSlot=null;shortcutSelection=null; Rebuild(); }
    void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--) { var child = parent.GetChild(i).gameObject; child.SetActive(false); if (Application.isPlaying) Destroy(child); else DestroyImmediate(child); }
    }
    void Rebuild()
    {
        Canvas.ForceUpdateCanvases(); lastMenuSize=shell.rect.size;
        shownProgressionEntry=null; events.SetSelectedGameObject(null); Clear(body); Clear(detailRoot); buttons.Clear();
        title.text = page == Page.SkillTree ? "SKILL TREE" : page.ToString().ToUpperInvariant();
        characterLabel.text = DisplayName(Member); BuildDetails();
        switch (page) { case Page.Party: Party(); break; case Page.Attributes: Attributes(); break; case Page.Shortcuts: Shortcuts(); break; case Page.Progression: Progression(); break; case Page.SkillTree: SkillTree(); break; case Page.Inventory: Inventory(); break; case Page.Settings: Settings(); break; case Page.Saves: Saves(); break; }
        if(page!=Page.SkillTree || skillCards.Count==0) SelectFirst();
        if(page==Page.Party) {
            var selected=buttons.Find(b=>b.name==DisplayName(Member));
            if(selected)events.SetSelectedGameObject(selected.gameObject);
        }
        if(page==Page.Settings) RestoreSettingsSelection();
        if(page==Page.Progression) RestoreProgressionSelection();
        if(page==Page.SkillTree) RestoreSkillSelection();
        if(page==Page.Shortcuts) {
            var target=buttons.Find(b=>b.name==shortcutSelection);
            if(target && target.IsInteractable())events.SetSelectedGameObject(target.gameObject);
            if(assigning!=null && pendingShortcutSlot.HasValue) PreviewShortcut(pendingShortcutSlot.Value,assigning);
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
    void OnDestroy() { if(menuSaveService)menuSaveService.Changed-=SaveMenuChanged; Close(); if (ownedEvents) { if (Application.isPlaying) Destroy(ownedEvents); else DestroyImmediate(ownedEvents); } }
}
