using NUnit.Framework;
using UnityEngine;
using Tenebrasia.GridPrototype;

public class DefenseImpactTests
{
    [TestCase(DefenseChoice.Dodge, 1.2f, DefenseResult.Dodged)]
    [TestCase(DefenseChoice.Parry, 1.2f, DefenseResult.TooEarly)]
    [TestCase(DefenseChoice.Parry, 1.35f, DefenseResult.Parried)]
    [TestCase(DefenseChoice.Dodge, 1.5f, DefenseResult.TooLate)]
    [TestCase(DefenseChoice.Both, 1.35f, DefenseResult.ConflictingInput)]
    public void WindowsDistinguishDodgeParryAndFailedAttempts(DefenseChoice choice, float time, DefenseResult expected)
    {
        var hit = new DefenseImpact(1.4f, 0.3f, 0.12f); hit.Advance(time); hit.Attempt(choice);
        hit.Advance(2); Assert.IsTrue(hit.Resolve()); Assert.AreEqual(expected, hit.Result);
        Assert.IsFalse(hit.Resolve(), "Damage must resolve once only");
    }
    [TestCase(1.4f, 0.12f)]
    [TestCase(0.9f, 0.21f)]
    public void TimingBarGoldBandMatchesParryWindowAndImpactIsAtThreeQuarters(float duration, float window)
    {
        var hit = new DefenseImpact(duration, 0.4f, window);
        Assert.AreEqual(0.75f, hit.BarPosition(duration), 0.0001f);
        float start = hit.BarPosition(duration - window);
        hit.Advance(duration - window * 0.5f);
        Assert.That(hit.BarPosition(hit.Elapsed), Is.InRange(start, 0.75f));
        hit.Attempt(DefenseChoice.Parry); Assert.AreEqual(DefenseResult.Parried, hit.Result);
        hit.Advance(window); Assert.Greater(hit.BarPosition(hit.Elapsed), 0.75f);
        var late = new DefenseImpact(duration, 0.4f, window);
        late.Advance(duration + 0.01f); late.Attempt(DefenseChoice.Parry);
        Assert.AreEqual(DefenseResult.TooLate, late.Result);
    }
    [Test] public void ParryFlashHasVisibleGeometryAndDedicatedShader()
    {
        var shader = Resources.Load<Shader>("ParryFlash");
        Assert.IsNotNull(shader); Assert.IsTrue(shader.isSupported);
        var obj = new GameObject("Test parry flash");
        try
        {
            obj.AddComponent<ParryBurst>().Initialize(Vector3.zero, Vector3.back, shader);
            var lines = obj.GetComponentsInChildren<LineRenderer>();
            Assert.AreEqual(17, lines.Length);
            foreach (var line in lines)
            {
                Assert.AreEqual(shader, line.sharedMaterial.shader);
                Assert.Greater(line.widthMultiplier, 0.05f);
                Assert.Greater(line.sharedMaterial.color.a, 0.9f);
            }
        }
        finally { Object.DestroyImmediate(obj); }
    }
    [Test] public void RegisteredPressRemainsVisibleAndReportsLateOffset()
    {
        var hit = new DefenseImpact(1, 0.3f, 0.12f);
        hit.Advance(1.03f); hit.Resolve(); hit.Attempt(DefenseChoice.Parry);
        float press = hit.DisplayTime; hit.Advance(0.2f);
        Assert.AreEqual(press, hit.DisplayTime);
        StringAssert.StartsWith("Too late (", hit.AttemptFeedback);
        Assert.AreEqual(DefenseResult.TooLate, hit.Result);
        var success = new DefenseImpact(1, 0.3f, 0.12f);
        success.Advance(0.95f); success.Attempt(DefenseChoice.Parry); success.Advance(0.2f); success.Resolve();
        Assert.AreEqual(0.95f, success.DisplayTime, 0.0001f);
        Assert.AreEqual("", success.AttemptFeedback);
    }
    [TestCase(1.4f)]
    [TestCase(0.9f)]
    public void LatencyCompensationAcceptsDelayedGoldPressWithoutWideningWindow(float duration)
    {
        var hit = new DefenseImpact(duration, 0.3f, 0.12f, true, 0.035f);
        hit.Advance(duration + 0.016f);
        Assert.IsFalse(hit.Resolve(), "Damage must wait for the compensated input deadline.");
        hit.Attempt(DefenseChoice.Parry);
        Assert.AreEqual(DefenseResult.Parried, hit.Result);
        Assert.AreEqual(duration - 0.019f, hit.AttemptTime, 0.0001f);
        hit.Advance(0.03f); Assert.IsTrue(hit.Resolve()); Assert.AreEqual(0, hit.Damage(25, false, true));
        Assert.AreEqual(0.12f, hit.ParryWindow, 0.0001f);
        var early = new DefenseImpact(duration, 0.3f, 0.12f, true, 0.035f);
        early.Advance(duration - 0.12f + 0.025f); early.Attempt(DefenseChoice.Parry);
        Assert.AreEqual(DefenseResult.TooEarly, early.Result);
        var late = new DefenseImpact(duration, 0.3f, 0.12f, true, 0.035f);
        late.Advance(duration + 0.05f); late.Resolve(); late.Attempt(DefenseChoice.Parry);
        Assert.AreEqual(DefenseResult.TooLate, late.Result);
    }
    [Test] public void FirstAttemptLocksHitAndNextHitHasFreshOpportunity()
    {
        var first = new DefenseImpact(1, 0.3f, 0.12f); first.Advance(0.2f); first.Attempt(DefenseChoice.Parry);
        first.Advance(0.75f); Assert.IsFalse(first.Attempt(DefenseChoice.Dodge));
        first.Advance(0.1f); first.Resolve(); Assert.AreEqual(25, first.Damage(25, false, true));
        var next = new DefenseImpact(1, 0.3f, 0.12f); next.Advance(0.95f); next.Attempt(DefenseChoice.Parry);
        next.Advance(0.1f); next.Resolve(); Assert.AreEqual(0, next.Damage(25, false, true));
    }
    [Test] public void SharedInputRespectsEachTargetsEligibilityAndBlock()
    {
        var hit = new DefenseImpact(1, 0.3f, 0.12f); hit.Advance(0.95f); hit.Attempt(DefenseChoice.Dodge);
        hit.Advance(0.1f); hit.Resolve();
        Assert.IsTrue(hit.AvoidsHit(true)); Assert.IsFalse(hit.AvoidsHit(false));
        Assert.AreEqual(0, hit.Damage(25, false, true));
        Assert.AreEqual(25, hit.Damage(25, false, false));
        Assert.AreEqual(12, hit.Damage(25, true, false));
        Assert.AreEqual(DefenseResult.Unable, hit.ForTarget(false));
    }
    [Test] public void MissedAndLateInputsRetainBlockButCannotUndoDamage()
    {
        var hit = new DefenseImpact(1, 0.3f, 0.12f); hit.Advance(1.1f); hit.Resolve();
        Assert.AreEqual(12, hit.Damage(25, true, true));
        hit.Attempt(DefenseChoice.Parry); Assert.AreEqual(DefenseResult.TooLate, hit.Result);
        Assert.AreEqual(12, hit.Damage(25, true, true));
    }
    [Test] public void UnparryableHitsCanStillBeDodged()
    {
        var parry = new DefenseImpact(1, 0.3f, 0.12f, false); parry.Advance(0.95f); parry.Attempt(DefenseChoice.Parry);
        Assert.AreEqual(DefenseResult.Unparryable, parry.Result);
        var dodge = new DefenseImpact(1, 0.3f, 0.12f, false); dodge.Advance(0.95f); dodge.Attempt(DefenseChoice.Dodge);
        Assert.AreEqual(DefenseResult.Dodged, dodge.Result);
    }
    [Test] public void DodgeSlowMotionExpiresPreciselyEvenAcrossALongFrame()
    {
        var clock = new DefenseSlowMotion(); clock.Begin(0.18f, 0.15f);
        Assert.AreEqual(0.015f, clock.Advance(0.1f), 0.0001f);
        Assert.AreEqual(0.132f, clock.Advance(0.2f), 0.0001f);
        Assert.AreEqual(0.1f, clock.Advance(0.1f), 0.0001f);
        Assert.AreEqual(0, clock.Remaining);
    }
    [Test] public void LeavingDefenseClearsSlowMotionImmediately()
    {
        var clock = new DefenseSlowMotion(); clock.Begin(0.18f, 0.15f); clock.Clear();
        Assert.AreEqual(0.1f, clock.Advance(0.1f), 0.0001f);
    }
    [Test] public void AssistanceWidensWindowsWithoutChangingAttackDuration()
    {
        var obj = new GameObject("Defense settings");
        try
        {
            var settings = obj.AddComponent<DefensePrototypeSettings>(); var standard = settings.CreateImpact(1.4f);
            settings.Assisted = true; var assisted = settings.CreateImpact(1.4f);
            Assert.AreEqual(standard.Duration, assisted.Duration);
            Assert.Greater(assisted.DodgeWindow, standard.DodgeWindow); Assert.Greater(assisted.ParryWindow, standard.ParryWindow);
            Assert.AreEqual(0.3f, standard.DodgeWindow, 0.001f, "Existing impact retains its captured settings");
        }
        finally { Object.DestroyImmediate(obj); }
    }
}
