using System.Collections.Generic;
using TMPro;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class BattleHud : MonoBehaviour
{
    private sealed class Card
    {
        public RectTransform Rect;
        public TMP_Text Health, Hits, ShieldText;
        public RectTransform HealthFill, Shield, ShieldFill;
        public RawImage Portrait;
        public Image Border;
        public int Index;
    }
    private InSceneGridBattle battle;
    private BattleOverheadDisplay overhead;
    public int InspectionDismissedFrame { get; private set; } = -1;
    private TMP_FontAsset font;
    private RectTransform root, orderPanel, orderContent, inspectionLayer, contextPanel, examinePanel, examineContent;
    private BattleStatusRow targetStatuses, examineStatuses;
    private BattleTarget lastStatusTarget;
    private float targetStatusGrace;
    private int targetDismissVersion;
    private TMP_Text damagePreview;
    private TMP_Text examineText, layoutLabel, cameraLabel, targetName, targetHealth, targetShieldText;
    private RectTransform targetShield, targetShieldFill;
    private RectTransform targetPanel, targetFill, defensePanel, defenseMarker, impactLine, dodgeWindow, parryWindow;
    private TMP_Text defenseCaption, defensePrompt, defenseResult, assistanceLabel;
    private RectTransform qteLayer, qtePrompt, qteFill;
    private TMP_Text qteCaption, qteCountdown, qteKey;
    private Image qteIcon, dualIcon;
    private RectTransform dualPrompt;
    private readonly List<Image> sequenceIcons = new();
    private readonly List<Image> sequenceFrames = new();
    private QteProgressRing tapRing;
    private QteTriangleCue triangleCue;
    private Sprite qteKeyboardIcon, qteControllerIcon;
    public void SetQteIcons(Sprite keyboard, Sprite controller)
    { qteKeyboardIcon = keyboard; qteControllerIcon = controller; }
    private readonly List<Card> cards = new();
    private readonly List<Texture> portraits = new();
    private ScrollRect orderScroll;
    private bool top = true, autoCamera = true;
    private BattleTarget inspectedTarget;
    private int controllerPortrait = -1;
    private int lastActor = -1, lastCount = -1, inspected = -1;
    private static readonly Color Dark = new Color(0.035f, 0.045f, 0.065f, 0.96f);

    public void Bind(InSceneGridBattle source, TMP_FontAsset textFont)
    {
        if (battle) { battle.ContextRequested -= ShowContext; battle.TargetContextRequested -= ShowTargetContext; }
        if(overhead)Remove(overhead.gameObject);
        battle = source; font = textFont; root = (RectTransform)transform;
        var overheadObject=new GameObject("Battle overhead displays");overheadObject.transform.SetParent(battle.transform,false);
        overhead=overheadObject.AddComponent<BattleOverheadDisplay>();overhead.Bind(battle,font);
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
        assistanceLabel = Setting("Defense timing", new Vector2(-12, 72), () =>
        { if (!battle.DefenseActive && !battle.SignatureActive) battle.DefenseSettings.Assisted = !battle.DefenseSettings.Assisted; });
        BuildDefense(); BuildSignature(); BuildTargetDetails(); BuildInspection(); battle.ContextRequested += ShowContext; battle.TargetContextRequested += ShowTargetContext;
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
        hp.pivot = new Vector2(0.5f, 0); hp.offsetMin = new Vector2(3, 3); hp.offsetMax = new Vector2(-3, 10.5f);
        var back = hp.gameObject.AddComponent<Image>(); back.color = new Color(0, 0, 0, 0.8f); back.raycastTarget = false;
        card.HealthFill = Rect("Portrait health fill", hp); Stretch(card.HealthFill);
        var healthImage = card.HealthFill.gameObject.AddComponent<Image>(); healthImage.color = new Color(.75f, .12f, .1f); healthImage.raycastTarget = false;
        card.Health = Text(hp, 11);
        card.Shield = MakeShield(card.Rect, 12.5f, 7.5f, out card.ShieldFill, out card.ShieldText);
        FitPortraitNumber(card.Health, card.Rect, .17f); FitPortraitNumber(card.ShieldText, card.Rect, .17f);
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
    private static void FitPortraitNumber(TMP_Text text, RectTransform portrait, float relativeSize)
    {
        text.enableAutoSizing = true; text.fontSizeMin = 2;
        text.fontSizeMax = Mathf.Clamp(portrait.rect.width * relativeSize, 2, 18);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.offsetMin = new Vector2(1, 0); text.rectTransform.offsetMax = new Vector2(-1, 0);
    }
    private RectTransform MakeShield(RectTransform parent, float bottom, float height, out RectTransform fill, out TMP_Text label)
    {
        var track = Rect("Barrier shield", parent); track.anchorMin = Vector2.zero; track.anchorMax = new Vector2(1, 0);
        track.offsetMin = new Vector2(3, bottom); track.offsetMax = new Vector2(-3, bottom + height);
        var back = track.gameObject.AddComponent<Image>(); back.color = new Color(.04f, .1f, .2f); back.raycastTarget = false;
        fill = Rect("Barrier remaining", track); Stretch(fill);
        var image = fill.gameObject.AddComponent<Image>(); image.color = new Color(.12f, .5f, 1f); image.raycastTarget = false;
        label = Text(track, 9); label.outlineWidth = .2f; label.outlineColor = Color.black;
        track.gameObject.SetActive(false); return track;
    }
    private void UpdateShield(RectTransform track, RectTransform fill, TMP_Text label, GridUnit unit)
    {
        track.gameObject.SetActive(unit != null && unit.Barrier > 0);
        if (unit == null || unit.Barrier <= 0) return;
        fill.anchorMax = new Vector2(unit.Barrier / (float)Mathf.Max(1, unit.BarrierMaximum), 1);
        label.text = $"{unit.Barrier} / {unit.BarrierMaximum}";
    }
    private void BuildTargetDetails()
    {
        targetPanel = Rect("Hovered or pinned target", root); targetPanel.sizeDelta = new Vector2(280, 60);
        var nameRect = Rect("Target name", targetPanel); nameRect.anchorMin = new Vector2(0, 1); nameRect.anchorMax = Vector2.one;
        nameRect.pivot = new Vector2(0.5f, 1); nameRect.sizeDelta = new Vector2(0, 24); nameRect.anchoredPosition = new Vector2(0, -4);
        targetName = Text(nameRect, 16); targetName.overflowMode = TextOverflowModes.Ellipsis;
        targetName.outlineColor = Color.black; targetName.outlineWidth = 0.2f;
        var track = Rect("Target maximum health", targetPanel); track.anchorMin = Vector2.zero; track.anchorMax = new Vector2(1, 0);
        track.pivot = new Vector2(0.5f, 0); track.offsetMin = new Vector2(0, 2); track.offsetMax = new Vector2(0, 20);
        var bar = track.gameObject.AddComponent<Image>(); bar.color = new Color(0.18f, 0.18f, 0.18f); bar.raycastTarget = false;
        targetFill = Rect("Target current health", track); Stretch(targetFill);
        var fill = targetFill.gameObject.AddComponent<Image>(); fill.color = new Color(0.75f, 0.12f, 0.1f); fill.raycastTarget = false;
        targetHealth = Text(track, 12);
        targetShield = MakeShield(targetPanel, 22, 10, out targetShieldFill, out targetShieldText);
        var statusRect = Rect("Target status effects", targetPanel);
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(.5f,0);statusRect.anchoredPosition=new Vector2(0,-22);statusRect.sizeDelta=new Vector2(280,36);
        targetStatuses=statusRect.gameObject.AddComponent<BattleStatusRow>();targetStatuses.Initialize(root,font);
        var previewRect = Rect("Damage preview and mitigation", targetPanel);
        previewRect.anchorMin = previewRect.anchorMax = new Vector2(.5f,0); previewRect.anchoredPosition = new Vector2(0,-66); previewRect.sizeDelta = new Vector2(360,48);
        damagePreview = Text(previewRect,12); damagePreview.outlineWidth = .2f; damagePreview.outlineColor = Color.black;
        targetPanel.gameObject.SetActive(false);
    }
    private void DrawTargetDetails()
    {
        if(targetDismissVersion!=battle.TargetFocus.DismissVersion)
        { targetDismissVersion=battle.TargetFocus.DismissVersion;lastStatusTarget=null;targetStatusGrace=0; }
        var target = battle.TargetFocus.Displayed;
        if(target!=null){lastStatusTarget=target;targetStatusGrace=Time.unscaledTime+.35f;}
        else if(lastStatusTarget?.Alive==true && (targetStatuses.PointerInside || Time.unscaledTime<targetStatusGrace))target=lastStatusTarget;
        targetPanel.gameObject.SetActive(target != null && !battle.InspectionOpen);
        if (target == null) return;
        damagePreview.text = battle.PreviewDamage(target);
        if (damagePreview.text.Length == 0 && Time.unscaledTime < battle.RecentHitFeedbackUntil) damagePreview.text = battle.RecentHitFeedback;
        targetStatuses.Show(target.StatusUnit, target.Unit == null);
        UpdateShield(targetShield, targetShieldFill, targetShieldText, target.StatusUnit);
        targetFill.parent.gameObject.SetActive(target.HasHealth);
        targetName.text = target.Name; targetHealth.text = $"{target.Health} / {target.MaxHealth}";
        targetFill.anchorMax = new Vector2(Mathf.Clamp01(target.Health / (float)Mathf.Max(1, target.MaxHealth)), 1);
    }
    private void BuildSignature()
    {
        qteLayer = Rect("Signature QTE", root); Stretch(qteLayer);
        var footer = Rect("QTE countdown", qteLayer);
        footer.anchorMin = footer.anchorMax = new Vector2(0.5f, 0);
        footer.pivot = new Vector2(0.5f, 0); footer.anchoredPosition = new Vector2(0, 16);
        footer.sizeDelta = new Vector2(260, 90);
        var caption = Rect("QTE feedback", footer); caption.anchorMin = new Vector2(0, 1); caption.anchorMax = Vector2.one;
        caption.pivot = new Vector2(0.5f, 1); caption.sizeDelta = new Vector2(0, 30);
        qteCaption = Text(caption, 14); qteCaption.outlineWidth = 0.2f; qteCaption.outlineColor = Color.black;
        var number = Rect("Seconds remaining", footer); number.anchorMin = number.anchorMax = new Vector2(0.5f, 0);
        number.anchoredPosition = new Vector2(0, 40); number.sizeDelta = new Vector2(100, 28);
        qteCountdown = Text(number, 20); qteCountdown.outlineWidth = 0.2f; qteCountdown.outlineColor = Color.black;
        var track = Rect("Deadline track", footer); track.anchorMin = track.anchorMax = new Vector2(0.5f, 0);
        track.anchoredPosition = new Vector2(0, 12); track.sizeDelta = new Vector2(220, 8);
        var trackImage = track.gameObject.AddComponent<Image>(); trackImage.color = Dark; trackImage.raycastTarget = false;
        qteFill = Rect("Time remaining fill", track); Stretch(qteFill);
        var fill = qteFill.gameObject.AddComponent<Image>(); fill.color = new Color(1, 0.75f, 0.2f); fill.raycastTarget = false;
        qtePrompt = Rect("QTE input", qteLayer); qtePrompt.sizeDelta = new Vector2(104, 48);
        var background = qtePrompt.gameObject.AddComponent<Image>(); background.color = Color.clear; background.raycastTarget = false;
        var iconRect = Rect("Input icon", qtePrompt); Stretch(iconRect);
        iconRect.offsetMin = new Vector2(12, 6); iconRect.offsetMax = new Vector2(-12, -6);
        qteIcon = iconRect.gameObject.AddComponent<Image>(); qteIcon.preserveAspect = true; qteIcon.raycastTarget = false;
        qteKey = Text(qtePrompt, 18);
        dualPrompt = Rect("QTE second input", qteLayer); dualPrompt.sizeDelta = new Vector2(64, 48);
        var secondIconRect = Rect("Input icon", dualPrompt); Stretch(secondIconRect);
        secondIconRect.offsetMin = new Vector2(12, 6); secondIconRect.offsetMax = new Vector2(-12, -6);
        dualIcon = secondIconRect.gameObject.AddComponent<Image>(); dualIcon.preserveAspect = true; dualIcon.raycastTarget = false;
        dualPrompt.gameObject.SetActive(false);
        var ringRect = Rect("Tap progress ring", qtePrompt); ringRect.sizeDelta = new Vector2(80, 80);
        tapRing = ringRect.gameObject.AddComponent<QteProgressRing>(); tapRing.raycastTarget = false;
        tapRing.color = new Color(0.3f, 1, 0.55f); tapRing.gameObject.SetActive(false);
        var triangleRect = Rect("QTE triangle cue", qtePrompt); triangleRect.sizeDelta = new Vector2(18, 14);
        triangleCue = triangleRect.gameObject.AddComponent<QteTriangleCue>(); triangleCue.gameObject.SetActive(false);
        qteLayer.gameObject.SetActive(false);
    }

    private void DrawSignature()
    {
        qteLayer.gameObject.SetActive(battle.SignatureActive);
        if (!battle.SignatureActive) return;
        if (inspectionLayer.gameObject.activeSelf) CloseInspection();
        var qte = battle.CurrentQte;
        bool controller = GameManager.Instance && GameManager.Instance.Input
            && GameManager.Instance.Input.CurrentScheme == PlayerInputReader.ControlScheme.Gamepad;
        bool dual = qte != null && qte.Mode == QteMode.DualInput;
        bool sequence = qte != null && qte.Mode == QteMode.FixedSequence;
        bool mash = qte != null && qte.Mode == QteMode.RepeatedTap;
        bool hold = qte != null && qte.Mode == QteMode.Hold;
        bool ringPrompt = mash || hold;
        var inputButton = qte != null ? qte.Expected : QteButton.South;
        var lane = sequence ? QteLane.Bottom : inputButton >= QteButton.StickUp || (controller && inputButton >= QteButton.DpadUp)
            ? QteLane.Left : !controller && inputButton == QteButton.South ? QteLane.Bottom : QteLane.Right;
        qtePrompt.anchorMin = qtePrompt.anchorMax = QteLayout.Anchor(lane);
        qtePrompt.anchoredPosition = QteLayout.Offset(lane);
        Color faceColor;
        qteIcon.sprite = ControllerGlyphs.Face(inputButton, controller, out faceColor);
        if (!qteIcon.sprite) qteIcon.sprite = controller ? qteControllerIcon : qteKeyboardIcon;
        qteIcon.color = faceColor;
        ControllerGlyphs.StyleQte(qteIcon, inputButton, controller);
        qteIcon.enabled = !sequence && qteIcon.sprite != null;
        qteKey.text = qteIcon.enabled || sequence ? "" : controller ? "A" : "SPACE";
        bool largeStickPrompt = controller && inputButton >= QteButton.StickUp && !sequence;
        qtePrompt.sizeDelta = largeStickPrompt ? new Vector2(108, 96)
            : new Vector2(sequence ? Mathf.Min(root.rect.width - 24, qte.InputCount * 54) : ringPrompt ? 64 : 104, 48);
        tapRing.gameObject.SetActive(ringPrompt);
        tapRing.Progress = mash ? qte.TapProgress : hold ? qte.HoldProgress : 0;
        qteIcon.rectTransform.localScale = qte != null && qte.InputHeld ? Vector3.one * 0.88f : Vector3.one;
        triangleCue.gameObject.SetActive(ringPrompt);
        if (ringPrompt)
        {
            // Fit the circle to the displayed sprite's outer extent (preserveAspect may letterbox it).
            float aspect = qteIcon.sprite ? qteIcon.sprite.rect.width / qteIcon.sprite.rect.height : 1;
            float width = qtePrompt.sizeDelta.x - 24, height = qtePrompt.sizeDelta.y - 12;
            if (width / height > aspect) width = height * aspect; else height = width / aspect;
            tapRing.FitToIcon(Mathf.Max(width, height) * 0.5f);
            triangleCue.Present(hold, tapRing.OuterRadius, qte.Duration - qte.Remaining, !qte.Complete && qte.Result != PressQteResult.Preparing);
        }
        for (int i = 0; i < sequenceFrames.Count; i++) sequenceFrames[i].gameObject.SetActive(sequence && i < qte.InputCount);
        if (sequence)
        {
            float spacing = Mathf.Min(54, qtePrompt.sizeDelta.x / qte.InputCount);
            for (int i = 0; i < qte.InputCount; i++)
            {
                if (i == sequenceIcons.Count)
                {
                    var frame = Rect("Sequence input " + i, qtePrompt);
                    var frameImage = frame.gameObject.AddComponent<Image>(); frameImage.raycastTarget = false;
                    var progress = Rect("Step progress", frame); progress.anchorMin = new Vector2(0, 0); progress.anchorMax = new Vector2(1, 0);
                    progress.sizeDelta = new Vector2(0, 2); progress.anchoredPosition = new Vector2(0, -2);
                    progress.gameObject.AddComponent<Image>().raycastTarget = false;
                    var inner = Rect("Button icon", frame); Stretch(inner); inner.offsetMin = new Vector2(4, 4); inner.offsetMax = new Vector2(-4, -4);
                    var image = inner.gameObject.AddComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false;
                    sequenceFrames.Add(frameImage); sequenceIcons.Add(image);
                }
                var border = sequenceFrames[i]; border.gameObject.SetActive(true);
                border.rectTransform.sizeDelta = new Vector2(Mathf.Min(46, spacing - 2), 46);
                border.rectTransform.anchoredPosition = new Vector2((i - (qte.InputCount - 1) * 0.5f) * spacing, 0);
                border.color = Color.clear;
                border.transform.Find("Step progress").GetComponent<Image>().color = i < qte.CompletedInputs ? new Color(0.15f, 0.6f, 0.3f) : i == qte.CompletedInputs
                    ? qte.Complete ? new Color(0.7f, 0.12f, 0.12f) : new Color(0.8f, 0.6f, 0.12f) : Dark;
                sequenceIcons[i].sprite = ControllerGlyphs.Face(qte.InputAt(i), controller, out faceColor);
                sequenceIcons[i].color = faceColor;
                ControllerGlyphs.StyleQte(sequenceIcons[i], qte.InputAt(i), controller);
            }
        }
        dualPrompt.gameObject.SetActive(dual);
        if (dual)
        {
            var secondaryLane = !controller && qte.Secondary == QteButton.South ? QteLane.Bottom : QteLane.Right;
            dualPrompt.anchorMin = dualPrompt.anchorMax = QteLayout.Anchor(secondaryLane);
            dualPrompt.anchoredPosition = QteLayout.Offset(secondaryLane);
            dualIcon.sprite = ControllerGlyphs.Face(qte.Secondary, controller, out faceColor);
            dualIcon.color = faceColor; ControllerGlyphs.StyleQte(dualIcon, qte.Secondary, controller);
            dualIcon.rectTransform.localScale = qte.SecondaryHeld ? Vector3.one * 0.88f : Vector3.one;
        }
        qtePrompt.gameObject.SetActive(qte != null);
        if (qte == null)
        {
            qteCaption.text = battle.SignatureName; qteCountdown.text = "";
            qteFill.anchorMin = new Vector2(0.5f, 0); qteFill.anchorMax = new Vector2(0.5f, 1);
            return;
        }
        qteCaption.text = qte.Mode == QteMode.Staged && qte.Result == PressQteResult.StageFeedback
            ? $"{qte.StageIndex + 1} / {qte.InputCount}: {(qte.LastStageResult == PressQteResult.Success ? "Success" : qte.LastStageResult == PressQteResult.TimedOut ? "Time up" : "Wrong input")}"
            : qte.Mode == QteMode.Staged && qte.Result == PressQteResult.Waiting ? $"Prompt {qte.StageIndex + 1} / {qte.InputCount}"
            : qte.Result == PressQteResult.Preparing ? "Get ready"
            : qte.Result == PressQteResult.Waiting ? dual ? $"Hold both: {(qte.InputHeld ? 1 : 0) + (qte.SecondaryHeld ? 1 : 0)} / 2" : hold ? $"Hold: {qte.HeldTime:0.0} / {qte.HoldDuration:0.0}s" : mash ? $"Tap repeatedly: {qte.TapProgress:0%}" : sequence ? $"{qte.CompletedInputs} / {qte.InputCount} — press in order" : "Press before time runs out"
            : qte.Result == PressQteResult.Success ? "Success â€” bonus damage"
            : qte.Result == PressQteResult.WrongInput ? "Wrong input â€” normal damage" : "Time up â€” normal damage";
        qteCountdown.text = qte.Result == PressQteResult.Preparing ? "" : qte.Remaining.ToString("0.00") + "s";
        float half = qte.Fraction * 0.5f;
        qteFill.anchorMin = new Vector2(0.5f - half, 0); qteFill.anchorMax = new Vector2(0.5f + half, 1);
        qteFill.GetComponent<Image>().color = !qte.Complete ? new Color(1, 0.75f, 0.2f)
            : qte.Result == PressQteResult.Success ? new Color(0.3f, 1, 0.55f) : new Color(1, 0.3f, 0.25f);
        qtePrompt.GetComponent<Image>().color = Color.clear;
    }

    private void BuildDefense()
    {
        defensePanel = Rect("Reactive defense", root); defensePanel.anchorMin = defensePanel.anchorMax = new Vector2(0.5f, 0);
        defensePanel.pivot = new Vector2(0.5f, 0); defensePanel.anchoredPosition = new Vector2(0, 12); defensePanel.sizeDelta = new Vector2(340, 94);
        var back = defensePanel.gameObject.AddComponent<Image>(); back.color = new Color(0.02f, 0.03f, 0.04f, 0.82f); back.raycastTarget = false;
        var title = Rect("Attack", defensePanel); title.anchorMin = new Vector2(0, 1); title.anchorMax = Vector2.one;
        title.pivot = new Vector2(0.5f, 1); title.sizeDelta = new Vector2(0, 22);
        defenseCaption = Text(title, 13); defenseCaption.overflowMode = TextOverflowModes.Ellipsis;
        var track = Rect("Impact timing", defensePanel); track.anchorMin = new Vector2(0, 1); track.anchorMax = Vector2.one;
        track.pivot = new Vector2(0.5f, 1); track.sizeDelta = new Vector2(-20, 12); track.anchoredPosition = new Vector2(0, -25);
        track.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f); track.GetComponent<Image>().raycastTarget = false;
        dodgeWindow = Rect("Dodge window", track); Stretch(dodgeWindow);
        dodgeWindow.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.45f, 0.8f); dodgeWindow.GetComponent<Image>().raycastTarget = false;
        parryWindow = Rect("Parry window", track); Stretch(parryWindow);
        parryWindow.gameObject.AddComponent<Image>().color = new Color(1, 0.72f, 0.15f); parryWindow.GetComponent<Image>().raycastTarget = false;
        impactLine = Rect("Authored impact", track);
        impactLine.anchorMin = new Vector2(DefenseImpact.ImpactBarPosition, 0);
        impactLine.anchorMax = new Vector2(DefenseImpact.ImpactBarPosition, 1);
        impactLine.sizeDelta = new Vector2(2, 6);
        impactLine.gameObject.AddComponent<Image>().color = new Color(1, 0.45f, 0.15f);
        impactLine.GetComponent<Image>().raycastTarget = false;
        defenseMarker = Rect("Current time", track); defenseMarker.anchorMin = Vector2.zero; defenseMarker.anchorMax = new Vector2(0, 1);
        defenseMarker.sizeDelta = new Vector2(3, 4); defenseMarker.gameObject.AddComponent<Image>().raycastTarget = false;
        var prompt = Rect("Defense controls", defensePanel); prompt.anchorMin = Vector2.zero; prompt.anchorMax = new Vector2(1, 0);
        prompt.pivot = new Vector2(0.5f, 0); prompt.anchoredPosition = new Vector2(0, 27); prompt.sizeDelta = new Vector2(0, 22);
        defensePrompt = Text(prompt, 13);
        var result = Rect("Defense result", defensePanel); result.anchorMin = Vector2.zero; result.anchorMax = new Vector2(1, 0);
        result.pivot = new Vector2(0.5f, 0); result.sizeDelta = new Vector2(0, 26); defenseResult = Text(result, 14);
        defensePanel.gameObject.SetActive(false);
    }
    private void DrawDefense()
    {
        assistanceLabel.text = "Timing: " + (battle.DefenseSettings.Assisted ? "Assisted" : "Standard");
        assistanceLabel.GetComponentInParent<Button>().interactable = !battle.DefenseActive && !battle.SignatureActive;
        defensePanel.gameObject.SetActive(battle.DefenseActive);
        if (!battle.DefenseActive) return;
        if (inspectionLayer.gameObject.activeSelf) CloseInspection();
        defenseCaption.text = battle.DefenseCaption;
        bool controller = GameManager.Instance && GameManager.Instance.Input && GameManager.Instance.Input.CurrentScheme == PlayerInputReader.ControlScheme.Gamepad;
        defensePrompt.text = controller ? ControllerGlyphs.PlayStation ? "Circle: Dodge   |   R1: Parry" : "B: Dodge   |   RB: Parry" : "Space: Dodge   |   R: Parry";
        defenseResult.text = battle.DefenseFeedback;
        var hit = battle.CurrentImpact;
        if (hit == null) return;
        dodgeWindow.anchorMin = new Vector2(hit.BarPosition(hit.Duration - hit.DodgeWindow), 0);
        dodgeWindow.anchorMax = new Vector2(hit.ImpactPosition, 1);
        parryWindow.gameObject.SetActive(hit.Parryable);
        parryWindow.anchorMin = new Vector2(hit.BarPosition(hit.Duration - hit.ParryWindow), 0);
        parryWindow.anchorMax = new Vector2(hit.ImpactPosition, 1);
        impactLine.anchorMin = new Vector2(hit.ImpactPosition, 0);
        impactLine.anchorMax = new Vector2(hit.ImpactPosition, 1);
        dodgeWindow.GetComponent<Image>().color = hit.Resolved ? new Color(0.2f, 0.25f, 0.3f) : new Color(0.1f, 0.45f, 0.8f);
        parryWindow.GetComponent<Image>().color = hit.Resolved ? new Color(0.35f, 0.3f, 0.2f) : new Color(1, 0.72f, 0.15f);
        // Keep the registered press visible instead of letting the cursor run past it.
        float progress = hit.BarPosition(hit.DisplayTime);
        if (!string.IsNullOrEmpty(hit.AttemptFeedback)) defenseResult.text = hit.AttemptFeedback;
        bool accepted = hit.Result == DefenseResult.Parried || hit.Result == DefenseResult.Dodged;
        defenseMarker.GetComponent<Image>().color = !hit.Attempted ? Color.white
            : accepted ? new Color(0.3f, 1, 0.55f) : new Color(1, 0.25f, 0.2f);
        defenseMarker.anchorMin = new Vector2(progress, 0); defenseMarker.anchorMax = new Vector2(progress, 1);
    }
    private void BuildInspection()
    {
        inspectionLayer = Rect("Inspection modal", root); Stretch(inspectionLayer);
        var shade = inspectionLayer.gameObject.AddComponent<Image>(); shade.color = new Color(0, 0, 0, 0.35f);
        var dismiss = inspectionLayer.gameObject.AddComponent<Button>(); dismiss.targetGraphic = shade; dismiss.onClick.AddListener(CloseInspection);
        contextPanel = Rect("Character context menu", inspectionLayer); contextPanel.sizeDelta = new Vector2(140, 30);
        Button(contextPanel, "Examine", ControllerInspectSubmit);
        examinePanel = Rect("Examine character", inspectionLayer); examinePanel.gameObject.AddComponent<Image>().color = Dark;
        var panelBlocker = examinePanel.gameObject.AddComponent<Button>(); panelBlocker.targetGraphic = examinePanel.GetComponent<Image>();
        var panelNav = panelBlocker.navigation; panelNav.mode = Navigation.Mode.None; panelBlocker.navigation = panelNav;
        examinePanel.sizeDelta = new Vector2(340, Mathf.Min(430, Mathf.Max(200, root.rect.height - 60)));
        var close = Rect("Close examine", examinePanel); close.anchorMin = close.anchorMax = new Vector2(1, 1);
        close.pivot = Vector2.one; close.anchoredPosition = new Vector2(-8, -8); close.sizeDelta = new Vector2(80, 32); Button(close, "Close", CloseInspection);
        var viewport = Rect("Examine scroll", examinePanel); Stretch(viewport); viewport.offsetMin = new Vector2(18, 16); viewport.offsetMax = new Vector2(-18, -92);
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>(); var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
        examineContent = Rect("Character details", viewport); examineContent.anchorMin = new Vector2(0, 1); examineContent.anchorMax = Vector2.one;
        examineContent.pivot = new Vector2(0.5f, 1); scroll.content = examineContent;
        examineText = Text(examineContent, 15); examineText.alignment = TextAlignmentOptions.TopLeft;
        var statusRect=Rect("Examined status effects",examinePanel);statusRect.anchorMin=statusRect.anchorMax=new Vector2(.5f,1);statusRect.anchoredPosition=new Vector2(0,-65);statusRect.sizeDelta=new Vector2(300,36);
        examineStatuses=statusRect.gameObject.AddComponent<BattleStatusRow>();examineStatuses.Initialize(root,font);
        CloseInspection();
    }
    public void ShowContext(int index, Vector2 screenPosition)
    {
        if (!battle || battle.DefenseActive || battle.SignatureActive || index < 0 || index >= battle.Combatants.Count) return;
        ShowTargetContext(battle.Targetables[index], screenPosition);
    }
    public void ShowTargetContext(BattleTarget target, Vector2 screenPosition)
    {
        if (!battle || battle.ControllerInputLocked || target == null || !target.Alive) return;
        inspectedTarget = target; inspected = target.ActorIndex; battle.InspectionOpen = true; inspectionLayer.gameObject.SetActive(true);
        contextPanel.gameObject.SetActive(true); examinePanel.gameObject.SetActive(false);
        var canvas = GetComponentInParent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var p);
        contextPanel.anchoredPosition = new Vector2(Mathf.Clamp(p.x, root.rect.xMin + 70, root.rect.xMax - 70), Mathf.Clamp(p.y, root.rect.yMin + 15, root.rect.yMax - 15));
    }
    public void ShowExamine(int index)
    {
        inspected = index; inspectedTarget = battle.Targetables[index]; battle.InspectionOpen = true; inspectionLayer.gameObject.SetActive(true);
        contextPanel.gameObject.SetActive(false); examinePanel.gameObject.SetActive(true); UpdateExamine();
    }
    private void UpdateExamine()
    {
        if (inspectedTarget == null) return;
        examineStatuses.Show(inspectedTarget.StatusUnit, inspectedTarget.Unit == null);
        if (inspected >= 0)
        {
            var info = battle.Info(inspected);
            examineText.text = info.Details(battle.Combatants[inspected], battle.DisplayName(inspected));
        }
        else if (!inspectedTarget.HasHealth) examineText.text = $"<b>{inspectedTarget.Name}</b>\n\nInteractable object";
        else examineText.text = $"<b>{inspectedTarget.Name}</b>\nHealth  {inspectedTarget.Health} / {inspectedTarget.MaxHealth}\n\nDestructible object";
        examineContent.sizeDelta = new Vector2(0, examineText.GetPreferredValues(examineText.text, Mathf.Max(100, examinePanel.rect.width - 50), 0).y + 16);
    }
    public void ControllerInspectSubmit()
    {
        if (!battle.InspectionOpen || inspectedTarget == null) return;
        contextPanel.gameObject.SetActive(false); examinePanel.gameObject.SetActive(true); UpdateExamine();
    }
    public void ControllerInspectScroll(float direction, float seconds)
    {
        if (!examinePanel.gameObject.activeSelf) return;
        var scroll = examinePanel.GetComponentInChildren<ScrollRect>();
        scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + direction * seconds * BattleControllerNavigation.ScrollSpeed);
    }
    public void ControllerPortraitStep(int delta, bool activate)
    {
        var order = battle.UpcomingTurns();
        if (order.Count == 0) return;
        controllerPortrait = controllerPortrait < 0 ? 0 : (controllerPortrait + delta + order.Count) % order.Count;
        if (top) orderScroll.horizontalNormalizedPosition = order.Count <= 1 ? 0 : controllerPortrait / (float)(order.Count - 1);
        else orderScroll.verticalNormalizedPosition = order.Count <= 1 ? 1 : 1 - controllerPortrait / (float)(order.Count - 1);
        if (activate)
        {
            int index = order[controllerPortrait]; battle.FocusCharacter(index); battle.TargetFocus.Pin(battle.Targetables[index]);
        }
    }
    public void ClearControllerPortrait() { controllerPortrait = -1; }
    public void CloseInspection()
    { if (battle) { if (battle.InspectionOpen) InspectionDismissedFrame = Time.frameCount; battle.InspectionOpen = false; } if (inspectionLayer) inspectionLayer.gameObject.SetActive(false); }
    private void OnDisable() { CloseInspection(); if(overhead)overhead.gameObject.SetActive(false); }
    private void OnEnable() { if(overhead)overhead.gameObject.SetActive(true); }
    private void OnDestroy() { if(overhead)Remove(overhead.gameObject); if (battle) { battle.ContextRequested -= ShowContext; battle.TargetContextRequested -= ShowTargetContext; } }
    private void LateUpdate() => Render();
    public void Render()
    {
        if (!battle) return;
        if (battle.InspectionOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseInspection();
        if (battle.InspectionOpen && examinePanel.gameObject.activeSelf) UpdateExamine();
        DrawDefense(); DrawSignature(); DrawTargetDetails(); if(overhead)overhead.Render();
        var order = battle.UpcomingTurns();
        if (lastActor != battle.CurrentActorIndex || lastCount != order.Count)
        { ApplyLayout(); lastActor = battle.CurrentActorIndex; lastCount = order.Count; }
        for (int slot = 0; slot < cards.Count; slot++)
        {
            var card = cards[slot]; card.Rect.gameObject.SetActive(slot < order.Count); if (slot >= order.Count) continue;
            int i = order[slot]; card.Index = i; card.Portrait.texture = portraits[i];
            card.Portrait.color = portraits[i] ? Color.white : new Color(0.3f, 0.35f, 0.43f);
            var unit = battle.Combatants[i];
            card.HealthFill.anchorMax = new Vector2(Mathf.Clamp01(unit.Health / (float)Mathf.Max(1, unit.MaxHealth)), 1);
            UpdateShield(card.Shield, card.ShieldFill, card.ShieldText, unit);
            FitPortraitNumber(card.Health, card.Rect, .17f); FitPortraitNumber(card.ShieldText, card.Rect, .17f);
            card.Health.text = $"{unit.Health} / {unit.MaxHealth}";
            int hits = battle.AssignedHits(i); card.Hits.text = hits > 0 ? $"x{hits}" : "";
            card.Border.color = slot == controllerPortrait ? new Color(0.1f, 0.85f, 1) : slot == 0 ? new Color(1, 0.75f, 0.25f) : Dark;
        }
    }
}
