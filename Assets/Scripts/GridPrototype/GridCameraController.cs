using UnityEngine;
using UnityEngine.InputSystem;

namespace Tenebrasia.GridPrototype
{
    // A drag never becomes a grid click, even if the pointer returns to its origin.
    public sealed class GridPointerGesture
    {
        public bool Held { get; private set; }
        public bool Dragged { get; private set; }
        private Vector2 origin;
        public void Begin(Vector2 position) { origin = position; Held = true; Dragged = false; }
        public void Update(Vector2 position)
        { if (Held && (position - origin).sqrMagnitude > 36) Dragged = true; }
        public bool End(Vector2 position)
        {
            Update(position); bool click = Held && !Dragged; Held = false; return click;
        }
        public void Cancel() { Held = false; Dragged = false; }
    }

    [DefaultExecutionOrder(-20)]
    public sealed class GridCameraController : MonoBehaviour
    {
        public float FlySpeed = 10f;
        public float LookSensitivity = 0.18f;
        public float ZoomSpeed = 2f;
        public float FocusDistance = 12f;
        public Transform FocusTarget;
        public System.Func<Vector2, bool> PointerOverUI;
        public Vector3 OrbitPivot { get; private set; }
        public bool CharacterFocused { get; private set; }
        private Camera view;
        private bool rotating, orbiting, clickPending;
        private Vector2 clickPosition;
        private float pivotDistance = 15f;
        private readonly GridPointerGesture pan = new GridPointerGesture();

        private void Awake()
        {
            view = GetComponent<Camera>();
            view.orthographic = false; view.fieldOfView = 55; view.nearClipPlane = 0.1f;
            OrbitPivot = transform.position + transform.forward * pivotDistance;
        }

        public bool ConsumeClick(out Vector2 position)
        { position = clickPosition; bool ready = clickPending; clickPending = false; return ready; }

        public void FocusActive()
        {
            if (!FocusTarget) return;
            OrbitPivot = FocusTarget.position;
            pivotDistance = FocusDistance;
            transform.position = OrbitPivot - transform.forward * pivotDistance;
            CharacterFocused = true;
        }

        public void Fly(Vector3 localDisplacement)
        {
            transform.position += transform.rotation * localDisplacement;
            CharacterFocused = false;
        }

        public void Pan(Vector2 pixels)
        {
            float scale = 2 * Mathf.Max(2, pivotDistance) * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * 0.5f)
                / Mathf.Max(1, view.pixelHeight);
            transform.position -= (transform.right * pixels.x + transform.up * pixels.y) * scale;
            CharacterFocused = false;
        }

        public void Dolly(float distance)
        { transform.position += transform.forward * distance; CharacterFocused = false; }

        public void BeginOrbit()
        {
            if (CharacterFocused && FocusTarget) OrbitPivot = FocusTarget.position;
            else
            {
                Ray ray = view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                OrbitPivot = Physics.Raycast(ray, out var hit, 250) ? hit.point : ray.GetPoint(Mathf.Max(2, pivotDistance));
            }
            pivotDistance = Mathf.Max(0.5f, Vector3.Distance(transform.position, OrbitPivot));
        }

        public void Orbit(Vector2 pixels)
        {
            if (CharacterFocused && FocusTarget) OrbitPivot = FocusTarget.position;
            Quaternion rotation = Rotated(pixels);
            transform.SetPositionAndRotation(OrbitPivot - rotation * Vector3.forward * pivotDistance, rotation);
        }

        public void Look(Vector2 pixels)
        { transform.rotation = Rotated(pixels); CharacterFocused = false; }

        private Quaternion Rotated(Vector2 pixels)
        {
            Vector3 angles = transform.eulerAngles;
            float pitch = Mathf.DeltaAngle(0, angles.x);
            return Quaternion.Euler(Mathf.Clamp(pitch - pixels.y * LookSensitivity, -85, 85),
                angles.y + pixels.x * LookSensitivity, 0);
        }

        private void Update()
        {
            clickPending = false;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame) FocusActive();
            if (mouse == null) return;
            Vector2 position = mouse.position.ReadValue(), delta = mouse.delta.ReadValue();
            bool overWorld = view.pixelRect.Contains(position) && !(PointerOverUI?.Invoke(position) ?? false);
            if (mouse.rightButton.wasPressedThisFrame && overWorld)
            { rotating = true; pan.Cancel(); }
            if (mouse.middleButton.wasPressedThisFrame && overWorld)
            { orbiting = true; rotating = false; pan.Cancel(); BeginOrbit(); }
            if (mouse.leftButton.wasPressedThisFrame && overWorld && !rotating && !orbiting) pan.Begin(position);
            if (pan.Held)
            {
                pan.Update(position);
                if (pan.Dragged && mouse.leftButton.isPressed) Pan(delta);
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    clickPending = pan.End(position) && overWorld;
                    clickPosition = position;
                }
            }
            if (orbiting && mouse.middleButton.isPressed) Orbit(delta);
            else if (rotating && mouse.rightButton.isPressed) Look(delta);
            if (!mouse.middleButton.isPressed) orbiting = false;
            if (!mouse.rightButton.isPressed) rotating = false;
            if (overWorld)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0) Dolly(scroll / 120f * ZoomSpeed);
            }
            if (keyboard != null && (overWorld || rotating || orbiting || pan.Held))
            {
                Vector3 direction = new Vector3((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.eKey.isPressed ? 1 : 0) - (keyboard.qKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                if (direction != Vector3.zero) Fly(direction.normalized * FlySpeed * Time.unscaledDeltaTime
                    * (keyboard.shiftKey.isPressed ? 2 : 1));
            }
        }

        private void OnApplicationFocus(bool focused) { if (!focused) ResetGestures(); }
        private void OnDisable() { ResetGestures(); }
        private void ResetGestures() { pan.Cancel(); rotating = orbiting = clickPending = false; }
    }
}
