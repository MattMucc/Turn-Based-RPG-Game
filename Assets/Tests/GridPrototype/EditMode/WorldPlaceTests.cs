using System.Collections.Generic;
using Game.World;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

public class WorldPlaceTests
{
    readonly List<GameObject> objects=new();
    readonly Vector3 origin=new Vector3(140000,0,140000);
    WorldPlaceService service;
    Transform caster;
    GameObject Make(string name){var o=new GameObject(name);objects.Add(o);return o;}
    GameObject Box(Vector3 pos,Vector3 size)
    {var o=Make("Solid");o.transform.position=origin+pos;o.AddComponent<BoxCollider>().size=size;return o;}
    WorldPlaceObject Target(float x=0)
    {
        var o=Make("Relic");o.transform.position=origin+Vector3.right*x;
        var t=o.AddComponent<WorldPlaceObject>();
        t.PresentForm=Make("Present");t.PresentForm.transform.SetParent(o.transform,false);t.PresentForm.AddComponent<BoxCollider>().center=Vector3.up*.5f;
        t.PastForm=Make("Past");t.PastForm.transform.SetParent(o.transform,false);t.PastForm.AddComponent<BoxCollider>().center=Vector3.up*.5f;t.PastForm.SetActive(false);
        Physics.SyncTransforms();return t;
    }
    [SetUp]public void Setup()
    {
        service=Make("Place service").AddComponent<WorldPlaceService>();caster=Make("Caster").transform;caster.position=origin+new Vector3(-2,1,0);
        Box(new Vector3(0,-.5f,0),new Vector3(30,1,30));Physics.SyncTransforms();
    }
    [TearDown]public void Cleanup(){for(int i=objects.Count-1;i>=0;i--)if(objects[i])Object.DestroyImmediate(objects[i]);objects.Clear();}
    [Test]public void RestoreAndRelocateShareOneEffectAndRecallOriginalPosition()
    {
        var a=Target();Assert.IsTrue(service.TryRestore(a,caster,12));
        Assert.IsTrue(service.TryMove(a,origin+Vector3.right*3,caster,0,12,true));Assert.IsTrue(a.Restored);Assert.IsTrue(a.Displaced);
        var b=Target(5);Assert.IsTrue(service.TryRestore(b,caster,12));
        Assert.IsFalse(a.HasEffect);Assert.AreEqual(origin,a.transform.position);Assert.AreSame(b,service.ActiveObject);
        Assert.IsTrue(service.TryDismiss());Assert.IsFalse(b.HasEffect);
    }
    [Test]public void OccupiedRestorationCannotBeReplacedAndPendingDismissWaitsForClearance()
    {
        var a=Target();var b=Target(5);Assert.IsTrue(service.TryRestore(a,caster,12));
        var person=Make("Walker");person.transform.position=origin+Vector3.up*2;var cc=person.AddComponent<CharacterController>();cc.height=2;cc.radius=.3f;Physics.SyncTransforms();
        Assert.IsFalse(service.TryRestore(b,caster,12));Assert.AreSame(a,service.ActiveObject);Assert.IsTrue(a.Restored);
        Assert.IsFalse(service.TryDismiss());Assert.IsTrue(service.PendingReturn);
        service.TickPendingReturn();Assert.IsTrue(a.Restored);
        person.transform.position+=Vector3.right*4;Physics.SyncTransforms();service.TickPendingReturn();Assert.IsNull(service.ActiveObject);Assert.IsFalse(a.Restored);
    }
    [Test]public void BlockedReturnDoesNotOverlapObjectAndRetriesWhenClear()
    {
        var a=Target();Assert.IsTrue(service.TryMove(a,origin+Vector3.right*3,caster,0,12,true));
        var obstruction=Box(Vector3.up*.5f,Vector3.one);Physics.SyncTransforms();Assert.IsFalse(service.TryDismiss());Assert.AreEqual(origin+Vector3.right*3,a.transform.position);
        obstruction.SetActive(false);Physics.SyncTransforms();service.TickPendingReturn();Assert.AreEqual(origin,a.transform.position);
    }
    [Test]public void DragRequiresStrengthDistanceAndClearPathAndStaysMoved()
    {
        var a=Target();a.RequiredStrength=8;
        Assert.IsFalse(service.TryMove(a,origin+Vector3.right*2,caster,7,12,false));
        Assert.IsFalse(service.TryMove(a,origin+Vector3.right*4,caster,8,12,false));
        var obstacle=Box(new Vector3(1,.5f,0),new Vector3(.2f,1,2));Physics.SyncTransforms();
        Assert.IsFalse(service.TryMove(a,origin+Vector3.right*2,caster,8,12,false));
        obstacle.SetActive(false);Physics.SyncTransforms();Assert.IsTrue(service.TryMove(a,origin+Vector3.right*2,caster,8,12,false));Assert.IsNull(service.ActiveObject);
        Assert.IsFalse(service.TryDismiss());Assert.AreEqual(origin+Vector3.right*2,a.transform.position);
    }
    [Test]public void RangeLimitsActivationNotMaintenanceAndInvalidReplacementPreservesActive()
    {
        var a=Target();var b=Target(5);Assert.IsTrue(service.TryRestore(a,caster,12));
        caster.position+=Vector3.back*100;service.TickPendingReturn();Assert.IsTrue(a.Restored);
        Assert.IsFalse(service.TryRestore(b,caster,12));Assert.AreSame(a,service.ActiveObject);
    }
    [Test]public void SocketUnlockIsPermanentAfterRelocationReturns()
    {
        var a=Target();a.SocketKey="Key";
        var socket=Make("Socket").AddComponent<WorldPlaceSocket>();socket.SocketKey="Key";socket.transform.position=origin+Vector3.right*3;
        socket.PermanentShortcut=Make("Shortcut");socket.PermanentShortcut.SetActive(false);
        int calls=0;socket.OnFirstActivation.AddListener(()=>calls++);
        Assert.IsTrue(service.TryMove(a,socket.Position,caster,0,12,true,socket));Assert.IsTrue(socket.PermanentShortcut.activeSelf);
        Assert.IsTrue(service.TryDismiss());Assert.IsTrue(socket.PermanentShortcut.activeSelf);socket.Activate(a);Assert.AreEqual(1,calls);
    }
    [Test]public void RelocationCanCrossGapButOrdinaryDraggingCannot()
    {
        objects[2].SetActive(false); // The initial floor; replace it with two banks.
        Box(new Vector3(-.5f,-.5f,0),new Vector3(2,1,4));Box(new Vector3(3,-.5f,0),new Vector3(2,1,4));
        var a=Target();Physics.SyncTransforms();
        Assert.IsFalse(service.TryMove(a,origin+Vector3.right*3,caster,99,12,false));
        Assert.IsTrue(service.TryMove(a,origin+Vector3.right*3,caster,0,12,true));
    }
}
