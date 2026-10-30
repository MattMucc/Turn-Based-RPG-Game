using NUnit.Framework;
using UnityEngine;
using Tenebrasia.GridPrototype;

public class GridCameraTests
{
    [Test] public void ShortClickSelectsButPanDoesNot()
    {
        var gesture = new GridPointerGesture();
        gesture.Begin(Vector2.zero); Assert.IsTrue(gesture.End(new Vector2(2, 1)));
        gesture.Begin(Vector2.zero); gesture.Update(new Vector2(10, 0));
        Assert.IsFalse(gesture.End(Vector2.zero), "Returning to the press point must not undo a drag.");
    }
    [Test] public void CancelledOrUnstartedGestureCannotSelect()
    {
        var gesture = new GridPointerGesture();
        Assert.IsFalse(gesture.End(Vector2.zero));
        gesture.Begin(Vector2.zero); gesture.Cancel(); Assert.IsFalse(gesture.End(Vector2.zero));
    }
    [Test] public void FocusFollowsMovingActorAndManualMovementReleasesIt()
    {
        var go = new GameObject("Follow camera", typeof(Camera)); var target = new GameObject("Moving unit");
        try
        {
            var control = go.AddComponent<GridCameraController>(); control.FocusTarget = target.transform;
            control.FocusActive(); var original = go.transform.position;
            target.transform.position += new Vector3(2, 1, 3); control.FollowFocusedTarget();
            Assert.That(Vector3.Distance(go.transform.position, original + new Vector3(2, 1, 3)), Is.LessThan(0.001f));
            control.Fly(Vector3.right); original = go.transform.position;
            target.transform.position += Vector3.right * 5; control.FollowFocusedTarget(); Assert.AreEqual(original, go.transform.position);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(target); }
    }
    [Test] public void ActionFrameIncludesBothAttackerAndDistantTarget()
    {
        var go = new GameObject("Action camera", typeof(Camera));
        try
        {
            var camera = go.GetComponent<Camera>(); camera.aspect = 1.5f;
            var control = go.AddComponent<GridCameraController>(); go.transform.rotation = Quaternion.Euler(40, 0, 0);
            var points = new[] { new Vector3(-12, 0, 0), new Vector3(12, 3, 0) };
            control.FrameAction(points);
            foreach (var point in points)
            { var screen = camera.WorldToViewportPoint(point); Assert.That(screen.x, Is.InRange(0.1f, 0.9f)); Assert.That(screen.y, Is.InRange(0.1f, 0.9f)); Assert.Greater(screen.z, 0); }
        }
        finally { Object.DestroyImmediate(go); }
    }
    [Test] public void SmoothFocusStartsWithoutJumpingArrivesAndCanBeInterrupted()
    {
        var go = new GameObject("Smooth camera", typeof(Camera)); var actor = new GameObject("Next actor");
        try
        {
            var control = go.AddComponent<GridCameraController>(); go.transform.position = new Vector3(0, 10, -10);
            actor.transform.position = new Vector3(20, 0, 0); var start = go.transform.position;
            control.SmoothFocus(actor.transform); Assert.AreEqual(start, go.transform.position);
            control.AdvanceFocus(0.275f); Assert.IsTrue(control.Transitioning);
            Assert.Greater(Vector3.Distance(start, go.transform.position), 0);
            Assert.Greater(Vector3.Distance(go.transform.position, actor.transform.position - go.transform.forward * control.FocusDistance), 0.1f);
            control.AdvanceFocus(0.275f); Assert.IsFalse(control.Transitioning);
            Assert.That(Vector3.Distance(go.transform.position, actor.transform.position - go.transform.forward * control.FocusDistance), Is.LessThan(0.001f));
            control.SmoothFocus(actor.transform); control.Fly(Vector3.right); start = go.transform.position;
            control.AdvanceFocus(1); Assert.AreEqual(start, go.transform.position);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(actor); }
    }
    [Test] public void FocusOrbitMaintainsRadiusAndTranslationReleasesCharacter()
    {
        var go = new GameObject("Test camera", typeof(Camera));
        var target = new GameObject("Test focus");
        try
        {
            var control = go.AddComponent<GridCameraController>();
            control.FocusTarget = target.transform;
            target.transform.position = new Vector3(2, 1, 3);
            go.transform.rotation = Quaternion.Euler(35, 0, 0);
            control.FocusActive(); control.BeginOrbit();
            float radius = Vector3.Distance(go.transform.position, target.transform.position);
            control.Orbit(new Vector2(100, 20));
            Assert.That(Vector3.Distance(go.transform.position, target.transform.position), Is.EqualTo(radius).Within(0.001));
            Assert.That(Vector3.Dot(go.transform.forward, (target.transform.position - go.transform.position).normalized), Is.GreaterThan(0.999));
            control.Fly(Vector3.right); Assert.IsFalse(control.CharacterFocused);
            control.FocusActive(); control.Dolly(1); Assert.IsFalse(control.CharacterFocused);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(target); }
    }
}
