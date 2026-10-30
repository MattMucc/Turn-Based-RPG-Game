using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class EncounterRestoreTests
{
    [Test] public void SnapshotRestoresPositionActivationAndOriginalColliderStates()
    {
        // Test the actual manager snapshot without starting a scene or entering Play Mode.
        var actor = new GameObject("Restore test");
        var solid = actor.AddComponent<BoxCollider>();
        var trigger = actor.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.enabled = false;
        actor.transform.position = new Vector3(10, 2, 10);
        actor.transform.rotation = Quaternion.Euler(0, 37, 0);
        try
        {
            var type = Type.GetType("BattleManager, Assembly-CSharp").GetNestedType("ActorSnapshot", BindingFlags.NonPublic);
            var snapshot = Activator.CreateInstance(type, new object[] { actor.transform });
            type.GetMethod("Suspend").Invoke(snapshot, null);
            Assert.IsFalse(solid.enabled); Assert.IsFalse(trigger.enabled);
            actor.transform.position = Vector3.zero; actor.transform.rotation = Quaternion.identity;
            actor.SetActive(false);
            type.GetMethod("Restore").Invoke(snapshot, null);
            Assert.IsTrue(actor.activeSelf); Assert.IsTrue(solid.enabled); Assert.IsFalse(trigger.enabled);
            Assert.AreEqual(new Vector3(10, 2, 10), actor.transform.position);
            Assert.That(Quaternion.Angle(Quaternion.Euler(0, 37, 0), actor.transform.rotation), Is.LessThan(0.01));
        }
        finally { UnityEngine.Object.DestroyImmediate(actor); }
    }
}
