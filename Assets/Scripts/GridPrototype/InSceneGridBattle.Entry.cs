using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public sealed partial class InSceneGridBattle
    {
        private BattleEntryPlan entryPlan;
        private float entryElapsed;
        private int entryFinishedFrame = -1;
        private Vector3 entryCameraStart, entryCameraEnd;
        private Quaternion entryCameraRotation, entryCameraEndRotation;
        private float entryCameraFov;
        private Quaternion[] entryRotations;
        public bool EntryActive => entryPlan != null;
        public float EntryProgress => entryPlan == null ? 1 : Mathf.Clamp01(entryElapsed / Mathf.Max(.01f, entryPlan.Duration));
        public float EntryHudAlpha => EntryActive ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.72f, 1, EntryProgress)) : 1;

        private void BeginEntry(BattleEntryPlan plan, Camera source, IReadOnlyList<Quaternion> originalRotations)
        {
            entryPlan = plan; entryElapsed = 0; playerTurning = false; busy = true;
            entryRotations = new Quaternion[actors.Count];
            entryCameraStart = source.transform.position; entryCameraRotation = source.transform.rotation; entryCameraFov = source.fieldOfView;
            view.transform.SetPositionAndRotation(entryCameraStart, entryCameraRotation); view.fieldOfView = entryCameraFov;
            cameraControls.ReleaseFocus(); cameraControls.enabled = false;
            var bounds = new Bounds(Grid.Point(plan.Cells[0]) + Vector3.up, Vector3.one * 2);
            for (int i = 0; i < actors.Count; i++)
            {
                entryRotations[i] = originalRotations[i];
                bounds.Encapsulate(Grid.Point(plan.Cells[i]) + Vector3.up * 2.2f);
                bounds.Encapsulate(Grid.Point(plan.Cells[i]));
                actors[i].position = plan.Routes[i][0] + Vector3.up * offsets[i];
                actors[i].rotation = entryRotations[i];
                if (!plan.PreserveWorldPositions && i > 0 && i < allyCount) actors[i].gameObject.SetActive(false);
            }
            Vector3 outward = (-plan.Facing + Vector3.Cross(Vector3.up, plan.Facing) * .45f + Vector3.up * 1.15f).normalized;
            entryCameraEndRotation = Quaternion.LookRotation(-outward, Vector3.up);
            Quaternion inverse = Quaternion.Inverse(entryCameraEndRotation);
            float vertical = Mathf.Tan(55 * Mathf.Deg2Rad * .5f) * .78f, horizontal = vertical * Mathf.Max(.3f, view.aspect);
            float distance = 10;
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                Vector3 local = inverse * Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                distance = Mathf.Max(distance, Mathf.Abs(local.x) / horizontal - local.z, Mathf.Abs(local.y) / vertical - local.z);
            }
            entryCameraEnd = bounds.center + outward * distance;
            overlay.Show(new Dictionary<Vector2Int, GridRoute>(), new Vector2Int(-1, -1)); overlay.SetOpacity(0);
            message = "Taking battle positions…";
        }

        // Also driven directly by EditMode checks; never starts Play Mode.
        public void AdvanceEntry(float seconds)
        {
            if (!EntryActive || closing) return;
            entryElapsed += Mathf.Max(0, seconds);
            float t = EntryProgress, eased = Mathf.SmoothStep(0, 1, t);
            view.transform.SetPositionAndRotation(Vector3.Lerp(entryCameraStart, entryCameraEnd, eased), Quaternion.Slerp(entryCameraRotation, entryCameraEndRotation, eased));
            view.fieldOfView = Mathf.Lerp(entryCameraFov, 55, eased);
            for (int i = 0; i < actors.Count; i++)
            {
                bool companion = !entryPlan.PreserveWorldPositions && i > 0 && i < allyCount;
                float travel = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(companion ? .12f : 0, .8f, t));
                if (companion) actors[i].gameObject.SetActive(t >= .12f);
                actors[i].position = BattleFormation.RoutePosition(entryPlan.Routes[i], travel) + Vector3.up * offsets[i];
                Vector3 facing = IsAlly(i) ? entryPlan.Facing : Grid.Point(entryPlan.Cells[0]) - Grid.Point(entryPlan.Cells[i]); facing.y = 0;
                Quaternion target = facing.sqrMagnitude < .001f ? entryRotations[i] : IsAlly(i) ? Quaternion.LookRotation(facing) : GridTargeting.Facing(facing);
                actors[i].rotation = Quaternion.Slerp(entryRotations[i], target, travel);
            }
            overlay.SetOpacity(Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, .8f, t)));
            if (t < 1) return;
            entryPlan = null; entryFinishedFrame = Time.frameCount; cameraControls.enabled = true; cameraControls.ReleaseFocus();
            StartOpeningTurn(); TurnStarted?.Invoke();
        }
    }
}
