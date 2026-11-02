using UnityEngine;

namespace Game.GridPrototype
{
    // Encounter-local clock: never changes global Time.timeScale or camera input.
    public sealed class DefenseSlowMotion
    {
        public float Remaining { get; private set; }
        private float multiplier = 1;
        public void Begin(float duration, float speed)
        { Remaining = Mathf.Max(0, duration); multiplier = Mathf.Clamp(speed, 0.05f, 1); }
        public float Advance(float delta)
        {
            delta = Mathf.Max(0, delta);
            float slowed = Mathf.Min(delta, Remaining);
            Remaining -= slowed;
            return slowed * multiplier + (delta - slowed);
        }
        public void Clear() { Remaining = 0; multiplier = 1; }
    }
}
