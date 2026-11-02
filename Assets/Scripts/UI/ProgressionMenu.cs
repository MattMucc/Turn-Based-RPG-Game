using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// A compact prototype allocation panel; not the future inventory/system menu.
[DefaultExecutionOrder(-50)]
public sealed class ProgressionMenu : MonoBehaviour
{
    GameObject canvasObject, ownedEvents;
    RectTransform panel;
    TMP_Text heading, summary;
    readonly TMP_Text[] statLabels = new TMP_Text[6];
    readonly Button[] statButtons = new Button[6];
    readonly string[] names = { "Vitality", "Strength", "Magic", "Physical Defense", "Magical Defense", "Initiative" };
    bool open, savedCursor, savedNavigation;
    CursorLockMode savedLock;
    EventSystem events;
    int memberIndex, selected, closedFrame = -1;
    public bool BlocksWorldInput => open || closedFrame == Time.frameCount;
    bool InBattle => BattleManager.Instance && BattleManager.Instance.InBattle;
    void Update()
    {
        if (!GameManager.Instance) return;
        if (InBattle) { Close(); return; }
        if (!Application.isFocused) return;
        var keyboard = Keyboard.current; var pad = Gamepad.current;
        bool toggle = (keyboard != null && keyboard.tabKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame);
        if (toggle) { if (open) Close(); else Open(); return; }
        if (!open) return;
        if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame)) { Close(); return; }
        if ((keyboard != null && keyboard.upArrowKey.wasPressedThisFrame) || (pad != null && pad.dpad.up.wasPressedThisFrame)) selected = (selected + 5) % 6;
        if ((keyboard != null && keyboard.downArrowKey.wasPressedThisFrame) || (pad != null && pad.dpad.down.wasPressedThisFrame)) selected = (selected + 1) % 6;
        if ((keyboard != null && keyboard.leftArrowKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame)) Cycle(-1);
        if ((keyboard != null && keyboard.rightArrowKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame)) Cycle(1);
        if ((keyboard != null && keyboard.enterKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame)) Spend(selected);
        Render();
    }
    public void Open()
    {
        if (open || InBattle || !GameManager.Instance || GameManager.Instance.Progression.Members.Count == 0) return;
        if (!canvasObject) Build();
        savedCursor = Cursor.visible; savedLock = Cursor.lockState;
        events = EventSystem.current;
        if (!events)
        {
            ownedEvents = new GameObject("Progression input", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events = ownedEvents.GetComponent<EventSystem>();
        }
        savedNavigation = events.sendNavigationEvents; events.sendNavigationEvents = false;
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        open = true; canvasObject.SetActive(true);
        if (GameManager.Instance.PossessedCharacter) GameManager.Instance.PossessedCharacter.ResetMotion();
        Render();
    }
    public void Close()
    {
        if (!open) return;
        open = false; closedFrame = Time.frameCount;
        if (canvasObject) canvasObject.SetActive(false);
        if (events) events.sendNavigationEvents = savedNavigation;
        Cursor.visible = savedCursor; Cursor.lockState = savedLock;
    }
    void Cycle(int step)
    {
        int count = GameManager.Instance.Progression.Members.Count;
        if (count > 0) memberIndex = (memberIndex + step + count) % count;
        Render();
    }
    BattleCharacterInfo Member()
    {
        var members = GameManager.Instance.Progression.Members;
        if (members.Count == 0) return null;
        memberIndex = Mathf.Clamp(memberIndex, 0, members.Count - 1); return members[memberIndex];
    }
    void Spend(int index)
    {
        if (!open || InBattle) return;
        var member = Member(); if (member) member.SpendPoint((CharacterStat)index);
        Render();
    }
    void Render()
    {
        if (!panel || !GameManager.Instance) return;
        var member = Member(); var party = GameManager.Instance.Progression;
        heading.text = member ? (string.IsNullOrWhiteSpace(member.DisplayName) ? member.name.Replace("(Clone)", "") : member.DisplayName) : "No character";
        summary.text = member ? $"Level {member.Level}   •   {member.UnspentPoints} points available\n" +
            (party.Level >= party.LevelCap ? "Party XP: level cap reached" : $"Party XP: {party.Progress} / {party.Cost(party.Level)}") : "Register a party member.";
        for (int i = 0; i < 6; i++)
        {
            statLabels[i].text = names[i] + "   " + (member ? member.Stat((CharacterStat)i).ToString() : "—");
            statButtons[i].interactable = member && member.UnspentPoints > 0;
            statButtons[i].image.color = i == selected ? new Color(1, .82f, .38f) : new Color(.85f, .87f, .9f);
        }
    }
    RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
        var rect = (RectTransform)obj.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
    }
    TMP_Text Label(RectTransform rect, string value, int size = 15)
    {
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size; text.color = Color.white; text.text = value; text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft; return text;
    }
    Button Button(string value, float x, float y, float width, UnityEngine.Events.UnityAction click)
    {
        var rect = Rect(value, panel, x, y, width, 28); var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.85f, .87f, .9f); var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None }; button.onClick.AddListener(click);
        var label = Label(Rect("Label", rect, 0, 0, width, 28), value); label.color = Color.black; label.alignment = TextAlignmentOptions.Center;
        return button;
    }
    void Build()
    {
        canvasObject = new GameObject("Character progression", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        panel = Rect("Stats", canvasObject.transform, 0, 0, 340, 400);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f); panel.anchoredPosition = Vector2.zero;
        panel.gameObject.AddComponent<Image>().color = new Color(.035f, .045f, .065f, .97f);
        heading = Label(Rect("Name", panel, 16, 12, 220, 28), "Character", 18);
        Button("<", 246, 12, 32, () => Cycle(-1)); Button(">", 286, 12, 32, () => Cycle(1));
        summary = Label(Rect("Experience", panel, 16, 46, 308, 44), "");
        for (int i = 0; i < 6; i++)
        {
            int index = i; statLabels[i] = Label(Rect(names[i], panel, 16, 102 + i * 34, 245, 28), names[i]);
            statButtons[i] = Button("+", 278, 102 + i * 34, 40, () => Spend(index));
        }
        Label(Rect("Help", panel, 16, 310, 308, 48), "D-pad / ↑↓: stat   A/Cross / Enter: spend\nLB/RB / ←→: character", 12);
        Button("Close · Tab / Esc / B / Circle", 16, 362, 308, Close);
    }
    void OnDisable() => Close();
    void OnDestroy() { Close(); if (ownedEvents) Destroy(ownedEvents); }
}
