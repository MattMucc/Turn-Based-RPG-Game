using System.Collections.Generic;
using UnityEngine;

// Loose formation in open ground; recorded leader path is the fallback around scenery.
[RequireComponent(typeof(CharacterController))]
public sealed class PartyFollower : MonoBehaviour
{
    [SerializeField, Min(1)] private float followDistance = 2.2f;
    [SerializeField, Min(.1f)] private float walkSpeed = 6f;
    [SerializeField, Min(.1f)] private float catchUpSpeed = 9f;
    [SerializeField, Min(.05f)] private float trailSpacing = .2f;
    [SerializeField] private float gravity = -30f;
    [SerializeField] private Transform leader;
    [SerializeField] private float sideOffset;
    [SerializeField, Min(.1f)] private float settleRadius = .6f;
    private Vector3 velocity;
    private bool settled, manualWaiting;
    private Transform waitingPoint;
    private Vector3 waitingPosition;
    public void SetCompanionSlot(int slot) { sideOffset = slot % 2 == 0 ? -1.8f : 1.8f; followDistance = slot % 2 == 0 ? 2.6f : 3.2f; }
    public void WaitAt(Transform point) { manualWaiting = true; waitingPoint = point; waitingPosition = point ? point.position : transform.position; }
    public void ResumeFollowing() { manualWaiting = false; waitingPoint = null; ResetTrail(); }
    public Transform Leader => leader;
    public bool WaitingForSafeRoute { get; private set; }
    private CharacterController controller;
    private readonly List<Vector3> trail = new();
    private Vector3 lastRecorded;
    private float verticalSpeed;
    private bool routeInterrupted;

    public void Follow(Transform target) { leader = target; ResetTrail(); }
    public void ResetTrail()
    {
        trail.Clear(); verticalSpeed = 0; velocity = Vector3.zero; settled = false; routeInterrupted = false; WaitingForSafeRoute = false;
        if (leader) { lastRecorded = leader.position; trail.Add(lastRecorded); }
    }
    private void OnEnable() => ResetTrail();
    private void Update()
    {
        if (!Application.isFocused || (BattleManager.Instance && BattleManager.Instance.InBattle)
            || (GameManager.Instance && GameManager.Instance.WorldInputBlocked)) return;
        StepFollow(Time.deltaTime);
    }
    public void StepFollow(float dt)
    {
        if (!isActiveAndEnabled || !leader || dt <= 0 || (BattleManager.Instance && BattleManager.Instance.InBattle)) return;
        var player = GetComponent<PlayerMovement>(); if (player && player.IsPossessed) return;
        if (!controller) controller = GetComponent<CharacterController>();
        if (!controller || !controller.enabled) return;
        dt = Mathf.Min(dt, .1f);
        if (trail.Count == 0) ResetTrail();
        float change = Vector3.Distance(lastRecorded, leader.position);
        // Teleports and an exhausted route buffer must not drag companions through the world.
        if (change > 8 || trail.Count >= 2048) routeInterrupted = true;
        if (!routeInterrupted && change >= trailSpacing) { trail.Add(leader.position); lastRecorded = leader.position; }
        while (trail.Count > 1 && Horizontal(trail[0] - transform.position).magnitude < .18f) trail.RemoveAt(0);
        float remaining = Horizontal(trail[0] - transform.position).magnitude;
        for (int i = 1; i < trail.Count; i++) remaining += Horizontal(trail[i] - trail[i - 1]).magnitude;
        remaining += Horizontal(leader.position - lastRecorded).magnitude;
        Vector3 delta = Horizontal(trail[0] - transform.position);
        float step = Mathf.Min(delta.magnitude, Mathf.Max(0, remaining - followDistance), (remaining > followDistance + 4 ? catchUpSpeed : walkSpeed) * dt);
        Vector3 motion = delta.sqrMagnitude > .0001f ? delta.normalized * step : Vector3.zero;
        Vector3 goal = manualWaiting ? (waitingPoint ? waitingPoint.position : waitingPosition) : leader.position - Horizontal(leader.forward).normalized * followDistance + Horizontal(leader.right).normalized * sideOffset;
        bool direct = ClearWalkLine(goal);
        if (direct && !manualWaiting)
        {
            // Side-by-side followers rarely touch a breadcrumb exactly. Discard the
            // passed part of the route so an obstacle never sends them back to spawn.
            int closest = 0; float distance = float.PositiveInfinity;
            for (int i = 0; i < trail.Count; i++)
            {
                float candidate = Horizontal(trail[i] - transform.position).sqrMagnitude;
                if (candidate <= distance) { closest = i; distance = candidate; }
            }
            if (closest > 0) trail.RemoveRange(0, closest);
        }
        if (direct || manualWaiting)
        {
            delta = Horizontal(goal - transform.position);
            if (delta.magnitude < settleRadius) settled = true;
            else if (delta.magnitude > settleRadius + .5f) settled = false;
            Vector3 desired = settled || !direct ? Vector3.zero : delta.normalized * Mathf.Min(walkSpeed + Mathf.Max(0, delta.magnitude - 4), catchUpSpeed);
            if (GameManager.Instance && !settled)
                foreach (var member in GameManager.Instance.GetBattleParty())
                {
                    if (!member || member.transform == transform) continue;
                    Vector3 away = Horizontal(transform.position - member.transform.position);
                    if (away.sqrMagnitude > .01f && away.magnitude < 1.4f) desired += away.normalized * (1.4f - away.magnitude) * 2;
                }
            velocity = Vector3.MoveTowards(velocity, desired, 18 * dt);
            motion = Vector3.ClampMagnitude(velocity * dt, delta.magnitude);
            if (settled) motion = Vector3.zero;
            step = motion.magnitude;
        }
        else velocity = motion / dt;
        WaitingForSafeRoute = (!direct && (routeInterrupted || manualWaiting)) || (step > 0 && !SupportedAhead(motion));
        if (WaitingForSafeRoute) motion = Vector3.zero;
        verticalSpeed = controller.isGrounded && verticalSpeed <= 0 ? -2 : Mathf.Max(-30, verticalSpeed + gravity * dt);
        var flags = controller.Move(motion + Vector3.up * verticalSpeed * dt);
        if ((flags & CollisionFlags.Below) != 0) verticalSpeed = -2;
        if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
        if ((flags & CollisionFlags.Sides) != 0 && step > 0) WaitingForSafeRoute = true;
        if (motion.sqrMagnitude > .0001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(motion), 1 - Mathf.Exp(-12 * dt));
    }
    private static Vector3 Horizontal(Vector3 delta) { delta.y = 0; return delta; }
    private bool ClearWalkLine(Vector3 destination)
    {
        Vector3 delta = Horizontal(destination - transform.position);
        if (delta.magnitude > 12) return false;
        Vector3 foot = transform.TransformPoint(controller.center) - Vector3.up * controller.height * .5f;
        int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .4f));
        for (int i = 1; i <= steps; i++)
        {
            Vector3 point = foot + (destination - transform.position) * (i / (float)steps);
            if (!GroundSupported(point, point.y)) return false;
        }
        if (delta.sqrMagnitude < .001f) return true;
        Vector3 bottom = foot + Vector3.up * (controller.radius + .08f), top = foot + Vector3.up * (controller.height - controller.radius);
        foreach (var hit in Physics.CapsuleCastAll(bottom, top, controller.radius * .9f, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.collider.transform.IsChildOf(transform) && !hit.collider.GetComponentInParent<PlayerMovement>()) return false;
        return true;
    }
    private bool SupportedAhead(Vector3 motion)
    {
        Vector3 foot = transform.TransformPoint(controller.center) - Vector3.up * (controller.height * .5f * Mathf.Abs(transform.lossyScale.y));
        Vector3 point = foot + motion + motion.normalized * (controller.radius + .1f);
        return GroundSupported(point, foot.y);
    }
    private bool GroundSupported(Vector3 point, float height)
    {
        var hits = Physics.RaycastAll(point + Vector3.up * .5f, Vector3.down, 1.1f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<PlayerMovement>()) continue;
            return hit.collider.gameObject.layer != LayerMask.NameToLayer("Water") && Vector3.Angle(hit.normal, Vector3.up) <= controller.slopeLimit && hit.point.y - height <= controller.stepOffset + .1f;
        }
        return false;
    }
}
