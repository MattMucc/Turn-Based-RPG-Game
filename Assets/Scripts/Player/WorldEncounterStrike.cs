using UnityEngine;

public static class WorldEncounterStrike
{
    public static EnemyTrigger FindTarget(Transform attacker, float reach, float halfAngle)
    {
        Vector3 origin = attacker.position + Vector3.up * .5f;
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
