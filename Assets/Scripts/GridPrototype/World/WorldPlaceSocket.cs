using UnityEngine;
using UnityEngine.Events;
namespace Game.World
{
    public sealed class WorldPlaceSocket : MonoBehaviour
    {
        public string SocketKey;
        public Transform Destination;
        [Min(.1f)] public float SnapRadius = 1;
        public GameObject PermanentShortcut;
        public UnityEvent OnFirstActivation = new UnityEvent();
        public bool Activated { get; private set; }
        public Vector3 Position => Destination ? Destination.position : transform.position;
        public bool Accepts(WorldPlaceObject obj) => obj && obj.SocketKey == SocketKey;
        public void Activate(WorldPlaceObject obj)
        {
            if (Activated || !Accepts(obj)) return;
            Activated = true;
            if (PermanentShortcut) PermanentShortcut.SetActive(true);
            Physics.SyncTransforms(); OnFirstActivation.Invoke();
        }
    }
}
