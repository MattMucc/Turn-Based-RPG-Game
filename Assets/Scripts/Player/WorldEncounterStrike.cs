using UnityEngine;

public static class WorldEncounterStrike
{
    public static Vector3 Origin(Transform attacker) => attacker.position + Vector3.up * .5f;

    public static void DrawDebug(Transform attacker, float reach, float halfAngle)
    {
        Color previous = Gizmos.color;
        Vector3 origin = Origin(attacker);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, reach);
        Gizmos.color = Color.yellow;
        // Range is tested against collider surfaces; angle is tested against the enemy pivot.
        // Draw both filters rather than imply a single box-shaped collider exists.
        Vector3 forward = Vector3.ProjectOnPlane(attacker.forward, Vector3.up).normalized;
        Vector3 last = origin + Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward * reach;
        Gizmos.DrawLine(origin, last);
        const int segments = 48;
        for (int i = 1; i <= segments; i++)
        {
            Vector3 next = origin + Quaternion.AngleAxis(Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments), Vector3.up) * forward * reach;
            Gizmos.DrawLine(last, next); last = next;
        }
        Gizmos.DrawLine(origin, last);
        Gizmos.DrawLine(origin, origin + forward * reach);
        var target = FindTarget(attacker, reach, halfAngle);
        if (target)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, target.transform.position);
            Gizmos.DrawWireSphere(target.transform.position, .15f);
        }
        Gizmos.color = previous;
    }

    public static EnemyTrigger FindTarget(Transform attacker, float reach, float halfAngle)
    {
        Vector3 origin = Origin(attacker);
        EnemyTrigger best = null; float nearest = float.PositiveInfinity;
        foreach (var collider in Physics.OverlapSphere(origin, reach, ~0, QueryTriggerInteraction.Ignore))
        {
            var enemy = collider.GetComponentInParent<EnemyTrigger>();
            if (!enemy || !enemy.enabled || !enemy.BattleGroup) continue;
            Vector3 point = collider.ClosestPoint(origin);
            Vector3 delta = point - origin;
            Vector3 facing = enemy.transform.position - attacker.position; facing.y = 0;
            if (delta.sqrMagnitude >= nearest || Vector3.Angle(attacker.forward, facing) > halfAngle) continue;
            bool blocked = false;
            foreach (var hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(attacker) && !hit.transform.IsChildOf(enemy.transform)) { blocked = true; break; }
            if (blocked) continue;
            nearest = delta.sqrMagnitude; best = enemy;
        }
        return best;
    }
}
