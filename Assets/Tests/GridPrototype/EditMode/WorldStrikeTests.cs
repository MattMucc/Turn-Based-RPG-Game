using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using Object = UnityEngine.Object;

public class WorldStrikeTests
{
    readonly List<GameObject> objects = new List<GameObject>();
    GameObject player, enemy;
    Component movement, trigger;
    Type enemyType, strikeType;
    GameObject Make(string name) { var value = new GameObject(name); objects.Add(value); return value; }
    [SetUp] public void Setup()
    {
        var origin = new Vector3(50000, 20, 50000);
        player = Make("Player"); player.transform.position = origin;
        player.AddComponent<CapsuleCollider>();
        enemy = Make("Enemy"); enemy.transform.position = origin + Vector3.forward * 3;
        enemy.AddComponent<CharacterController>();
        enemyType = Type.GetType("EnemyMovement, Assembly-CSharp", true);
        movement = enemy.AddComponent(enemyType);
        trigger = enemy.AddComponent(Type.GetType("EnemyTrigger, Assembly-CSharp", true));
        var group = Make("Group"); group.transform.position = origin;
        var bounds = group.AddComponent<BoxCollider>(); bounds.isTrigger = true; bounds.size = Vector3.one * 40;
        var groupComponent = group.AddComponent(Type.GetType("BattleGroup, Assembly-CSharp", true));
        trigger.GetType().GetMethod("Initialize").Invoke(trigger, new object[] { groupComponent });
        strikeType = Type.GetType("WorldEncounterStrike, Assembly-CSharp", true);
        Physics.SyncTransforms();
    }
    [TearDown] public void Cleanup() { for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]); objects.Clear(); }
    bool Aware() => (bool)enemyType.GetMethod("IsAwareOf").Invoke(movement, new object[] { player.transform });
    object Target() => strikeType.GetMethod("FindTarget").Invoke(null, new object[] { player.transform, 3.5f, 60f });
    [Test] public void BehindEnemyIsUnawareButFacingPlayerIsDetectedImmediately()
    {
        Assert.IsFalse(Aware()); Assert.AreSame(trigger, Target());
        enemy.transform.rotation = Quaternion.Euler(0, 180, 0); Physics.SyncTransforms();
        Assert.IsTrue(Aware());
    }
    [Test] public void AlertedEnemyRemainsAwareEvenWhenFacingAway()
    {
        enemyType.GetMethod("StartChasing").Invoke(movement, null);
        Assert.IsTrue(Aware());
    }
    [Test] public void StrikeRequiresForwardReachAndClearLineOfSight()
    {
        player.transform.rotation = Quaternion.Euler(0, 180, 0); Assert.IsNull(Target());
        player.transform.rotation = Quaternion.identity;
        enemy.transform.position += Vector3.forward * 5; Physics.SyncTransforms(); Assert.IsNull(Target());
        enemy.transform.position -= Vector3.forward * 5;
        var wall = Make("Wall"); wall.transform.position = player.transform.position + Vector3.forward * 1.5f;
        wall.AddComponent<BoxCollider>().size = new Vector3(4, 4, .25f); Physics.SyncTransforms();
        Assert.IsNull(Target());
        enemy.transform.rotation = Quaternion.Euler(0, 180, 0); Assert.IsFalse(Aware());
    }
    [Test] public void WorldAttackUsesRightTriggerAndMouseWithoutChangingBattleBindings()
    {
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/PlayerInputActions.inputactions");
        var attack = asset.FindAction("World/Attack", true);
        Assert.IsTrue(HasPath(attack, "<Gamepad>/rightTrigger"));
        Assert.IsTrue(HasPath(attack, "<Mouse>/leftButton"));
        Assert.IsFalse(HasPath(attack, "<Gamepad>/buttonWest"));
    }
    static bool HasPath(InputAction action, string path)
    { foreach (var binding in action.bindings) if (binding.path == path) return true; return false; }
}
