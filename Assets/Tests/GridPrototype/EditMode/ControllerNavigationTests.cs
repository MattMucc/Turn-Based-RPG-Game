using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Game.GridPrototype;
using Object = UnityEngine.Object;

public class ControllerNavigationTests : InputTestFixture
{
    private BattleActionTests fixture;
    private InSceneGridBattle battle;
    private GameObject root, events;
    private Component menu, nav, hud;
    private Type menuType, navType;
    private Gamepad pad;
    private EventSystem previousEvents;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public override void Setup()
    {
        base.Setup();
        fixture = new BattleActionTests(); fixture.Setup();
        battle = (InSceneGridBattle)typeof(BattleActionTests).GetField("battle", Private).GetValue(fixture);
        root = new GameObject("Controller test UI", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 32000;
        previousEvents = EventSystem.current;
        events = new GameObject("Controller test events", typeof(EventSystem));
        // EventSystem/BaseRaycaster do not execute their runtime OnEnable in EditMode.
        typeof(EventSystem).GetMethod("OnEnable", Private).Invoke(events.GetComponent<EventSystem>(), null);
        EventSystem.current = events.GetComponent<EventSystem>();
        typeof(GraphicRaycaster).GetMethod("OnEnable", Private).Invoke(root.GetComponent<GraphicRaycaster>(), null);
        menuType = Type.GetType("BattleMenuUI, Assembly-CSharp");
        navType = Type.GetType("BattleControllerNavigation, Assembly-CSharp");
        var buttonType = Type.GetType("BattleMenuButton, Assembly-CSharp");
        menu = root.AddComponent(menuType);
        var content = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
        content.transform.SetParent(root.transform, false);
        foreach (string field in new[] { "attackButton", "skillsButton", "itemsButton", "runButton" })
        {
            var row = new GameObject(field, typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(content.transform, false); row.GetComponent<Button>().targetGraphic = row.GetComponent<Image>();
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(row.transform, false);
            menuType.GetField(field, Private).SetValue(menu, row.AddComponent(buttonType));
        }
        menuType.GetMethod("Bind").Invoke(menu, new object[] { battle });
        nav = root.GetComponent(navType);
        hud = root.GetComponentInChildren(Type.GetType("BattleHud, Assembly-CSharp"));
        pad = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
        Canvas.ForceUpdateCanvases();
    }
    public override void TearDown()
    {
        if (root)
        {
            typeof(GraphicRaycaster).GetMethod("OnDisable", Private).Invoke(root.GetComponent<GraphicRaycaster>(), null);
            Object.DestroyImmediate(root);
        }
        if (events)
        {
            typeof(EventSystem).GetMethod("OnDisable", Private).Invoke(events.GetComponent<EventSystem>(), null);
            Object.DestroyImmediate(events);
        }
        if (previousEvents) EventSystem.current = previousEvents;
        fixture.Cleanup();
        base.TearDown();
    }
    private void Tick() => navType.GetMethod("ProcessController").Invoke(nav, new object[] { pad, 0.016f });
    private BattleNavigationState State => (BattleNavigationState)navType.GetProperty("State").GetValue(nav);
    private GridCameraController Camera => battle.BattleCamera.GetComponent<GridCameraController>();

    [Test] public void DualHudShowsBothLanesAndIndependentHeldFeedback()
    {
        battle.SetAutoCamera(false);
        var action = battle.Loadout.Options.Find(o => o.QteType == QteMode.DualInput);
        battle.ChooseAction(action); battle.ToggleTarget(battle.Combatants[1].Position); battle.TryPrepareAction(out var sequence);
        sequence.MoveNext(); battle.AdvanceFacing(1); if (battle.CurrentQte == null) sequence.MoveNext();
        battle.AdvanceSignature(0.5f); battle.AdvanceSignature(0.1f, true, secondaryHeld: true);
        hud.GetType().GetMethod("Render").Invoke(hud, null);
        var first = (RectTransform)hud.transform.Find("Signature QTE/QTE input");
        var second = (RectTransform)hud.transform.Find("Signature QTE/QTE second input");
        Assert.IsTrue(first.gameObject.activeSelf); Assert.IsTrue(second.gameObject.activeSelf);
        Assert.AreEqual(QteLayout.Anchor(QteLane.Left), first.anchorMin);
        Assert.AreEqual(QteLayout.Anchor(QteLane.Bottom), second.anchorMin);
        Assert.AreEqual(Vector3.one, first.Find("Input icon").localScale);
        Assert.AreEqual(0.88f, second.Find("Input icon").localScale.x, 0.001f);
        Assert.IsNotNull(second.Find("Input icon").GetComponent<Image>().sprite);
        battle.Finish(false); (sequence as IDisposable)?.Dispose();
    }

    [Test] public void StagedHudShowsOnePromptAndKeepsDirectionalLanesStable()
    {
        battle.SetAutoCamera(false);
        var action = battle.Loadout.Options.Find(o => o.QteType == QteMode.Staged);
        battle.ChooseAction(action); battle.ToggleTarget(battle.Combatants[1].Position); battle.TryPrepareAction(out var sequence);
        sequence.MoveNext(); battle.AdvanceFacing(1); if (battle.CurrentQte == null) sequence.MoveNext();
        battle.AdvanceSignature(0.5f); hud.GetType().GetMethod("Render").Invoke(hud, null);
        var prompt = (RectTransform)hud.transform.Find("Signature QTE/QTE input");
        Assert.AreEqual(QteLayout.Anchor(QteLane.Left), prompt.anchorMin);
        Assert.IsNotNull(prompt.Find("Input icon").GetComponent<Image>().sprite);
        Assert.IsNull(prompt.Find("Sequence input 0"));
        battle.AdvanceSignature(0, wrong: true); battle.AdvanceSignature(0.35f);
        hud.GetType().GetMethod("Render").Invoke(hud, null);
        Assert.AreEqual(QteLayout.Anchor(QteLane.Right), prompt.anchorMin);
        Assert.AreEqual(QteButton.East, battle.CurrentQte.Expected);
        battle.Finish(false); (sequence as IDisposable)?.Dispose();
    }

    [Test] public void FaceMaterialsSeparateDiscAndSymbolColorsForBothFamilies()
    {
        var glyphs = Type.GetType("ControllerGlyphs, Assembly-CSharp");
        var catalog = Resources.Load("ControllerGlyphs", glyphs);
        var pairs = (Array)glyphs.GetField("Buttons").GetValue(catalog);
        var pair = pairs.GetValue(2);
        var obj = new GameObject("Face material test", typeof(RectTransform), typeof(Image));
        try
        {
            var image = obj.GetComponent<Image>();
            image.sprite = (Sprite)pair.GetType().GetField("Xbox").GetValue(pair);
            glyphs.GetMethod("StyleFace").Invoke(null, new object[] { image });
            Assert.AreEqual("Game/ControllerFace", image.material.shader.name);
            Assert.IsTrue(image.material.shader.isSupported);
            Assert.Less(image.material.GetColor("_FaceColor").grayscale, 0.05f);
            Assert.Greater(image.material.GetColor("_SymbolColor").b, 0.9f);
            image.sprite = (Sprite)pair.GetType().GetField("PlayStation").GetValue(pair);
            glyphs.GetMethod("StyleFace").Invoke(null, new object[] { image });
            Assert.AreEqual(Color.white, image.material.GetColor("_FaceColor"));
            Assert.Less(image.material.GetColor("_SymbolColor").grayscale, 0.05f);
            Assert.AreEqual(Color.white, image.color, "Row tints must not recolor the entire two-color face icon.");
        }
        finally { Object.DestroyImmediate(obj); }
    }

    [Test] public void TriggerFocusLayerIsBehindCommandsAndCannotBlockWorldInput()
    {
        var dimmer = root.transform.Find("Shortcut focus dimmer").GetComponent<Image>();
        Assert.IsFalse(dimmer.raycastTarget);
        Assert.Less(dimmer.transform.GetSiblingIndex(), root.transform.Find("Battle command viewport").GetSiblingIndex());
        Assert.Greater(dimmer.transform.GetSiblingIndex(), hud.transform.GetSiblingIndex());
        Press(pad.leftTrigger); Tick();
        Assert.AreEqual(true, menuType.GetMethod("ShouldShowShortcutFocus").Invoke(menu, new object[] { true }));
        Assert.AreEqual(false, menuType.GetMethod("ShouldShowShortcutFocus").Invoke(menu, new object[] { false }));
        Release(pad.leftTrigger); Tick();
        Assert.AreEqual(false, menuType.GetMethod("ShouldShowShortcutFocus").Invoke(menu, new object[] { true }));
        Press(pad.leftTrigger); Tick(); battle.Block();
        Assert.AreEqual(false, menuType.GetMethod("ShouldShowShortcutFocus").Invoke(menu, new object[] { true }));
    }
    [Test] public void ShortcutUsesSeparateTriggerGlyphAndNeutralIconsContrastWithButtons()
    {
        var button = (Component)menuType.GetField("attackButton", Private).GetValue(menu);
        var type = button.GetType();
        var obj = new GameObject("Icon", typeof(RectTransform), typeof(Image)); obj.transform.SetParent(button.transform, false);
        var icon = obj.GetComponent<Image>(); type.GetField("icon", Private).SetValue(button, icon);
        var glyphType = Type.GetType("ControllerGlyphs, Assembly-CSharp");
        var catalog = Resources.Load("ControllerGlyphs", glyphType);
        var pairs = (Array)glyphType.GetField("Buttons").GetValue(catalog);
        var rb = (Sprite)pairs.GetValue(7).GetType().GetField("Xbox").GetValue(pairs.GetValue(7));
        type.GetMethod("SetIcons").Invoke(button, new object[] { null, rb, true });
        var schemeType = Type.GetType("PlayerInputReader+ControlScheme, Assembly-CSharp");
        type.GetMethod("UpdateIcon", Private).Invoke(button, new[] { Enum.Parse(schemeType, "Gamepad") });
        Assert.AreEqual("Attack", ((TMP_Text)type.GetProperty("Label").GetValue(button)).text);
        Assert.Less(icon.color.grayscale, 0.2f);
        var trigger = button.transform.Find("Shortcut trigger").GetComponent<Image>();
        Assert.IsTrue(trigger.gameObject.activeSelf); Assert.IsNotNull(trigger.sprite);
        Assert.Less(trigger.color.grayscale, 0.2f);
        Assert.AreEqual(16.5f, trigger.GetComponent<LayoutElement>().preferredWidth);
        Assert.Less(trigger.transform.GetSiblingIndex(), icon.transform.GetSiblingIndex());
        type.GetMethod("UpdateIcon", Private).Invoke(button, new[] { Enum.Parse(schemeType, "Keyboard") });
        Assert.IsFalse(trigger.gameObject.activeSelf); Assert.IsFalse(button.transform.Find("Shortcut plus").gameObject.activeSelf);
    }

    [TestCase(QteMode.SinglePress)]
    [TestCase(QteMode.FixedSequence)]
    [TestCase(QteMode.RepeatedTap)]
    [TestCase(QteMode.Hold)]
    public void QteHudUsesSpritesAndShowsRingOnlyForRepeatedTaps(QteMode mode)
    {
        battle.SetAutoCamera(false);
        var action = battle.Loadout.Options.Find(o => o.SignatureQte && o.QteType == mode);
        battle.ChooseAction(action); battle.ToggleTarget(battle.Combatants[1].Position);
        Assert.IsTrue(battle.TryPrepareAction(out var sequence));
        sequence.MoveNext(); battle.AdvanceFacing(1);
        if (battle.CurrentQte == null) sequence.MoveNext();
        battle.AdvanceSignature(0.5f);
        battle.AdvanceSignature(0.1f, true, held: true);
        hud.GetType().GetMethod("Render").Invoke(hud, null);
        var prompt = hud.transform.Find("Signature QTE/QTE input");
        Assert.IsNotNull(prompt);
        Assert.AreEqual(0, prompt.GetComponent<Image>().color.a);
        var ring = prompt.Find("Tap progress ring");
        Assert.AreEqual(mode == QteMode.RepeatedTap || mode == QteMode.Hold, ring.gameObject.activeSelf);
        if (mode == QteMode.FixedSequence)
        {
            for (int n = 0; n < 4; n++)
            {
                Assert.IsNotNull(prompt.Find("Sequence input " + n + "/Button icon").GetComponent<Image>().sprite);
                Assert.AreEqual(0, prompt.Find("Sequence input " + n).GetComponent<Image>().color.a);
            }
            Assert.AreEqual(new Vector2(0.5f, 0), ((RectTransform)prompt).anchorMin);
        }
        else Assert.IsNotNull(prompt.Find("Input icon").GetComponent<Image>().sprite);
        if (mode == QteMode.RepeatedTap || mode == QteMode.Hold)
        {
            var icon = prompt.Find("Input icon");
            Assert.AreEqual(0.88f, icon.localScale.x, 0.001f);
            var cue = prompt.Find("QTE triangle cue"); Assert.IsTrue(cue.gameObject.activeSelf);
            Assert.AreEqual(mode == QteMode.Hold, cue.Find("Filled triangle").gameObject.activeSelf);
            Assert.AreEqual(mode != QteMode.Hold, cue.Find("Outlined triangle").gameObject.activeSelf);
            var ringType = Type.GetType("QteProgressRing, Assembly-CSharp");
            float outer = (float)ringType.GetProperty("OuterRadius").GetValue(ring.GetComponent(ringType));
            Assert.AreEqual(25, outer, 0.01f, "The square Space glyph is height-limited to 36px; ring inner radius is 18px.");
            if (mode == QteMode.Hold) Assert.AreEqual(outer, ((RectTransform)cue).anchoredPosition.y, 0.001f);
            battle.AdvanceSignature(0, held: false); hud.GetType().GetMethod("Render").Invoke(hud, null);
            Assert.AreEqual(Vector3.one, icon.localScale);
        }
        if (mode == QteMode.RepeatedTap)
        {
            var type = Type.GetType("QteProgressRing, Assembly-CSharp");
            Assert.AreEqual(1f / action.QteRequiredTaps, (float)type.GetProperty("Progress").GetValue(ring.GetComponent(type)), 0.001f);
            int lit = 0;
            foreach (var image in ring.GetComponentsInChildren<Image>())
                if (image.name.StartsWith("Fill ") && image.enabled) { lit++; Assert.IsNotNull(image.GetComponent<CanvasRenderer>()); }
            Assert.AreEqual(8, lit, "One of twelve taps must visibly fill eight of 96 arc segments.");
            type.GetProperty("Progress").SetValue(ring.GetComponent(type), 1f);
            lit = 0;
            foreach (var image in ring.GetComponentsInChildren<Image>()) if (image.name.StartsWith("Fill ") && image.enabled) lit++;
            Assert.AreEqual(96, lit);
            type.GetProperty("Progress").SetValue(ring.GetComponent(type), 0f);
            foreach (var image in ring.GetComponentsInChildren<Image>()) if (image.name.StartsWith("Fill ")) Assert.IsFalse(image.enabled);
        }
        battle.Finish(false); (sequence as IDisposable)?.Dispose();
    }

    [Test] public void RootTriggerShortcutsOpenCategoriesAndFavoritesNeverAlsoClickWorld()
    {
        int movement = battle.Movement;
        Press(pad.leftTrigger); Press(pad.buttonWest); Tick();
        Assert.IsTrue((bool)menuType.GetProperty("ControllerBrowsing").GetValue(menu));
        Assert.IsNull(battle.ChosenAction);
        Assert.IsTrue(battle.ActionReady);
        Release(pad.buttonWest); Release(pad.leftTrigger);
        Press(pad.buttonEast); Tick(); Release(pad.buttonEast);
        Assert.IsFalse((bool)menuType.GetProperty("ControllerBrowsing").GetValue(menu));
        Press(pad.leftTrigger); Press(pad.buttonNorth); Tick();
        Assert.IsTrue((bool)menuType.GetProperty("ControllerBrowsing").GetValue(menu));
        Assert.IsNull(battle.ChosenAction, "Opening a category must not also pick its favorite.");
        Release(pad.buttonNorth); Release(pad.leftTrigger);
        Press(pad.buttonEast); Tick(); Release(pad.buttonEast);
        Press(pad.leftTrigger); Press(pad.buttonSouth); Tick();
        Assert.IsTrue((bool)menuType.GetProperty("ControllerBrowsing").GetValue(menu));
        Assert.IsNull(battle.ChosenAction);
        Assert.AreEqual(movement, battle.Movement);
        Assert.IsTrue(battle.ActionReady);
        Release(pad.buttonSouth); Release(pad.leftTrigger);
        Press(pad.buttonEast); Tick(); Release(pad.buttonEast);
        Press(pad.leftTrigger); Press(pad.rightShoulder); Tick();
        Assert.IsFalse(battle.ActionReady, "LT + RB must execute Block from root.");
    }

    [Test] public void ViewTogglesFlightAndFocusedOrbitReleasesOnMovement()
    {
        Press(pad.selectButton); Tick(); Assert.AreEqual(BattleControllerMode.FreeFlight, State.Mode);
        Assert.IsFalse(events.GetComponent<EventSystem>().sendNavigationEvents);
        Release(pad.selectButton);
        Press(pad.leftStickButton); Tick(); Assert.AreEqual(battle.Actor(0), Camera.FocusTarget);
        Release(pad.leftStickButton); Camera.AdvanceFocus(1);
        float radius = Vector3.Distance(Camera.transform.position, battle.Actor(0).position);
        Set(pad.rightStick, Vector2.right); Tick();
        Assert.IsTrue(Camera.CharacterFocused);
        Assert.AreEqual(radius, Vector3.Distance(Camera.transform.position, battle.Actor(0).position), 0.01f);
        Set(pad.leftStick, Vector2.up); Tick(); Assert.IsFalse(Camera.CharacterFocused);
        Set(pad.leftStick, Vector2.zero); Set(pad.rightStick, Vector2.zero);
        Press(pad.selectButton); Tick(); Assert.AreEqual(BattleControllerMode.Cursor, State.Mode);
        Release(pad.selectButton); Press(pad.startButton); Tick();
        Assert.AreEqual(BattleControllerMode.Cursor, State.Mode, "Start is reserved for menus.");
    }

    [Test] public void DpadAreasAndContextExamineDoNotSpendActionOrTriggerRun()
    {
        Press(pad.leftTrigger); Press(pad.dpad.up); Tick();
        Assert.AreEqual(BattleControllerArea.TurnOrder, State.Area);
        Release(pad.dpad.up); Release(pad.leftTrigger); Tick();
        Press(pad.dpad.right); Tick(); Release(pad.dpad.right);
        Press(pad.buttonSouth); Tick(); Release(pad.buttonSouth);
        Assert.AreEqual(battle.Actor(1), Camera.FocusTarget);
        Press(pad.leftTrigger); Press(pad.dpad.right); Tick();
        Release(pad.dpad.right); Release(pad.leftTrigger); Tick();
        Assert.AreEqual(BattleControllerArea.Battlefield, State.Area);
        Press(pad.dpad.right); Tick(); Release(pad.dpad.right);
        Press(pad.buttonSouth); Tick(); Release(pad.buttonSouth);
        Assert.IsTrue(battle.InspectionOpen);
        Press(pad.buttonSouth); Tick(); Release(pad.buttonSouth);
        Assert.IsTrue(hud.transform.Find("Inspection modal/Examine character").gameObject.activeSelf);
        Press(pad.buttonEast); Tick(); Release(pad.buttonEast);
        Assert.IsFalse(battle.InspectionOpen);
        Assert.IsTrue(battle.ActionReady); Assert.AreEqual(3, battle.Movement);
        Assert.IsTrue(root.activeSelf);
    }

    [Test] public void CursorActivationRoutesToOneUiButtonAndCameraModifierDoesNotClick()
    {
        Press(pad.selectButton); Tick(); Release(pad.selectButton);
        Press(pad.selectButton); Tick(); Release(pad.selectButton);
        var button = Array.Find(root.GetComponentsInChildren<Button>(), b => b.GetComponentInChildren<TMP_Text>()?.text == "Skills");
        Canvas.ForceUpdateCanvases();
        Vector2 position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
        navType.GetProperty("Pointer").SetValue(nav, position);
        Press(pad.rightTrigger); Press(pad.buttonSouth); Tick();
        Assert.AreEqual(false, menuType.GetProperty("ControllerBrowsing").GetValue(menu));
        Release(pad.rightTrigger); Release(pad.buttonSouth);
        // EditMode canvases have no rendered Graphic depth; exercise the same dispatch
        // boundary used after the runtime graphic raycast selects this button.
        navType.GetMethod("ClickUI", Private).Invoke(nav, new object[] { button.gameObject });
        Assert.AreEqual(true, menuType.GetProperty("ControllerBrowsing").GetValue(menu));
        Assert.IsTrue(battle.ActionReady); Assert.IsNull(battle.ChosenAction);
        Press(pad.dpad.down); Tick();
        Assert.AreEqual(BattleControllerMode.Navigation, State.Mode);
        Release(pad.dpad.down);
        Press(pad.buttonSouth); Tick(); Release(pad.buttonSouth); Tick();
        Assert.IsNotNull(battle.ChosenAction);
        Assert.AreEqual(BattleControllerArea.Battlefield, State.Area);
    }

    [Test] public void QteOwnsSticksAndModeToggleWithoutCameraOrNavigationSideEffects()
    {
        Press(pad.selectButton); Tick(); Release(pad.selectButton);
        var action = battle.Loadout.Options.Find(o => o.SignatureQte);
        battle.ChooseAction(action); battle.ToggleTarget(new Vector2Int(4, 3));
        Assert.IsTrue(battle.TryPrepareAction(out var sequence));
        var position = Camera.transform.position; var rotation = Camera.transform.rotation;
        Set(pad.leftStick, Vector2.one); Set(pad.rightStick, Vector2.one);
        Press(pad.selectButton); Tick();
        Assert.AreEqual(BattleControllerMode.FreeFlight, State.Mode);
        Assert.AreEqual(position, Camera.transform.position); Assert.AreEqual(rotation, Camera.transform.rotation);
        Assert.IsFalse(root.transform.Find("Controller cursor").gameObject.activeSelf);
        battle.Finish(false); ((IDisposable)sequence).Dispose();
    }

    [Test] public void DestructibleBrowsingOpensExamineWithoutSpendingAction()
    {
        var propObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            propObject.transform.position = battle.Grid.Point(new Vector2Int(2, 3)) + Vector3.up * 0.5f;
            var prop = propObject.AddComponent<BattleDestructible>(); prop.DisplayName = "Test crate";
            Physics.SyncTransforms(); battle.RegisterDestructible(prop);
            Vector2 point = battle.BattleCamera.WorldToScreenPoint(propObject.transform.position);
            battle.ControllerPoint(point, false, false);
            Assert.AreEqual("Test crate", battle.TargetFocus.Hovered.Name);
            battle.ControllerPoint(point, false, true);
            Assert.IsTrue(battle.InspectionOpen); Assert.IsTrue(battle.ActionReady);
            hud.GetType().GetMethod("ControllerInspectSubmit").Invoke(hud, null);
            Assert.IsTrue(Array.Exists(root.GetComponentsInChildren<TMP_Text>(), t => t.text.Contains("Destructible object")));
            hud.GetType().GetMethod("CloseInspection").Invoke(hud, null);
            Assert.IsFalse(battle.InspectionOpen); Assert.IsTrue(battle.ActionReady);
        }
        finally { Object.DestroyImmediate(propObject); Physics.SyncTransforms(); }
    }

    [Test] public void UnbindRestoresEventNavigationAndReleasesWorldInputOwnership()
    {
        Press(pad.selectButton); Tick();
        Assert.IsTrue(battle.ExternalControllerActive); Assert.IsTrue(Camera.ControllerOwnsInput);
        menuType.GetMethod("Unbind").Invoke(menu, null);
        Assert.IsFalse(battle.ExternalControllerActive); Assert.IsFalse(Camera.ControllerOwnsInput);
        Assert.IsTrue(events.GetComponent<EventSystem>().sendNavigationEvents);
    }

    [Test] public void DriftAndFixedQteLanesAreStable()
    {
        Assert.AreEqual(Vector2.zero, BattleNavigationState.FilterStick(new Vector2(0.1f, 0.1f)));
        Assert.AreEqual(Vector2.up, BattleNavigationState.FilterStick(Vector2.up));
        Assert.AreEqual(0.45f, QteLayout.Anchor(QteLane.Left).y);
        Assert.AreEqual(QteLayout.Anchor(QteLane.Left).y, QteLayout.Anchor(QteLane.Right).y);
        Assert.AreEqual(0.5f, QteLayout.Anchor(QteLane.Bottom).x);
    }

    [Test] public void RightStickScrollsFrameInBothAxesAndConsumesAtEndpoint()
    {
        var frame = new GameObject("Scroll window", typeof(RectTransform), typeof(ScrollRect));
        var body = new GameObject("Scroll content", typeof(RectTransform));
        try
        {
            body.transform.SetParent(frame.transform, false);
            var scroll = frame.GetComponent<ScrollRect>(); scroll.content = (RectTransform)body.transform;
            ((RectTransform)frame.transform).sizeDelta = new Vector2(100, 100); scroll.content.sizeDelta = new Vector2(500, 500);
            scroll.horizontal = scroll.vertical = true;
            scroll.horizontalNormalizedPosition = scroll.verticalNormalizedPosition = 0.5f;
            var method = navType.GetMethod("ScrollHovered");
            Assert.AreEqual(true, method.Invoke(null, new object[] { body, Vector2.one, 0.1f }));
            Assert.AreEqual(0.8f, scroll.verticalNormalizedPosition, 0.001f); Assert.AreEqual(0.8f, scroll.horizontalNormalizedPosition, 0.001f);
            Assert.AreEqual(true, method.Invoke(null, new object[] { body, Vector2.one, 5f }));
            Assert.AreEqual(1, scroll.verticalNormalizedPosition, 0.001f);
        }
        finally { Object.DestroyImmediate(frame); }
    }
}
