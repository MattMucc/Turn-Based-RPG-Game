using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class PartyFollowerTests
{
    readonly List<GameObject> owned = new();
    readonly Vector3 origin = new(110000, 0, 110000);
    Type followerType;
    Component follower;
    Transform leader;
    GameObject body;
    GameObject Make(string name) { var obj = new GameObject(name); owned.Add(obj); return obj; }
    GameObject Box(Vector3 position, Vector3 scale)
    { var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); owned.Add(obj); obj.transform.position = origin + position; obj.transform.localScale = scale; return obj; }
    void Setup(float floorDepth = 40)
    {
        Box(new Vector3(0, -.5f, floorDepth == 40 ? 0 : 1), new Vector3(40, 1, floorDepth));
        leader = Make("Leader").transform; leader.position = origin + new Vector3(0, 1, 3);
        body = Make("Follower"); body.transform.position = origin + Vector3.up;
        var cc = body.AddComponent<CharacterController>(); cc.height = 2; cc.radius = .3f; cc.skinWidth = .02f;
        followerType = Type.GetType("PartyFollower, Assembly-CSharp", true); follower = body.AddComponent(followerType);
        followerType.GetMethod("Follow").Invoke(follower, new object[] { leader }); Physics.SyncTransforms();
    }
    void Step(int count = 1) { for (int i = 0; i < count; i++) followerType.GetMethod("StepFollow").Invoke(follower, new object[] { .02f }); }
    [TearDown] public void Cleanup() { for (int i = owned.Count - 1; i >= 0; i--) if (owned[i]) Object.DestroyImmediate(owned[i]); owned.Clear(); }
    [Test] public void FollowsMovingLeaderThenStopsAtPersonalSpacing()
    {
        Setup(); for (int i = 0; i < 100; i++) { leader.position += Vector3.forward * .06f; Step(); } Step(200);
        float gap = Vector3.Distance(new Vector3(body.transform.position.x, 1, body.transform.position.z), leader.position);
        Assert.That(gap, Is.InRange(1.6f, 3.35f)); Assert.Greater(body.transform.position.z, origin.z + 5);
    }
    [Test] public void FollowerStopsBeforeUnsupportedGapRatherThanTeleporting()
    {
        Setup(6);
        for (int i = 0; i < 100; i++) { leader.position += Vector3.forward * .06f; Step(); } Step(100);
        Assert.Less(body.transform.position.z, origin.z + 4); Assert.Greater(body.transform.position.y, .8f);
        Assert.IsTrue((bool)followerType.GetProperty("WaitingForSafeRoute").GetValue(follower));
    }
    [Test] public void DisabledFollowerDoesNotMoveAndCanResumeAfterRestoration()
    {
        Setup(); ((Behaviour)follower).enabled = false; var before = body.transform.position; leader.position += Vector3.forward * 2; Step(20);
        Assert.AreEqual(before, body.transform.position);
        ((Behaviour)follower).enabled = true; Step(100); Assert.Greater(body.transform.position.z, before.z + 1);
    }
    [Test] public void RecordedCornersAvoidWalkingStraightThroughWall()
    {
        Setup(); Box(new Vector3(2, 1, 1), new Vector3(.5f, 2, 3)); Physics.SyncTransforms();
        for (int i = 0; i < 80; i++) { leader.position += Vector3.right * .05f; Step(); }
        for (int i = 0; i < 80; i++) { leader.position -= Vector3.forward * .05f; Step(); }
        Step(200); Assert.Greater(body.transform.position.x, origin.x + 3);
        Assert.Less(body.transform.position.z, origin.z + 2);
    }
    [Test] public void DefaultRosterActivatesOnlyCompanionsAndBindsBothFollowersToControlledCharacter()
    {
        var managerType = Type.GetType("GameManager, Assembly-CSharp", true);
        var movementType = Type.GetType("PlayerMovement, Assembly-CSharp", true);
        followerType = Type.GetType("PartyFollower, Assembly-CSharp", true);
        var manager = Make("Party manager").AddComponent(managerType);
        var members = new List<Game.GridPrototype.BattleCharacterInfo>();
        for (int i = 0; i < 4; i++)
        {
            var obj = Make("Member " + i); obj.AddComponent<CharacterController>(); obj.AddComponent(movementType);
            members.Add(obj.AddComponent<Game.GridPrototype.BattleCharacterInfo>());
        }
        managerType.GetField("BattleParty").SetValue(manager, members.GetRange(0, 3));
        managerType.GetField("ReserveParty").SetValue(manager, members.GetRange(3, 1));
        managerType.GetField("<PossessedCharacter>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(manager, members[0].GetComponent(movementType));
        managerType.GetMethod("ConfigureFollowers").Invoke(manager, null);
        Assert.IsFalse(members[3].gameObject.activeSelf);
        for (int i = 1; i < 3; i++)
        {
            Assert.IsTrue(members[i].gameObject.activeSelf);
            var following = members[i].GetComponent(followerType);
            Assert.AreSame(members[0].transform, followerType.GetProperty("Leader").GetValue(following));
            Assert.IsFalse(((Behaviour)members[i].GetComponent(movementType)).enabled);
        }
    }
    [Test] public void ExplicitWaitingHoldsPositionUntilReleased()
    {
        Setup(); followerType.GetMethod("WaitAt").Invoke(follower, new object[] { null });
        float before = body.transform.position.z; leader.position += Vector3.forward * 4;
        Step(100); Assert.AreEqual(before, body.transform.position.z, .1f);
        followerType.GetMethod("ResumeFollowing").Invoke(follower, null); Step(150);
        Assert.Greater(body.transform.position.z, before + 2);
    }
}
