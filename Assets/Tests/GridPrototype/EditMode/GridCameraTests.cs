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
