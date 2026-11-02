using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.GridPrototype
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
        public System.Action<Vector2> ContextClick;
        public bool Transitioning { get; private set; }
        private Vector3 focusStart;
        private float focusElapsed;
        private bool poseTransition, shoulderView;
        private Quaternion poseStartRotation, poseEndRotation, tacticalRotation;
        private Vector3 poseEndPosition;
        private readonly GridPointerGesture rightGesture = new GridPointerGesture();
        public Vector3 OrbitPivot { get; private set; }
        public bool CharacterFocused { get; private set; }
        private Camera view;
        private bool rotating, orbiting, clickPending;
        private Vector2 clickPosition;
        private float pivotDistance = 15f;
        private Vector3 followedPosition;
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
            poseTransition = shoulderView = false;
            OrbitPivot = FocusTarget.position;
            pivotDistance = FocusDistance;
            transform.position = OrbitPivot - transform.forward * pivotDistance;
            followedPosition = FocusTarget.position;
            CharacterFocused = true; Transitioning = false;
        }

        public void SmoothFocus(Transform target)
        {
            if (!target) return;
            if (shoulderView)
            {
                poseTransition = true; poseStartRotation = transform.rotation; poseEndRotation = tacticalRotation;
                poseEndPosition = target.position - tacticalRotation * Vector3.forward * FocusDistance;
                shoulderView = false;
            }
            else poseTransition = false;
            FocusTarget = target; focusStart = transform.position; focusElapsed = 0;
            CharacterFocused = true; Transitioning = true; followedPosition = target.position;
        }
        public void SmoothDefenseFrame(Transform defender, Transform attacker)
        {
            if (!defender || !attacker) return;
            if (!view) view = GetComponent<Camera>();
            if (!view) return;
            Vector3 forward = attacker.position - defender.position; forward.y = 0;
            if (forward.sqrMagnitude < 0.001f) return;
            forward.Normalize(); Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (!shoulderView) tacticalRotation = transform.rotation;
            // Frame both full bodies from a raised three-quarter angle, then hold the shot.
            Bounds defenderBounds = ActorBounds(defender), attackerBounds = ActorBounds(attacker);
            Vector3 center = (defenderBounds.center + attackerBounds.center) * 0.5f;
            Vector3 outward = (right * 0.8f - forward * 0.6f + Vector3.up * 0.5f).normalized;
            Quaternion rotation = Quaternion.LookRotation(-outward, Vector3.up);
            float distance = FramingDistance(center, rotation, defenderBounds, attackerBounds);
            Vector3 desired = center + outward * distance;
            // Prefer the opposite three-quarter side if scenery blocks the usual side.
            if (Physics.SphereCast(center, 0.2f, outward, out var hit, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 alternate = (-right * 0.8f - forward * 0.6f + Vector3.up * 0.5f).normalized;
                Quaternion alternateRotation = Quaternion.LookRotation(-alternate, Vector3.up);
                float alternateDistance = FramingDistance(center, alternateRotation, defenderBounds, attackerBounds);
                if (!Physics.SphereCast(center, 0.2f, alternate, out var otherHit, alternateDistance, ~0, QueryTriggerInteraction.Ignore))
                    desired = center + alternate * alternateDistance;
                else
                    desired = center + outward * Mathf.Max(0.3f, hit.distance - 0.1f);
            }
            Vector3 look = center - desired;
            poseStartRotation = transform.rotation; poseEndRotation = Quaternion.LookRotation(look, Vector3.up);
            poseEndPosition = desired; focusStart = transform.position; focusElapsed = 0;
            FocusTarget = defender; followedPosition = defender.position;
            CharacterFocused = Transitioning = poseTransition = shoulderView = true;
        }
        private static Bounds ActorBounds(Transform actor)
        {
            var bounds = new Bounds(actor.position + Vector3.up * 0.9f, new Vector3(1, 1.8f, 1));
            bool found = false;
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || renderer is ParticleSystemRenderer || renderer is LineRenderer) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            bounds.Expand(0.4f); // Room for wind-up and in-place defensive motions.
            return bounds;
        }

        private float FramingDistance(Vector3 center, Quaternion rotation, params Bounds[] actors)
        {
            float vertical = Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * 0.5f) * 0.7f;
            float horizontal = vertical * Mathf.Max(0.1f, view.aspect);
            Quaternion inverse = Quaternion.Inverse(rotation);
            float distance = 4.5f;
            foreach (var bounds in actors)
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                            Vector3 local = inverse * (corner - center);
                            distance = Mathf.Max(distance, Mathf.Abs(local.x) / horizontal - local.z,
                                Mathf.Abs(local.y) / vertical - local.z);
                        }
            return distance;
        }

        public void ReleaseFocus() { CharacterFocused = false; Transitioning = poseTransition = shoulderView = false; }
        public void AdvanceFocus(float deltaTime)
        {
            if (!CharacterFocused || !FocusTarget) return;
            if (!Transitioning) { FollowFocusedTarget(); return; }
            focusElapsed += Mathf.Max(0, deltaTime);
            float t = Mathf.Clamp01(focusElapsed / 0.55f); t = t * t * (3 - 2 * t);
            OrbitPivot = FocusTarget.position;
            if (poseTransition)
            {
                if (!shoulderView) poseEndPosition = OrbitPivot - poseEndRotation * Vector3.forward * FocusDistance;
                transform.SetPositionAndRotation(Vector3.Lerp(focusStart, poseEndPosition, t), Quaternion.Slerp(poseStartRotation, poseEndRotation, t));
                pivotDistance = Vector3.Distance(transform.position, OrbitPivot);
            }
            else
            {
                pivotDistance = FocusDistance;
                transform.position = Vector3.Lerp(focusStart, OrbitPivot - transform.forward * FocusDistance, t);
            }
            followedPosition = FocusTarget.position;
            if (focusElapsed >= 0.55f) Transitioning = poseTransition = false;
        }

        public void FollowFocusedTarget()
        {
            if (!CharacterFocused || !FocusTarget || Transitioning || shoulderView) return;
            Vector3 delta = FocusTarget.position - followedPosition;
            transform.position += delta; OrbitPivot += delta; followedPosition = FocusTarget.position;
        }
        private void LateUpdate() => AdvanceFocus(Time.unscaledDeltaTime);

        public void FrameAction(System.Collections.Generic.IReadOnlyList<Vector3> points)
        {
            if (points == null || points.Count == 0) return;
            if (!view) view = GetComponent<Camera>();
            if (!view) return;
            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (var point in points) bounds.Encapsulate(point);
            float halfFov = Mathf.Atan(Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * 0.5f) * Mathf.Min(1, view.aspect));
            pivotDistance = Mathf.Max(FocusDistance, (bounds.extents.magnitude + 2) / Mathf.Sin(halfFov) * 1.25f);
            OrbitPivot = bounds.center;
            transform.position = OrbitPivot - transform.forward * pivotDistance;
            ReleaseFocus();
        }

        public void Fly(Vector3 localDisplacement)
        {
            transform.position += transform.rotation * localDisplacement;
            ReleaseFocus();
        }

        public void Pan(Vector2 pixels)
        {
            float scale = 2 * Mathf.Max(2, pivotDistance) * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * 0.5f)
                / Mathf.Max(1, view.pixelHeight);
            transform.position -= (transform.right * pixels.x + transform.up * pixels.y) * scale;
            ReleaseFocus();
        }

        public void Dolly(float distance)
        { transform.position += transform.forward * distance; ReleaseFocus(); }

        public void BeginOrbit()
        {
            Transitioning = poseTransition = shoulderView = false; if (FocusTarget) followedPosition = FocusTarget.position;
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
            FollowFocusedTarget();
            if (CharacterFocused && FocusTarget) OrbitPivot = FocusTarget.position;
            Quaternion rotation = Rotated(pixels);
            transform.SetPositionAndRotation(OrbitPivot - rotation * Vector3.forward * pivotDistance, rotation);
        }

        public void Look(Vector2 pixels)
        { transform.rotation = Rotated(pixels); ReleaseFocus(); }

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
            { rotating = true; pan.Cancel(); rightGesture.Begin(position); }
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
            else if (rotating && mouse.rightButton.isPressed)
            { rightGesture.Update(position); if (rightGesture.Dragged) Look(delta); }
            if (mouse.rightButton.wasReleasedThisFrame && rightGesture.Held)
            { if (rightGesture.End(position) && overWorld) ContextClick?.Invoke(position); }
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
        private void ResetGestures() { pan.Cancel(); rightGesture.Cancel(); rotating = orbiting = clickPending = false; }
    }
}
