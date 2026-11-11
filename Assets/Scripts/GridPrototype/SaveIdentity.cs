using UnityEngine;
namespace Game.GridPrototype
{
    [DisallowMultipleComponent]
    public sealed class SaveIdentity : MonoBehaviour
    {
        [SerializeField, HideInInspector] string id;
        public string Id => id;
#if UNITY_EDITOR
        public void AssignEditorId(string value){id=value;}
        void OnValidate(){if(string.IsNullOrEmpty(id))id=System.Guid.NewGuid().ToString("N");}
#endif
    }
}
