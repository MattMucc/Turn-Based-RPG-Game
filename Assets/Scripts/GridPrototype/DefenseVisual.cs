using UnityEngine;

namespace Tenebrasia.GridPrototype
{
    // Prototype visual motion only. Never changes GridUnit.Position or movement budgets.
    internal sealed class DefenseVisual
    {
        private readonly Transform model;
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        public DefenseVisual(Transform actor)
        {
            model = actor.Find("Model");
            // Keep an actor without a separate model fixed; HUD cues still work.
            if (model) { position = model.localPosition; rotation = model.localRotation; }
        }
        public void Windup(float progress)
        {
            if (!model) return;
            model.localRotation = rotation * Quaternion.Euler(-18 * Mathf.Clamp01(progress), 0, 0);
        }
        public void React(DefenseChoice choice, float age)
        {
            if (!model) return;
            float amount = Mathf.Sin(Mathf.Clamp01(age / 0.3f) * Mathf.PI);
            if (choice == DefenseChoice.Dodge) model.localPosition = position + Vector3.right * (0.35f * amount);
            else model.localRotation = rotation * Quaternion.Euler(0, 0, 12 * amount);
        }
        public void Impact(float age)
        {
            if (model) model.localRotation = rotation * Quaternion.Euler(14 * (1 - Mathf.Clamp01(age / 0.2f)), 0, 0);
        }
        public void Reset() { if (model) { model.localPosition = position; model.localRotation = rotation; } }
    }
}
