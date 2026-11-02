using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.GridPrototype;

public class PressQteTests : InputTestFixture
{
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
