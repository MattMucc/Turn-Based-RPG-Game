using Game.GridPrototype;
using UnityEngine;
[RequireComponent(typeof(BoxCollider),typeof(SaveIdentity))]
public sealed class SaveCheckpoint : MonoBehaviour
{
    public string DisplayName="Checkpoint";
    void Reset(){GetComponent<BoxCollider>().isTrigger=true;}
    void OnTriggerEnter(Collider other)
    {
        var player=other.GetComponentInParent<PlayerMovement>();
        if(!player||!player.IsPossessed||!GameManager.Instance)return;
        GameManager.Instance.Saves.Checkpoint(DisplayName);
    }
    void OnDrawGizmos(){Gizmos.color=new Color(.3f,.8f,1);Gizmos.DrawWireSphere(transform.position,1.4f);}
}
