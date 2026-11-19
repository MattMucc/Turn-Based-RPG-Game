using System;
using Game.GridPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class MerchantPrototypeSetup
{
    [MenuItem("Tools/Game/Prepare prototype merchant")]
    public static void Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        if(UnityEngine.Object.FindFirstObjectByType<Merchant>())return;
        var manager=UnityEngine.Object.FindFirstObjectByType<GameManager>();if(!manager)throw new InvalidOperationException("No GameManager.");
        var root=new GameObject("Prototype Merchant");Undo.RegisterCreatedObjectUndo(root,"Add merchant");root.transform.position=new Vector3(14,0,-14);
        if(Physics.Raycast(root.transform.position+Vector3.up*8,Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore))root.transform.position=hit.point;
        var merchant=root.AddComponent<Merchant>();root.GetComponent<BoxCollider>().isTrigger=true;
        foreach(var item in manager.Equipment.Catalog)if(item)merchant.Offers.Add(new Merchant.Offer{Equipment=item,BuyPrice=50,SellPrice=10});
        merchant.Offers.Add(new Merchant.Offer{Item=PartyInventory.HealthPotion(1),BuyPrice=15,SellPrice=5});
        merchant.Offers.Add(new Merchant.Offer{Item=PartyInventory.ManaPotion(1),BuyPrice=15,SellPrice=5});
        var model=GameObject.CreatePrimitive(PrimitiveType.Capsule);model.name="Merchant placeholder";model.transform.SetParent(root.transform,false);model.transform.localPosition=Vector3.up;
        var sign=new GameObject("Merchant sign");sign.transform.SetParent(root.transform,false);sign.transform.localPosition=Vector3.up*2.5f;
        var text=sign.AddComponent<TextMesh>();text.text="MERCHANT";text.characterSize=.12f;text.fontSize=36;text.anchor=TextAnchor.MiddleCenter;
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }
}
