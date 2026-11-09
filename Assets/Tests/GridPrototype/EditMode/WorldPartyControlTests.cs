using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using Game.GridPrototype;
using Object = UnityEngine.Object;

public class WorldPartyControlTests
{
    readonly List<GameObject> objects = new();
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    GameObject Make(string name) { var obj = new GameObject(name); objects.Add(obj); return obj; }
    [TearDown] public void Cleanup()
    { foreach (var obj in objects) if (obj) Object.DestroyImmediate(obj); objects.Clear(); Time.timeScale = 1; }

    [TestCase(1, 1)] [TestCase(-1, 2)]
    public void SwitchingFreezesTimeAndTransfersControlOnlyAfterCameraArrives(int direction, int expected)
    {
        var type = Type.GetType("GameManager, Assembly-CSharp", true);
        var movement = Type.GetType("PlayerMovement, Assembly-CSharp", true);
        var manager = Make("Manager").AddComponent(type);
        var camera = Make("Camera rig").AddComponent(Type.GetType("PlayerRotation, Assembly-CSharp", true));
        var party = new List<BattleCharacterInfo>();
        for (int i = 0; i < 3; i++)
        {
            var obj = Make("Member " + i); obj.transform.position = Vector3.right * i * 10;
            obj.AddComponent<CharacterController>(); obj.AddComponent(movement);
            party.Add(obj.AddComponent<BattleCharacterInfo>());
        }
        type.GetField("BattleParty").SetValue(manager, party);
        type.GetField("<PossessedCharacter>k__BackingField", Hidden).SetValue(manager, party[0].GetComponent(movement));
        type.GetField("<PlayerRotation>k__BackingField", Hidden).SetValue(manager, camera);
        Time.timeScale = .75f;
        Assert.IsTrue((bool)type.GetMethod("TryCycleCharacter").Invoke(manager, new object[] { direction }));
        Assert.AreEqual(0, Time.timeScale);
        type.GetMethod("AdvanceCharacterSwitch").Invoke(manager, new object[] { .2f });
        Assert.AreSame(party[0].GetComponent(movement), type.GetProperty("PossessedCharacter").GetValue(manager));
        Assert.Greater(camera.transform.position.x, 0); Assert.Less(camera.transform.position.x, party[expected].transform.position.x);
        type.GetMethod("AdvanceCharacterSwitch").Invoke(manager, new object[] { 1f });
        Assert.AreEqual(.75f, Time.timeScale);
        Assert.AreSame(party[expected].GetComponent(movement), type.GetProperty("PossessedCharacter").GetValue(manager));
        Assert.AreEqual(expected * 10, camera.transform.position.x);
        var encounter = (List<BattleCharacterInfo>)type.GetMethod("GetEncounterParty").Invoke(manager, null);
        Assert.AreSame(party[expected], encounter[0]); Assert.AreEqual("Member 0", party[0].name);
    }

    [Test] public void SharedInputCloneDoesNotInheritKeyboardOnlyDeviceRestriction()
    {
        var source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/PlayerInputActions.inputactions"));
        var type = Type.GetType("PlayerInputReader, Assembly-CSharp", true);
        var reader = Make("Reader").AddComponent(type);
        try
        {
            source.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
            type.GetField("input", Hidden).SetValue(reader, source);
            type.GetMethod("Awake", Hidden).Invoke(reader, null);
            var clone = (InputActionAsset)type.GetField("input", Hidden).GetValue(reader);
            Assert.AreNotSame(source, clone); Assert.IsNull(clone.bindingMask); Assert.IsNull(clone.devices);
            clone.FindActionMap("World", true).Enable();
            // Resolve the actual gamepad controls without sending input to the user's editor.
            var gamepad = InputSystem.AddDevice<Gamepad>();
            try { CollectionAssert.Contains(clone.FindAction("World/Move", true).controls, gamepad.leftStick); }
            finally { InputSystem.RemoveDevice(gamepad); }
        }
        finally { Object.DestroyImmediate(source); }
    }
    [Test] public void OutOfCombatRosterReplacementPreservesSlotPositionAndPossession()
    {
        var type=Type.GetType("GameManager, Assembly-CSharp",true);
        var movement=Type.GetType("PlayerMovement, Assembly-CSharp",true);
        var manager=Make("Roster manager").AddComponent(type);
        var members=new List<BattleCharacterInfo>();
        for(int i=0;i<4;i++)
        {
            var go=Make("Member "+i);go.transform.position=Vector3.right*i*3;
            go.AddComponent<CharacterController>();go.AddComponent(movement);members.Add(go.AddComponent<BattleCharacterInfo>());
        }
        var party=members.GetRange(0,3);var reserves=members.GetRange(3,1);
        type.GetField("BattleParty").SetValue(manager,party);type.GetField("ReserveParty").SetValue(manager,reserves);
        type.GetField("<PossessedCharacter>k__BackingField",Hidden).SetValue(manager,members[0].GetComponent(movement));
        var position=members[0].transform.position;
        Assert.IsTrue((bool)type.GetMethod("TrySetActiveMember").Invoke(manager,new object[]{0,members[3]}));
        Assert.AreSame(members[3],party[0]);Assert.AreSame(members[0],reserves[0]);Assert.AreEqual(position,members[3].transform.position);
        Assert.AreSame(members[3].GetComponent(movement),type.GetProperty("PossessedCharacter").GetValue(manager));
        Assert.IsFalse(members[0].gameObject.activeSelf);Assert.IsTrue(members[3].gameObject.activeSelf);
    }
    [Test] public void OnlyManagerControlledCharacterCanTriggerEvenIfFollowerHasPossessionFlag()
    {
        var type=Type.GetType("GameManager, Assembly-CSharp",true);var movement=Type.GetType("PlayerMovement, Assembly-CSharp",true);
        var instance=type.GetField("<Instance>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);var previous=instance.GetValue(null);
        var manager=Make("Encounter manager").AddComponent(type);
        var leader=Make("Leader");leader.AddComponent<CharacterController>();var controlled=leader.AddComponent(movement);
        var follower=Make("Follower");follower.AddComponent<CharacterController>();var companion=follower.AddComponent(movement);
        movement.GetMethod("Possess").Invoke(controlled,null);movement.GetMethod("Possess").Invoke(companion,null);
        try
        {
            instance.SetValue(null,manager);type.GetField("<PossessedCharacter>k__BackingField",Hidden).SetValue(manager,controlled);
            var check=Type.GetType("EnemyTrigger, Assembly-CSharp",true).GetMethod("IsControlledActor");
            Assert.IsTrue((bool)check.Invoke(null,new object[]{controlled}));Assert.IsFalse((bool)check.Invoke(null,new object[]{companion}));
            type.GetField("<PossessedCharacter>k__BackingField",Hidden).SetValue(manager,companion);
            Assert.IsFalse((bool)check.Invoke(null,new object[]{controlled}));Assert.IsTrue((bool)check.Invoke(null,new object[]{companion}));
        }
        finally{instance.SetValue(null,previous);}
    }
}
