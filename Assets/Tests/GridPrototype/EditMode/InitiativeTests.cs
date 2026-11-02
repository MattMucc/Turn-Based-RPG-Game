using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;
using Object = UnityEngine.Object;

public class InitiativeTests
{
    [Test] public void HigherStatsLeadAndTiesKeepStableActorOrder()
    {
        var order = new InitiativeOrder(new[] { 5, 20, 20, 1 });
        CollectionAssert.AreEqual(new[] { 1, 2, 0, 3 }, order.Preview(order.First, _ => true));
        int actor = order.First;
        foreach (int next in new[] { 2, 0, 3, 1 }) { actor = order.Next(actor, _ => true); Assert.AreEqual(next, actor); }
        Assert.AreEqual(2, order.Round);
    }
    [Test] public void AmbushMovesOneTurnOnlyThenRestoresNormalOrder()
    {
        var order = new InitiativeOrder(new[] { 5, 20, 10 }, 0);
        int actor = order.First; Assert.AreEqual(0, actor);
        foreach (int next in new[] { 1, 2, 1, 2, 0 }) { actor = order.Next(actor, _ => true); Assert.AreEqual(next, actor); }
        Assert.AreEqual(2, order.Round);
    }
    [Test] public void DefeatedActorsAreSkippedAcrossRoundBoundary()
    {
        var order = new InitiativeOrder(new[] { 5, 20, 10 }, 0);
        Assert.AreEqual(2, order.Next(0, i => i != 1));
        CollectionAssert.AreEqual(new[] { 2, 2 }, order.Preview(2, i => i != 1));
        Assert.AreEqual(2, order.Next(2, i => i != 1));
        Assert.AreEqual(2, order.Round);
    }
    [TestCase(false, 1)] [TestCase(true, 0)]
    public void EncounterUsesAuthoredStatsWithoutChangingActorIdentity(bool preemptive, int expectedFirst)
    {
        var objects = new List<GameObject>();
        try
        {
            var origin = new Vector3(40000, 0, 40000);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(floor);
            floor.transform.position = origin - Vector3.up * .5f; floor.transform.localScale = new Vector3(30, 1, 30);
            Physics.SyncTransforms();
            var figures = new List<Transform>();
            for (int i = 0; i < 3; i++)
            {
                var actor = new GameObject("Actor " + i); objects.Add(actor); figures.Add(actor.transform);
                actor.AddComponent<BattleCharacterInfo>().Initiative = new[] { 5, 20, 10 }[i];
            }
            var camera = new GameObject("Source camera"); objects.Add(camera);
            var root = new GameObject("Encounter"); objects.Add(root);
            var battle = root.AddComponent<InSceneGridBattle>();
            battle.Initialize(new TerrainBattleGrid(origin, 8, 8, 2, ~0, new Collider[0]), figures,
                new float[3], new[] { new Vector2Int(3, 3), new Vector2Int(4, 3), new Vector2Int(5, 3) },
                camera.AddComponent<Camera>(), Shader.Find("Game/GridOverlay"), _ => { }, preemptive);
            battle.AdvanceFacing(1);
            Assert.AreEqual(expectedFirst, battle.CurrentActorIndex);
            Assert.AreSame(figures[0], battle.Actor(0));
            Assert.AreEqual(preemptive, battle.PlayerReady);
            Assert.AreEqual(3, battle.Combatants[expectedFirst].Movement);
            Assert.AreEqual(0, battle.Combatants[expectedFirst == 0 ? 1 : 0].Movement);
            var advance = typeof(InSceneGridBattle).GetMethod("AdvanceTurn", BindingFlags.Instance | BindingFlags.NonPublic);
            var expected = preemptive ? new[] { 1, 2, 1, 2, 0 } : new[] { 2, 0, 1, 2, 0 };
            foreach (int next in expected) { advance.Invoke(battle, null); Assert.AreEqual(next, battle.CurrentActorIndex); }
            Assert.AreEqual(2, battle.Round);
        }
        finally { for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]); }
    }
}
