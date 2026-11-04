using UnityEngine;
namespace Game.World
{
    public sealed class WorldPlaceObject : MonoBehaviour
    {
        public string DisplayName = "Remembered structure";
        public GameObject PresentForm, PastForm;
        public bool CanRelocate = true, CanDrag = true;
        [Min(0)] public int RequiredStrength;
        public string SocketKey;
        public Vector3 BoundsCenter = new Vector3(0, .5f, 0), BoundsSize = Vector3.one;
        public bool Restored { get; private set; }
        public bool Displaced { get; private set; }
        public Vector3 ReturnPosition { get; private set; }
        public bool HasEffect => Restored || Displaced;
        public Bounds Volume(Vector3 position)
        {
            var matrix = Matrix4x4.TRS(position, transform.rotation, transform.lossyScale);
            var result = new Bounds(matrix.MultiplyPoint3x4(BoundsCenter), Vector3.zero);
            for (int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
                result.Encapsulate(matrix.MultiplyPoint3x4(BoundsCenter + Vector3.Scale(BoundsSize, new Vector3(x,y,z))*.5f));
            return result;
        }
        public void RememberOrigin() { if (!HasEffect) ReturnPosition = transform.position; }
        public void SetRestored(bool value)
        {
            Restored = value;
            if (PresentForm) PresentForm.SetActive(!value);
            if (PastForm) PastForm.SetActive(value);
            Physics.SyncTransforms();
        }
        public void Relocate(Vector3 position) { transform.position = position; Displaced = true; Physics.SyncTransforms(); }
        public void Revert()
        {
            if (Displaced) transform.position = ReturnPosition;
            Displaced = false; SetRestored(false);
        }
    }
}
