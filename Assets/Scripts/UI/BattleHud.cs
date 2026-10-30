using System.Collections.Generic;
using TMPro;
using Tenebrasia.GridPrototype;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class BattleHud : MonoBehaviour
{
    private sealed class Card
    {
        public RectTransform Rect;
        public TMP_Text Health, Hits;
        public RawImage Portrait;
        public Image Border;
        public int Index;
    }
    private InSceneGridBattle battle;
    public int InspectionDismissedFrame { get; private set; } = -1;
    private TMP_FontAsset font;
    private RectTransform root, orderPanel, orderContent, inspectionLayer, contextPanel, examinePanel, examineContent;
    private TMP_Text examineText, layoutLabel, cameraLabel, targetName, targetHealth;
    private RectTransform targetPanel, targetFill;
    private readonly List<Card> cards = new();
    private readonly List<Texture> portraits = new();
    private ScrollRect orderScroll;
    private bool top = true, autoCamera = true;
    private int lastActor = -1, lastCount = -1, inspected = -1;
    private static readonly Color Dark = new Color(0.035f, 0.045f, 0.065f, 0.96f);

    public void Bind(InSceneGridBattle source, TMP_FontAsset textFont)
    {
        if (battle) battle.ContextRequested -= ShowContext;
        battle = source; font = textFont; root = (RectTransform)transform;
        for (int i = transform.childCount - 1; i >= 0; i--) Remove(transform.GetChild(i).gameObject);
        cards.Clear(); portraits.Clear();
        orderPanel = Rect("Turn order", root); orderPanel.gameObject.AddComponent<Image>().color = Dark;
        orderPanel.gameObject.AddComponent<RectMask2D>();
        orderScroll = orderPanel.gameObject.AddComponent<ScrollRect>(); orderScroll.viewport = orderPanel;
        orderScroll.movementType = ScrollRect.MovementType.Clamped; orderScroll.scrollSensitivity = 30;
        orderContent = Rect("Upcoming turns", orderPanel); orderScroll.content = orderContent;
        var capture = orderContent.gameObject.AddComponent<BattlePortraitCapture>();
        for (int i = 0; i < battle.Combatants.Count; i++)
        {
            var info = battle.Info(i);
            portraits.Add(info && info.Portrait ? info.Portrait : capture.Capture(battle.Actor(i), i));
            cards.Add(MakeCard());
        }
        layoutLabel = Setting("Order placement", new Vector2(-12, 42), () => { top = !top; ApplyLayout(); });
        cameraLabel = Setting("Automatic camera", new Vector2(-12, 12), () =>
        { autoCamera = !autoCamera; battle.SetAutoCamera(autoCamera); cameraLabel.text = "Auto camera: " + (autoCamera ? "On" : "Off"); });
        cameraLabel.text = "Auto camera: " + (autoCamera ? "On" : "Off");
        BuildTargetDetails(); BuildInspection(); battle.ContextRequested += ShowContext;
        battle.SetAutoCamera(autoCamera); ApplyLayout(); Render();
    }
    private static void Remove(GameObject obj)
    { obj.SetActive(false); if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
    private static RectTransform Rect(string name, Transform parent)
    { var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false); return (RectTransform)obj.transform; }
    private static void Stretch(RectTransform rect)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private TMP_Text Text(RectTransform parent, float size = 14)
    {
        var rect = Rect("Label", parent); Stretch(rect); rect.offsetMin = new Vector2(6, 2); rect.offsetMax = new Vector2(-6, -2);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size;
        text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return text;
    }
    private Card MakeCard()
    {
        var card = new Card { Rect = Rect("Combatant portrait", orderContent) };
        card.Border = card.Rect.gameObject.AddComponent<Image>();
        var picture = Rect("Portrait", card.Rect); Stretch(picture); picture.offsetMin = Vector2.one * 3; picture.offsetMax = Vector2.one * -3;
        card.Portrait = picture.gameObject.AddComponent<RawImage>(); card.Portrait.raycastTarget = false;
        var hp = Rect("HP overlay", card.Rect); hp.anchorMin = Vector2.zero; hp.anchorMax = new Vector2(1, 0);
        hp.pivot = new Vector2(0.5f, 0); hp.offsetMin = new Vector2(3, 3); hp.offsetMax = new Vector2(-3, 21);
        var back = hp.gameObject.AddComponent<Image>(); back.color = new Color(0, 0, 0, 0.8f); back.raycastTarget = false;
        card.Health = Text(hp, 11);
        card.Hits = Text(card.Rect, 14); card.Hits.alignment = TextAlignmentOptions.TopRight; card.Hits.color = new Color(1, 0.8f, 0.25f);
        var click = card.Rect.gameObject.AddComponent<BattlePortrait>();
        click.LeftClick = () => { battle.FocusCharacter(card.Index); battle.TargetFocus.Pin(battle.Targetables[card.Index]); };
        click.RightClick = p => ShowContext(card.Index, p);
        return card;
    }
    private TMP_Text Button(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var image = parent.gameObject.AddComponent<Image>(); image.color = Dark;
        var button = parent.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
        var text = Text(parent); text.text = label; return text;
    }
    private TMP_Text Setting(string name, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(name, root); rect.anchorMin = rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = position; rect.sizeDelta = new Vector2(160, 26); return Button(rect, name, action);
    }
    public void ApplyLayout()
    {
        if (!orderPanel) return;
        int count = battle.UpcomingTurns().Count;
        orderPanel.anchorMin = orderPanel.anchorMax = top ? new Vector2(0.5f, 1) : Vector2.one;
        orderPanel.pivot = top ? new Vector2(0.5f, 1) : Vector2.one;
        orderPanel.anchoredPosition = top ? new Vector2(0, -6) : new Vector2(-12, -6);
        orderPanel.sizeDelta = top ? new Vector2(Mathf.Min(Mathf.Max(120, root.rect.width - 48), count * 56 + 8), 70)
            : new Vector2(60, Mathf.Min(Mathf.Max(70, root.rect.height - 200), count * 66 + 8));
        orderContent.anchorMin = orderContent.anchorMax = new Vector2(0, 1); orderContent.pivot = new Vector2(0, 1);
        orderContent.sizeDelta = top ? new Vector2(count * 56 + 8, 70) : new Vector2(60, count * 66 + 8);
        orderScroll.horizontal = top; orderScroll.vertical = !top;
        for (int i = 0; i < cards.Count; i++)
        {
            var rect = cards[i].Rect; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = top ? new Vector2(4 + i * 56, -4) : new Vector2(4, -4 - i * 66);
            rect.sizeDelta = new Vector2(52, 62);
        }
        orderScroll.horizontalNormalizedPosition = 0; orderScroll.verticalNormalizedPosition = 1;
        if (targetPanel)
        {
            targetPanel.anchorMin = targetPanel.anchorMax = top ? new Vector2(0.5f, 1) : Vector2.one;
            targetPanel.pivot = top ? new Vector2(0.5f, 1) : Vector2.one;
            targetPanel.anchoredPosition = top ? new Vector2(0, -80) : new Vector2(-12, -orderPanel.sizeDelta.y - 14);
        }
        layoutLabel.text = "Turn order: " + (top ? "Top" : "Right");
    }
    private void BuildTargetDetails()
    {
        targetPanel = Rect("Hovered or pinned target", root); targetPanel.sizeDelta = new Vector2(280, 48);
        var nameRect = Rect("Target name", targetPanel); nameRect.anchorMin = new Vector2(0, 1); nameRect.anchorMax = Vector2.one;
        nameRect.pivot = new Vector2(0.5f, 1); nameRect.sizeDelta = new Vector2(0, 24); nameRect.anchoredPosition = new Vector2(0, -4);
        targetName = Text(nameRect, 16); targetName.overflowMode = TextOverflowModes.Ellipsis;
        targetName.outlineColor = Color.black; targetName.outlineWidth = 0.2f;
        var track = Rect("Target maximum health", targetPanel); track.anchorMin = Vector2.zero; track.anchorMax = new Vector2(1, 0);
        track.pivot = new Vector2(0.5f, 0); track.offsetMin = new Vector2(0, 2); track.offsetMax = new Vector2(0, 20);
        var bar = track.gameObject.AddComponent<Image>(); bar.color = new Color(0.18f, 0.18f, 0.18f); bar.raycastTarget = false;
        targetFill = Rect("Target current health", track); Stretch(targetFill);
        var fill = targetFill.gameObject.AddComponent<Image>(); fill.color = new Color(0.75f, 0.12f, 0.1f); fill.raycastTarget = false;
        targetHealth = Text(track, 12); targetPanel.gameObject.SetActive(false);
    }
    private void DrawTargetDetails()
    {
        var target = battle.TargetFocus.Displayed;
        targetPanel.gameObject.SetActive(target != null && !battle.InspectionOpen);
        if (target == null) return;
        targetName.text = target.Name; targetHealth.text = $"{target.Health}/{target.MaxHealth}";
        targetFill.anchorMax = new Vector2(Mathf.Clamp01(target.Health / (float)Mathf.Max(1, target.MaxHealth)), 1);
    }
    private void BuildInspection()
    {
        inspectionLayer = Rect("Inspection modal", root); Stretch(inspectionLayer);
        var shade = inspectionLayer.gameObject.AddComponent<Image>(); shade.color = new Color(0, 0, 0, 0.35f);
        var dismiss = inspectionLayer.gameObject.AddComponent<Button>(); dismiss.targetGraphic = shade; dismiss.onClick.AddListener(CloseInspection);
        contextPanel = Rect("Character context menu", inspectionLayer); contextPanel.sizeDelta = new Vector2(140, 30);
        Button(contextPanel, "Examine", () => ShowExamine(inspected));
        examinePanel = Rect("Examine character", inspectionLayer); examinePanel.gameObject.AddComponent<Image>().color = Dark;
        var panelBlocker = examinePanel.gameObject.AddComponent<Button>(); panelBlocker.targetGraphic = examinePanel.GetComponent<Image>();
        var panelNav = panelBlocker.navigation; panelNav.mode = Navigation.Mode.None; panelBlocker.navigation = panelNav;
        examinePanel.sizeDelta = new Vector2(340, Mathf.Min(430, Mathf.Max(200, root.rect.height - 60)));
        var close = Rect("Close examine", examinePanel); close.anchorMin = close.anchorMax = new Vector2(1, 1);
        close.pivot = Vector2.one; close.anchoredPosition = new Vector2(-8, -8); close.sizeDelta = new Vector2(80, 32); Button(close, "Close", CloseInspection);
        var viewport = Rect("Examine scroll", examinePanel); Stretch(viewport); viewport.offsetMin = new Vector2(18, 16); viewport.offsetMax = new Vector2(-18, -48);
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>(); var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
        examineContent = Rect("Character details", viewport); examineContent.anchorMin = new Vector2(0, 1); examineContent.anchorMax = Vector2.one;
        examineContent.pivot = new Vector2(0.5f, 1); scroll.content = examineContent;
        examineText = Text(examineContent, 15); examineText.alignment = TextAlignmentOptions.TopLeft;
        CloseInspection();
    }
    public void ShowContext(int index, Vector2 screenPosition)
    {
        if (!battle || index < 0 || index >= battle.Combatants.Count) return;
        inspected = index; battle.InspectionOpen = true; inspectionLayer.gameObject.SetActive(true);
        contextPanel.gameObject.SetActive(true); examinePanel.gameObject.SetActive(false);
        var canvas = GetComponentInParent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var p);
        contextPanel.anchoredPosition = new Vector2(Mathf.Clamp(p.x, root.rect.xMin + 70, root.rect.xMax - 70), Mathf.Clamp(p.y, root.rect.yMin + 15, root.rect.yMax - 15));
    }
    public void ShowExamine(int index)
    {
        inspected = index; battle.InspectionOpen = true; inspectionLayer.gameObject.SetActive(true);
        contextPanel.gameObject.SetActive(false); examinePanel.gameObject.SetActive(true); UpdateExamine();
    }
    private void UpdateExamine()
    {
        var info = battle.Info(inspected); examineText.text = info.Details(battle.Combatants[inspected], battle.DisplayName(inspected));
        examineContent.sizeDelta = new Vector2(0, examineText.GetPreferredValues(examineText.text, Mathf.Max(100, examinePanel.rect.width - 50), 0).y + 16);
    }
    public void CloseInspection()
    { if (battle) { if (battle.InspectionOpen) InspectionDismissedFrame = Time.frameCount; battle.InspectionOpen = false; } if (inspectionLayer) inspectionLayer.gameObject.SetActive(false); }
    private void OnDisable() => CloseInspection();
    private void OnDestroy() { if (battle) battle.ContextRequested -= ShowContext; }
    private void LateUpdate() => Render();
    public void Render()
    {
        if (!battle) return;
        if (battle.InspectionOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseInspection();
        if (battle.InspectionOpen && examinePanel.gameObject.activeSelf) UpdateExamine();
        DrawTargetDetails();
        var order = battle.UpcomingTurns();
        if (lastActor != battle.CurrentActorIndex || lastCount != order.Count)
        { ApplyLayout(); lastActor = battle.CurrentActorIndex; lastCount = order.Count; }
        for (int slot = 0; slot < cards.Count; slot++)
        {
            var card = cards[slot]; card.Rect.gameObject.SetActive(slot < order.Count); if (slot >= order.Count) continue;
            int i = order[slot]; card.Index = i; card.Portrait.texture = portraits[i];
            card.Portrait.color = portraits[i] ? Color.white : new Color(0.3f, 0.35f, 0.43f);
            var unit = battle.Combatants[i]; card.Health.text = $"{unit.Health}/{unit.MaxHealth}";
            int hits = battle.AssignedHits(i); card.Hits.text = hits > 0 ? $"x{hits}" : "";
            card.Border.color = i == battle.CurrentActorIndex ? new Color(1, 0.75f, 0.25f) : Dark;
        }
    }
}
