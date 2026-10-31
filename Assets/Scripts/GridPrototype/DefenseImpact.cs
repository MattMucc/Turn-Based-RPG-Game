using System;

namespace Tenebrasia.GridPrototype
{
    public enum DefenseChoice { Dodge, Parry, Both }
    public enum DefenseResult { Waiting, Dodged, Parried, TooEarly, TooLate, Unparryable, ConflictingInput, Unable }

    // One input shared by this impact's targets; eligibility is evaluated per target.
    // Time is supplied by the encounter so this rule can be tested without Play Mode.
    public sealed class DefenseImpact
    {
        public float Duration { get; }
        public float DodgeWindow { get; }
        public float ParryWindow { get; }
        public bool Parryable { get; }
        public float InputLatencyCompensation { get; }
        public float ResolutionTime => Duration + InputLatencyCompensation;
        public float Elapsed { get; private set; }
        public bool Attempted { get; private set; }
        public bool Resolved { get; private set; }
        public float AttemptTime { get; private set; }
        public DefenseChoice Choice { get; private set; }
        public DefenseResult Result { get; private set; } = DefenseResult.Waiting;
        // The impact sits at 75%; the remaining track shows post-impact recovery.
        public const float ImpactBarPosition = 0.75f;
        public float BarPosition(float seconds) => Math.Min(1f, Math.Max(0f, seconds / Duration * ImpactBarPosition));
        public float DisplayTime => Attempted ? AttemptTime : Elapsed;
        public string AttemptFeedback
        {
            get
            {
                if (!Attempted) return "";
                float opens = Duration - (Choice == DefenseChoice.Dodge ? DodgeWindow : ParryWindow);
                if (Result == DefenseResult.TooEarly) return $"Too early ({Math.Ceiling(Math.Max(0, opens - AttemptTime) * 1000)} ms)";
                if (Result == DefenseResult.TooLate) return $"Too late ({Math.Ceiling(Math.Max(0, AttemptTime - Duration) * 1000)} ms)";
                return "";
            }
        }
        public bool Due => Elapsed >= ResolutionTime;

        public DefenseImpact(float duration, float dodgeWindow, float parryWindow, bool parryable = true, float inputLatencyCompensation = 0)
        {
            if (duration <= 0 || dodgeWindow <= 0 || parryWindow <= 0) throw new ArgumentOutOfRangeException();
            InputLatencyCompensation = Math.Max(0, Math.Min(0.1f, inputLatencyCompensation));
            Duration = duration; DodgeWindow = Math.Min(duration, dodgeWindow);
            ParryWindow = Math.Min(DodgeWindow, parryWindow); Parryable = parryable;
        }
        public void Advance(float seconds) { Elapsed += Math.Max(0, seconds); }
        public bool Attempt(DefenseChoice choice)
        {
            if (Attempted) return false;
            Attempted = true; Choice = choice; AttemptTime = Math.Max(0, Elapsed - InputLatencyCompensation);
            if (Resolved || AttemptTime > Duration) Result = DefenseResult.TooLate;
            else if (choice == DefenseChoice.Both) Result = DefenseResult.ConflictingInput;
            else if (choice == DefenseChoice.Parry && !Parryable) Result = DefenseResult.Unparryable;
            else if (AttemptTime < Duration - (choice == DefenseChoice.Dodge ? DodgeWindow : ParryWindow)) Result = DefenseResult.TooEarly;
            else Result = choice == DefenseChoice.Dodge ? DefenseResult.Dodged : DefenseResult.Parried;
            return true;
        }
        public bool Resolve()
        {
            if (Resolved || !Due) return false;
            Resolved = true;
            if (!Attempted) Result = DefenseResult.TooLate;
            return true;
        }
        public DefenseResult ForTarget(bool canReact) => canReact ? Result : DefenseResult.Unable;
        public bool AvoidsHit(bool canReact) => canReact && (Result == DefenseResult.Dodged || Result == DefenseResult.Parried);
        public int Damage(int incoming, bool blocking, bool canReact)
            => AvoidsHit(canReact) ? 0 : Math.Max(0, incoming) / (blocking ? 2 : 1);
    }
}
