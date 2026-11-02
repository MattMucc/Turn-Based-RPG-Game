using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;
using Object = UnityEngine.Object;

public class GroundDashTests
{
    GameObject actor, floor, wall;
    Component movement;
    Type movementType;
    CharacterController controller;
    readonly Vector3 origin = new Vector3(30000, 50, 30000);
    [SetUp] public void Setup()
    {
        floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = origin - Vector3.up * .5f;
        floor.transform.localScale = new Vector3(20, 1, 20);
        actor = new GameObject("Movement test"); actor.transform.position = origin;
        controller = actor.AddComponent<CharacterController>();
        controller.height = 2; controller.center = Vector3.up;
        controller.radius = .4f; controller.skinWidth = .02f; controller.minMoveDistance = 0;
        movementType = Type.GetType("PlayerMovement, Assembly-CSharp", true);
        movement = actor.AddComponent(movementType);
        movementType.GetField("cameraYaw", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, actor.transform);
        movementType.GetMethod("Possess").Invoke(movement, null);
        Physics.SyncTransforms();
        for (int i = 0; i < 10; i++) Step();
    }
    void Step(bool dash = false, bool jump = false, bool held = false)
    { movementType.GetMethod("StepMovement").Invoke(movement, new object[] { .02f, Vector2.zero, jump, held, dash, false, false, false }); }
    bool Dashing => (bool)movementType.GetProperty("IsDashing").GetValue(movement);
    [TearDown] public void Cleanup()
    { Object.DestroyImmediate(actor); Object.DestroyImmediate(floor); if (wall) Object.DestroyImmediate(wall); Physics.SyncTransforms(); }

    [Test] public void GroundDashTravelsThenStopsAndRespectsCooldown()
    {
        var start = actor.transform.position;
        Step(dash: true); Assert.IsTrue(Dashing);
        for (int i = 0; i < 12; i++) Step();
        Assert.IsFalse(Dashing);
        Assert.That(actor.transform.position.z - start.z, Is.InRange(2.5f, 2.8f));
        Step(dash: true); Assert.IsFalse(Dashing);
        for (int i = 0; i < 30; i++) Step();
        Step(dash: true); Assert.IsTrue(Dashing);
    }
    [Test] public void FallingCannotDashAndLandingStaysSettled()
    {
        controller.enabled = false; actor.transform.position = origin + Vector3.up * 5; controller.enabled = true;
        Physics.SyncTransforms(); Step(); Step(dash: true);
        Assert.IsFalse(Dashing); Assert.AreEqual(0, controller.stepOffset);
        for (int i = 0; i < 100; i++) Step();
        var landed = actor.transform.position.y;
        Assert.That(landed, Is.InRange(origin.y - .05f, origin.y + .05f));
        for (int i = 0; i < 50; i++) { Step(); Assert.That(actor.transform.position.y, Is.EqualTo(landed).Within(.005f)); }
    }
    [Test] public void JumpCancelsDashAndHoldingJumpDoesNotBounceAfterLanding()
    {
        Step(dash: true); Step(jump: true, held: true);
        Assert.IsFalse(Dashing);
        Assert.Greater(actor.transform.position.y, origin.y + .1f);
        for (int i = 0; i < 160; i++) Step(held: true);
        Assert.That(actor.transform.position.y, Is.EqualTo(origin.y).Within(.05f));
    }
    [Test] public void WallStopsDashWithoutPassingThroughIt()
    {
        wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = origin + new Vector3(0, 1.5f, 1.5f);
        wall.transform.localScale = new Vector3(4, 3, .3f); Physics.SyncTransforms();
        Step(dash: true);
        for (int i = 0; i < 15; i++) Step();
        Assert.IsFalse(Dashing); Assert.Less(actor.transform.position.z, origin.z + 1.1f);
    }
    [Test] public void BattleDashSpendsActionOnceAndBanksUnusedMovement()
    {
        var unit = new GridUnit("Dash", Vector2Int.zero);
        Assert.IsFalse(unit.Dash()); unit.BeginTurn(); unit.SpendMovement(2);
        Assert.IsTrue(unit.Dash()); Assert.AreEqual(4, unit.Movement);
        Assert.IsTrue(unit.TurnOpen); Assert.IsFalse(unit.ActionAvailable);
        Assert.IsFalse(unit.Dash()); Assert.AreEqual(4, unit.Movement);
        unit.EndTurn(); unit.BeginTurn(); Assert.AreEqual(7, unit.Movement);
    }
}
