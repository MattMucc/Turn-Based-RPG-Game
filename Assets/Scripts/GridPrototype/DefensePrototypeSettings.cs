using UnityEngine;

namespace Game.GridPrototype
{
    // Temporary tuning, not final combat balance. Stored on the possessed character.
    public sealed class DefensePrototypeSettings : MonoBehaviour
    {
        [Min(0.02f)] public float DodgeWindow = 0.30f;
        [Min(0.02f)] public float ParryWindow = 0.12f;
        [Min(1)] public float AssistanceMultiplier = 1.75f;
        public bool Assisted;
        [Tooltip("Compensates display/input latency without widening the defense window. Set to zero to disable.")]
        [Range(0, 0.1f)] public float InputLatencyCompensation = 0f;
        [Min(0.5f)] public float SingleWindup = 1.4f;
        [Min(0.5f)] public float ComboFirstWindup = 1.2f;
        [Min(0.5f)] public float ComboSecondWindup = 0.9f;
        [Min(0.5f)] public float ComboThirdWindup = 1.1f;
        [Header("Authored impact positions on timing bar")]
        [Tooltip("Normalized bar position of impact. Windup remains the time in seconds until impact; windows use that same clock.")]
        [Range(0.2f, 0.95f)] public float SingleImpactPosition = 0.75f;
        [Range(0.2f, 0.95f)] public float ComboFirstImpactPosition = 0.60f;
        [Range(0.2f, 0.95f)] public float ComboSecondImpactPosition = 0.85f;
        [Range(0.2f, 0.95f)] public float ComboThirdImpactPosition = 0.70f;
        public float ImpactPositionFor(bool combo, int hit) => !combo ? SingleImpactPosition
            : hit == 0 ? ComboFirstImpactPosition : hit == 1 ? ComboSecondImpactPosition : ComboThirdImpactPosition;
        [Min(0)] public int SingleDamage = 25;
        [Min(0)] public int ComboHitDamage = 10;
        [Min(0)] public int CounterDamage = 25;
        [Range(0, 1)] public float CueVolume = 0.15f;
        [Header("Defense feedback")]
        public bool ParrySparks = true;
        public bool DodgeSlowMotion = true;
        [Range(0, 0.4f)] public float DodgeSlowMotionDuration = 0.18f;
        [Range(0.05f, 1)] public float DodgeSlowMotionSpeed = 0.15f;
        public DefenseImpact CreateImpact(float windup, bool parryable = true, float impactPosition = DefenseImpact.ImpactBarPosition)
        {
            float multiplier = Assisted ? Mathf.Max(1, AssistanceMultiplier) : 1;
            return new DefenseImpact(Mathf.Max(0.5f, windup), Mathf.Max(0.02f, DodgeWindow) * multiplier,
                Mathf.Max(0.02f, ParryWindow) * multiplier, parryable, InputLatencyCompensation, impactPosition);
        }
    }
}
