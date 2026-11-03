using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.GridPrototype;

public class PressQteTests : InputTestFixture
{
    [TestCase(true)] [TestCase(false)]
    public void DualInputsCanBeHeldInEitherOrderButMustOverlap(bool directionFirst)
    {
        var qte = new PressQte(3, 0, mode: QteMode.DualInput);
        qte.Advance(0.1f, true, held: directionFirst, secondaryHeld: !directionFirst);
        Assert.IsFalse(qte.Complete);
        qte.Advance(0.1f, true, held: !directionFirst, secondaryHeld: directionFirst);
        Assert.IsFalse(qte.Complete, "Previously held half is not latched after release.");
        qte.Advance(0, true, held: true, secondaryHeld: true);
        Assert.AreEqual(PressQteResult.Success, qte.Result);
    }
    [Test] public void DualPreparationConflictAndTimeoutCannotAutoComplete()
    {
        var qte = new PressQte(1, 0.5f, mode: QteMode.DualInput);
        qte.Advance(0.5f, true, held: true, secondaryHeld: true);
        qte.Advance(0.1f, held: true, secondaryHeld: true); Assert.IsFalse(qte.Complete);
        qte.Advance(0, true, true, true, true); Assert.AreEqual(PressQteResult.WrongInput, qte.Result);
        var timeout = new PressQte(1, 0, mode: QteMode.DualInput);
        timeout.Advance(2, true, held: true); Assert.AreEqual(PressQteResult.TimedOut, timeout.Result);
        timeout.Advance(0, true, held: true, secondaryHeld: true); Assert.AreEqual(PressQteResult.TimedOut, timeout.Result);
    }
    [TestCase(true)] [TestCase(false)]
    public void DualInputReaderAllowsEitherHalfAndRejectsWrongDirection(bool faceFirst)
    {
        var pad = InputSystem.AddDevice<Gamepad>(); var input = new PressQteInput(); input.Reset(pad);
        if (faceFirst) Press(pad.buttonSouth); else Set(pad.leftStick, Vector2.right);
        input.Read(null, pad, out bool correct, out bool wrong, QteButton.StickRight, QteButton.South);
        Assert.IsTrue(correct); Assert.IsFalse(wrong);
        if (faceFirst) Set(pad.leftStick, Vector2.right); else Press(pad.buttonSouth);
        input.Read(null, pad, out correct, out wrong, QteButton.StickRight, QteButton.South);
        Assert.IsTrue(correct); Assert.IsFalse(wrong);
        Assert.IsTrue(PressQteInput.IsHeld(null, pad, QteButton.StickRight));
        Assert.IsTrue(PressQteInput.IsHeld(null, pad, QteButton.South));
        Set(pad.leftStick, Vector2.one);
        input.Read(null, pad, out correct, out wrong, QteButton.StickRight, QteButton.South); Assert.IsTrue(wrong);
    }
    [Test] public void DualKeyboardAllowsSpaceFirstThenDirectionAndRejectsExtraKey()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>(); var input = new PressQteInput();
        Press(keyboard.spaceKey); input.Read(keyboard, null, out bool correct, out bool wrong, QteButton.StickRight, QteButton.South);
        Assert.IsTrue(correct); Assert.IsFalse(wrong);
        Press(keyboard.dKey); input.Read(keyboard, null, out correct, out wrong, QteButton.StickRight, QteButton.South);
        Assert.IsTrue(correct); Assert.IsFalse(wrong);
        Assert.IsTrue(PressQteInput.IsHeld(keyboard, null, QteButton.StickRight));
        Assert.IsTrue(PressQteInput.IsHeld(keyboard, null, QteButton.South));
        Press(keyboard.wKey); input.Read(keyboard, null, out correct, out wrong, QteButton.StickRight, QteButton.South);
        Assert.IsTrue(wrong);
    }

    [Test] public void TapDecayHasGraceAndResumeUsesRemainingChargeInsteadOfLifetimeTapCount()
    {
        var qte = new PressQte(20, 0, mode: QteMode.RepeatedTap, requiredTaps: 4, progressDrainPerSecond: 0.2f, tapDrainDelay: 0.3f);
        qte.Advance(0, true); qte.Advance(0, true); Assert.AreEqual(0.5f, qte.TapProgress);
        qte.Advance(0.3f); Assert.AreEqual(0.5f, qte.TapProgress);
        qte.Advance(1, held: true); Assert.AreEqual(0.3f, qte.TapProgress, 0.0001f, "Holding cannot prevent tap decay.");
        qte.Advance(0, true); qte.Advance(0, true);
        Assert.AreEqual(4, qte.CompletedInputs); Assert.IsFalse(qte.Complete);
        Assert.AreEqual(0.8f, qte.TapProgress, 0.0001f);
        qte.Advance(0, true); Assert.AreEqual(PressQteResult.Success, qte.Result);
        qte.Advance(10); Assert.AreEqual(1, qte.TapProgress, "Resolved results stay frozen.");
    }
    [Test] public void HoldDecayResumesWithoutResetAndClampsToZero()
    {
        var qte = new PressQte(20, 0, mode: QteMode.Hold, holdDuration: 2, progressDrainPerSecond: 0.2f);
        qte.Advance(1, true, held: true); Assert.AreEqual(0.5f, qte.HoldProgress);
        qte.Advance(0); Assert.AreEqual(0.5f, qte.HoldProgress);
        qte.Advance(1); Assert.AreEqual(0.3f, qte.HoldProgress, 0.0001f);
        qte.Advance(0.5f, true, held: true); Assert.AreEqual(0.55f, qte.HoldProgress, 0.0001f);
        qte.Advance(5); Assert.AreEqual(0, qte.HoldProgress);
    }
    [Test] public void TapDecayCrossingGraceIsFrameIndependentAndStopsAtDeadline()
    {
        var large = new PressQte(2, 0, mode: QteMode.RepeatedTap, requiredTaps: 2);
        var small = new PressQte(2, 0, mode: QteMode.RepeatedTap, requiredTaps: 2);
        large.Advance(0, true); small.Advance(0, true);
        large.Advance(1); for (int i = 0; i < 10; i++) small.Advance(0.1f);
        Assert.AreEqual(large.TapProgress, small.TapProgress, 0.0001f);
        large.Advance(100); Assert.AreEqual(0.16f, large.TapProgress, 0.0001f);
        Assert.AreEqual(PressQteResult.TimedOut, large.Result);
    }

    [Test] public void StagedMissAndTimeoutAdvanceWithoutSkippingTheNextInput()
    {
        var qte = new PressQte(2, 0, new[] { QteButton.StickRight, QteButton.East, QteButton.South }, QteMode.Staged);
        qte.Advance(0, wrongPress: true); Assert.IsFalse(qte.Complete); Assert.AreEqual(PressQteResult.StageFeedback, qte.Result);
        qte.Advance(0.35f, true); Assert.AreEqual(1, qte.StageIndex); Assert.AreEqual(2, qte.Remaining);
        Assert.AreEqual(0, qte.SuccessfulStages, "Transition cannot reuse input.");
        qte.Advance(3); Assert.AreEqual(PressQteResult.TimedOut, qte.LastStageResult); Assert.IsFalse(qte.Complete);
        qte.Advance(0.35f); qte.Advance(0, true);
        Assert.IsTrue(qte.Complete); Assert.AreNotEqual(PressQteResult.Success, qte.Result);
        Assert.AreEqual(1, qte.SuccessfulStages);
    }
    [Test] public void SequencesCapAtEightAndConvertStickDirectionsToDpad()
    {
        var inputs = new QteButton[10]; for (int n = 0; n < inputs.Length; n++) inputs[n] = QteButton.StickLeft;
        var qte = new PressQte(4, 0, inputs, QteMode.FixedSequence);
        Assert.AreEqual(8, qte.InputCount); Assert.AreEqual(QteButton.DpadLeft, qte.Expected);
        Assert.AreEqual(QteButton.StickLeft, inputs[0], "Runtime normalization must not mutate authored input.");
    }
    [Test] public void DpadAndFaceShareKeyboardArrowsButControllerBindingsStayDistinct()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>(); var pad = InputSystem.AddDevice<Gamepad>();
        var reader = new PressQteInput(); reader.Reset(pad);
        Press(keyboard.upArrowKey); reader.Read(keyboard, pad, out bool correct, out bool wrong, QteButton.DpadUp);
        Assert.IsTrue(correct); Assert.IsFalse(wrong);
        reader.Read(keyboard, pad, out correct, out wrong, QteButton.North); Assert.IsTrue(correct); Assert.IsFalse(wrong);
        Release(keyboard.upArrowKey); Press(pad.buttonNorth);
        reader.Read(keyboard, pad, out correct, out wrong, QteButton.DpadUp); Assert.IsFalse(correct); Assert.IsTrue(wrong);
        Release(pad.buttonNorth); Set(pad.leftStick, Vector2.right);
        reader.Read(keyboard, pad, out correct, out wrong, QteButton.StickRight); Assert.IsTrue(correct); Assert.IsFalse(wrong);
        InputSystem.Update(); reader.Read(keyboard, pad, out correct, out wrong, QteButton.StickRight); Assert.IsFalse(correct);
        Set(pad.leftStick, Vector2.zero); reader.Read(keyboard, pad, out correct, out wrong, QteButton.StickRight);
        Set(pad.leftStick, Vector2.one); reader.Read(keyboard, pad, out correct, out wrong, QteButton.StickRight); Assert.IsTrue(wrong);
    }

    [Test] public void HoldNeedsFreshStartContinuousInputAndFitsInsideDeadline()
    {
        var qte = new PressQte(4, 0.5f, mode: QteMode.Hold, holdDuration: 2);
        qte.Advance(0.5f, true, held: true); qte.Advance(0.5f, held: true);
        Assert.AreEqual(0, qte.HeldTime, "Pre-held launch input cannot charge.");
        qte.Advance(0, held: false); qte.Advance(0.5f, true, held: true);
        Assert.AreEqual(0.25f, qte.HoldProgress); Assert.IsTrue(qte.InputHeld);
        qte.Advance(0, held: false); Assert.AreEqual(0.5f, qte.HeldTime, "Release must not instantly clear progress."); Assert.IsFalse(qte.InputHeld);
        qte.Advance(1, true, held: true); qte.Advance(1, held: true);
        Assert.AreEqual(PressQteResult.Success, qte.Result);
        var timeout = new PressQte(1, 0, mode: QteMode.Hold, holdDuration: 2);
        timeout.Advance(3, true, held: true);
        Assert.AreEqual(PressQteResult.TimedOut, timeout.Result); Assert.AreEqual(1, timeout.HeldTime);
    }

    [Test] public void ExplicitSingleIgnoresStaleSequenceAndMashRequiresEveryFreshTap()
    {
        var single = new PressQte(2, 0, new[] { QteButton.West, QteButton.North }, QteMode.SinglePress);
        Assert.AreEqual(1, single.InputCount); Assert.AreEqual(QteButton.South, single.Expected);
        single.Advance(0, true); Assert.AreEqual(PressQteResult.Success, single.Result);
        var mash = new PressQte(4, 0, mode: QteMode.RepeatedTap, requiredTaps: 3);
        mash.Advance(0.1f, true); Assert.AreEqual(1f / 3, mash.TapProgress, 0.001f);
        mash.Advance(0.1f); Assert.AreEqual(1, mash.CompletedInputs);
        mash.Advance(0.1f, true); Assert.IsFalse(mash.Complete);
        mash.Advance(0.1f, true); Assert.AreEqual(PressQteResult.Success, mash.Result); Assert.AreEqual(1, mash.TapProgress);
        var timeout = new PressQte(1, 0, mode: QteMode.RepeatedTap, requiredTaps: 3);
        timeout.Advance(0.1f, true); timeout.Advance(2);
        Assert.AreEqual(PressQteResult.TimedOut, timeout.Result);
    }
    [Test] public void SavedPrefabSeparatesAllThreeQteTypesAndGlyphCatalogIsComplete()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath("33fc2d0129d8dfe4c91a60c0f8d9d7bf"));
        Assert.IsNotNull(prefab, "The balanced character prefab must remain available for QTE validation.");
        var loadout = prefab.GetComponent<BattleActionLoadout>();
        var single = loadout.Options.Find(o => o.Name == "Signature Strike");
        Assert.AreEqual(QteMode.SinglePress, single.QteType); Assert.IsEmpty(single.QteSequence);
        Assert.AreEqual(QteMode.FixedSequence, loadout.Options.Find(o => o.Name == "Sequence Strike").QteType);
        Assert.AreEqual(QteMode.RepeatedTap, loadout.Options.Find(o => o.Name == "Burst Strike").QteType);
        var type = System.Type.GetType("ControllerGlyphs, Assembly-CSharp");
        var catalog = Resources.Load("ControllerGlyphs", type);
        Assert.IsNotNull(catalog);
        var pairs = (System.Array)type.GetField("Buttons").GetValue(catalog);
        Assert.AreEqual(10, pairs.Length);
        foreach (var pair in pairs)
        {
            Assert.IsNotNull(pair.GetType().GetField("Xbox").GetValue(pair));
            Assert.IsNotNull(pair.GetType().GetField("PlayStation").GetValue(pair));
        }
        foreach (var sprite in (Sprite[])type.GetField("KeyboardFaces").GetValue(catalog)) Assert.IsNotNull(sprite);
        var sony = InputSystem.AddDevice<UnityEngine.InputSystem.DualShock.DualShockGamepad>();
        Assert.AreEqual(true, type.GetMethod("DetectPlayStation").Invoke(null, new object[] { sony }));
        var xbox = InputSystem.AddDevice<Gamepad>();
        Assert.AreEqual(false, type.GetMethod("DetectPlayStation").Invoke(null, new object[] { xbox }));
    }

    [Test] public void FixedSequenceUsesOneDeadlineAndCopiesAuthoredOrder()
    {
        var authored = new[] { QteButton.West, QteButton.North, QteButton.East, QteButton.South };
        var qte = new PressQte(4, 0, authored); authored[0] = QteButton.South;
        Assert.AreEqual(QteButton.West, qte.Expected);
        qte.Advance(0.5f, true);
        Assert.AreEqual(1, qte.CompletedInputs); Assert.AreEqual(3.5f, qte.Remaining);
        Assert.AreEqual(QteButton.North, qte.Expected); Assert.IsFalse(qte.Complete);
        qte.Advance(0.5f, true); qte.Advance(0.5f, true); qte.Advance(0, true);
        Assert.AreEqual(PressQteResult.Success, qte.Result); Assert.AreEqual(4, qte.CompletedInputs);
        qte.Advance(10, false, true); Assert.AreEqual(PressQteResult.Success, qte.Result);
    }
    [Test] public void SequenceWrongInputOrTimeoutStopsWithoutPartialBonus()
    {
        var sequence = new[] { QteButton.West, QteButton.West };
        var qte = new PressQte(1, 0, sequence); qte.Advance(0.2f, true);
        qte.Advance(0, true, true); Assert.AreEqual(PressQteResult.WrongInput, qte.Result);
        Assert.AreEqual(1, qte.CompletedInputs);
        qte = new PressQte(1, 0, sequence); qte.Advance(0.2f, true); qte.Advance(1);
        Assert.AreEqual(PressQteResult.TimedOut, qte.Result);
    }
    [Test] public void SequenceBindingsRequireFreshEdgesAndRejectOtherButtons()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>(); var pad = InputSystem.AddDevice<Gamepad>();
        var input = new PressQteInput(); input.Reset(pad);
        Press(keyboard.leftArrowKey); input.Read(keyboard, pad, out bool correct, out bool wrong, QteButton.West);
        Assert.IsTrue(correct); Assert.IsFalse(wrong);
        InputSystem.Update(); input.Read(keyboard, pad, out correct, out wrong, QteButton.West);
        Assert.IsFalse(correct, "Repeated identical steps require release and press.");
        Release(keyboard.leftArrowKey); Press(pad.buttonNorth);
        input.Read(keyboard, pad, out correct, out wrong, QteButton.North);
        Assert.IsTrue(correct); Assert.IsFalse(wrong);
        input.Read(keyboard, pad, out correct, out wrong, QteButton.East);
        Assert.IsFalse(correct); Assert.IsTrue(wrong);
    }

    [Test] public void PreparationDoesNotConsumeCountdownOrAcceptLaunchPress()
    {
        var qte = new PressQte(1.5f);
        qte.Advance(0.5f, true, true);
        Assert.AreEqual(PressQteResult.Waiting, qte.Result);
        Assert.AreEqual(1.5f, qte.Remaining);
        qte.Advance(0.5f); Assert.AreEqual(1, qte.Remaining);
        Assert.AreEqual(2f / 3, qte.Fraction, 0.0001f);
    }
    [Test] public void SuccessIsImmediateAndLocksAgainstFurtherInput()
    {
        var qte = new PressQte(1, 0); qte.Advance(0.4f);
        qte.Advance(0.1f, true); Assert.AreEqual(PressQteResult.Success, qte.Result);
        qte.Advance(2, false, true);
        Assert.AreEqual(PressQteResult.Success, qte.Result); Assert.AreEqual(0.6f, qte.Remaining, 0.0001f);
    }
    [Test] public void WrongInputWinsConflictsAndTimeoutCannotBeReversed()
    {
        var wrong = new PressQte(1, 0); wrong.Advance(0, true, true);
        Assert.AreEqual(PressQteResult.WrongInput, wrong.Result);
        var timeout = new PressQte(1, 0); timeout.Advance(2);
        timeout.Advance(0, true); Assert.AreEqual(PressQteResult.TimedOut, timeout.Result);
        Assert.AreEqual(0, timeout.Remaining); Assert.AreEqual(0, timeout.Fraction);
    }
    [Test] public void OnlyFreshSpaceOrSouthPressesSucceedAndDriftDoesNotFail()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>(); var pad = InputSystem.AddDevice<Gamepad>();
        var input = new PressQteInput(); input.Reset(pad);
        Press(keyboard.spaceKey); input.Read(keyboard, pad, out bool correct, out bool wrong);
        Assert.IsTrue(correct); Assert.IsFalse(wrong);
        InputSystem.Update(); input.Read(keyboard, pad, out correct, out wrong); Assert.IsFalse(correct);
        Release(keyboard.spaceKey);
        Press(pad.buttonSouth); input.Read(keyboard, pad, out correct, out wrong); Assert.IsTrue(correct);
        Release(pad.buttonSouth);
        Set(pad.leftStick, new Vector2(0.1f, 0)); input.Read(keyboard, pad, out correct, out wrong); Assert.IsFalse(wrong);
        Set(pad.leftStick, Vector2.right); input.Read(keyboard, pad, out correct, out wrong); Assert.IsTrue(wrong);
        Set(pad.leftStick, Vector2.zero); input.Read(keyboard, pad, out correct, out wrong); Assert.IsFalse(wrong);
        Press(keyboard.dKey); input.Read(keyboard, pad, out correct, out wrong); Assert.IsTrue(wrong);
    }
}
