using System;
using UnityEngine.InputSystem;

namespace Game.GridPrototype
{
    public enum QteMode { SinglePress, FixedSequence, RepeatedTap, Hold, Staged, DualInput }
    public enum QteButton { South, East, West, North, DpadUp, DpadRight, DpadDown, DpadLeft, StickUp, StickRight, StickDown, StickLeft }

    public enum PressQteResult { Preparing, Waiting, Success, WrongInput, TimedOut, StageFeedback }

    // A deadline prompt, not a narrow timing-band test. Inputs are fresh press edges.
    public sealed class PressQte
    {
        private readonly QteButton[] sequence;
        public int StageIndex { get; private set; }
        public int SuccessfulStages { get; private set; }
        public PressQteResult LastStageResult { get; private set; }
        private PressQteResult stagedFailure = PressQteResult.WrongInput;
        private float feedbackRemaining;
        public static QteButton[] Normalize(QteMode mode, QteButton[] inputs)
        {
            if (mode == QteMode.DualInput)
            {
                QteButton direction = inputs != null && inputs.Length > 0 && inputs[0] >= QteButton.StickUp && inputs[0] <= QteButton.StickLeft ? inputs[0] : QteButton.StickRight;
                QteButton face = inputs != null && inputs.Length > 1 && inputs[1] >= QteButton.South && inputs[1] <= QteButton.North ? inputs[1] : QteButton.South;
                return new[] { direction, face };
            }
            if (mode == QteMode.SinglePress || inputs == null || inputs.Length == 0) return new[] { QteButton.South };
            int count = Math.Min(inputs.Length, mode == QteMode.FixedSequence || mode == QteMode.Staged ? 8 : 1);
            var copy = new QteButton[count];
            for (int i = 0; i < count; i++)
            {
                var value = Enum.IsDefined(typeof(QteButton), inputs[i]) ? inputs[i] : QteButton.South;
                copy[i] = mode == QteMode.FixedSequence && value >= QteButton.StickUp ? (QteButton)((int)value - 4) : value;
            }
            return copy;
        }
        public QteMode Mode { get; }
        public int RequiredTaps { get; }
        public float HoldDuration { get; }
        public float ProgressDrainPerSecond { get; }
        public float TapDrainDelay { get; }
        private float tapCharge, tapIdleTime;
        public float HeldTime { get; private set; }
        private bool holdStarted;
        public bool InputHeld { get; private set; }
        public bool SecondaryHeld { get; private set; }
        public QteButton Secondary => Mode == QteMode.DualInput ? sequence[1] : QteButton.South;
        public float HoldProgress => Math.Min(1, HeldTime / HoldDuration);
        public float TapProgress => Mode == QteMode.RepeatedTap ? tapCharge : 0;
        public int CompletedInputs { get; private set; }
        public int InputCount => sequence.Length;
        public QteButton InputAt(int index) => sequence[index];
        public QteButton Expected => sequence[Mode == QteMode.Staged ? StageIndex : Mode == QteMode.RepeatedTap || Mode == QteMode.Hold || Mode == QteMode.DualInput ? 0 : Math.Min(CompletedInputs, sequence.Length - 1)];
        public static string Label(QteButton button, bool controller) => controller
            ? button == QteButton.South ? "A" : button == QteButton.East ? "B" : button == QteButton.West ? "X" : "Y"
            : button == QteButton.South ? "SPACE" : button == QteButton.East ? "RIGHT" : button == QteButton.West ? "LEFT" : "UP";
        public float Duration { get; }
        public float Remaining { get; private set; }
        public float PreparationRemaining { get; private set; }
        public PressQteResult Result { get; private set; }
        public bool Complete => Result == PressQteResult.Success || Result == PressQteResult.WrongInput || Result == PressQteResult.TimedOut;
        public float Fraction => Remaining / Duration;
        public PressQte(float duration, float preparation = 0.5f, QteButton[] inputs = null, QteMode? mode = null, int requiredTaps = 12, float holdDuration = 2, float progressDrainPerSecond = 0.2f, float tapDrainDelay = 0.3f)
        {
            Mode = mode ?? (inputs != null && inputs.Length > 0 ? QteMode.FixedSequence : QteMode.SinglePress);
            ProgressDrainPerSecond = Math.Max(0, progressDrainPerSecond);
            TapDrainDelay = Math.Max(0, tapDrainDelay);
            HoldDuration = Math.Max(0.1f, holdDuration);
            RequiredTaps = Math.Max(1, requiredTaps);
            sequence = Normalize(Mode, inputs);
            Duration = Math.Max(0.1f, duration); Remaining = Duration;
            PreparationRemaining = Math.Max(0, preparation);
            Result = PreparationRemaining > 0 ? PressQteResult.Preparing : PressQteResult.Waiting;
        }
        public void Advance(float deltaTime, bool correctPress = false, bool wrongPress = false, bool held = false, bool secondaryHeld = false)
        {
            InputHeld = held; SecondaryHeld = secondaryHeld;
            if (Complete) return;
            deltaTime = Math.Max(0, deltaTime);
            if (Result == PressQteResult.Preparing)
            {
                PreparationRemaining = Math.Max(0, PreparationRemaining - deltaTime);
                if (PreparationRemaining == 0) Result = PressQteResult.Waiting;
                return; // Opening frame always displays the full countdown; no carried launch input.
            }
            if (Mode == QteMode.Staged)
            {
                if (Result == PressQteResult.StageFeedback)
                {
                    feedbackRemaining -= deltaTime;
                    if (feedbackRemaining <= 0) { StageIndex++; Remaining = Duration; Result = PressQteResult.Waiting; InputHeld = false; }
                    return; // Never reuse the previous prompt's input for the new prompt.
                }
                if (wrongPress) FinishStage(PressQteResult.WrongInput);
                else if (correctPress) FinishStage(PressQteResult.Success);
                else { Remaining = Math.Max(0, Remaining - deltaTime); if (Remaining == 0) FinishStage(PressQteResult.TimedOut); }
                return;
            }
            // Judge the press against the time displayed, before advancing this frame.
            if (wrongPress) { Result = PressQteResult.WrongInput; return; }
            if (Mode == QteMode.DualInput)
            {
                // Either half may already be held, but a new activation after preparation must complete the pair.
                if (correctPress && held && secondaryHeld) { CompletedInputs = 2; Result = PressQteResult.Success; return; }
                Remaining = Math.Max(0, Remaining - deltaTime);
                if (Remaining == 0) Result = PressQteResult.TimedOut;
                return;
            }
            if (Mode == QteMode.Hold)
            {
                // A fresh press starts/resumes charging; release drains gradually rather than clearing progress.
                if (correctPress) holdStarted = true;
                if (!held) { holdStarted = false; HeldTime = Math.Max(0, HeldTime - ProgressDrainPerSecond * HoldDuration * Math.Min(deltaTime, Remaining)); }
                else if (holdStarted) HeldTime = Math.Min(HoldDuration, HeldTime + Math.Min(deltaTime, Remaining));
                if (HeldTime >= HoldDuration) { Result = PressQteResult.Success; return; }
                Remaining = Math.Max(0, Remaining - deltaTime);
                if (Remaining == 0) Result = PressQteResult.TimedOut;
                return;
            }
            if (Mode == QteMode.RepeatedTap)
            {
                if (correctPress)
                {
                    CompletedInputs++; tapIdleTime = 0;
                    tapCharge = Math.Min(1, tapCharge + 1f / RequiredTaps);
                    if (tapCharge >= 1 - 0.000001f) { tapCharge = 1; Result = PressQteResult.Success; return; }
                }
                else
                {
                    float before = Math.Max(0, tapIdleTime - TapDrainDelay);
                    tapIdleTime += Math.Min(deltaTime, Remaining);
                    float drainTime = Math.Max(0, tapIdleTime - TapDrainDelay) - before;
                    tapCharge = Math.Max(0, tapCharge - ProgressDrainPerSecond * drainTime);
                }
                Remaining = Math.Max(0, Remaining - deltaTime);
                if (Remaining == 0) Result = PressQteResult.TimedOut;
                return;
            }
            if (correctPress)
            {
                CompletedInputs++;
                if (CompletedInputs == (Mode == QteMode.RepeatedTap ? RequiredTaps : sequence.Length)) { Result = PressQteResult.Success; return; }
            }
            Remaining = Math.Max(0, Remaining - deltaTime);
            if (Remaining == 0) Result = PressQteResult.TimedOut;
        }
        private void FinishStage(PressQteResult result)
        {
            LastStageResult = result;
            if (result == PressQteResult.Success) { SuccessfulStages++; CompletedInputs++; }
            else stagedFailure = result;
            if (StageIndex == sequence.Length - 1) Result = SuccessfulStages == sequence.Length ? PressQteResult.Success : stagedFailure;
            else { Result = PressQteResult.StageFeedback; feedbackRemaining = 0.35f; }
        }

    }

    // First prototype binding: Space / south face button. Other authored bindings come later.
    public sealed class PressQteInput
    {
        private int heldDirections;
        public void Reset(Gamepad pad) { heldDirections = Directions(pad); }
        private static int Directions(Gamepad pad)
        {
            if (pad == null) return 0;
            var left = pad.leftStick.ReadValue(); var right = pad.rightStick.ReadValue();
            return (left.x > 0.65f ? 1 : 0) | (left.x < -0.65f ? 2 : 0)
                | (left.y > 0.65f ? 4 : 0) | (left.y < -0.65f ? 8 : 0)
                | (right.x > 0.65f ? 16 : 0) | (right.x < -0.65f ? 32 : 0)
                | (right.y > 0.65f ? 64 : 0) | (right.y < -0.65f ? 128 : 0);
        }
        public static Key KeyboardKey(QteButton expected)
        {
            switch (expected)
            {
                case QteButton.South: return Key.Space;
                case QteButton.East: case QteButton.DpadRight: return Key.RightArrow;
                case QteButton.West: case QteButton.DpadLeft: return Key.LeftArrow;
                case QteButton.North: case QteButton.DpadUp: return Key.UpArrow;
                case QteButton.DpadDown: return Key.DownArrow;
                case QteButton.StickUp: return Key.W; case QteButton.StickRight: return Key.D;
                case QteButton.StickDown: return Key.S; default: return Key.A;
            }
        }
        private static UnityEngine.InputSystem.Controls.ButtonControl Button(Gamepad pad, QteButton expected)
        {
            if (pad == null) return null;
            switch (expected)
            {
                case QteButton.South: return pad.buttonSouth; case QteButton.East: return pad.buttonEast;
                case QteButton.West: return pad.buttonWest; case QteButton.North: return pad.buttonNorth;
                case QteButton.DpadUp: return pad.dpad.up; case QteButton.DpadRight: return pad.dpad.right;
                case QteButton.DpadDown: return pad.dpad.down; case QteButton.DpadLeft: return pad.dpad.left;
                default: return null;
            }
        }
        private static int StickMask(QteButton expected) => expected == QteButton.StickUp ? 4 : expected == QteButton.StickRight ? 1
            : expected == QteButton.StickDown ? 8 : expected == QteButton.StickLeft ? 2 : 0;
        public static bool IsHeld(Keyboard keyboard, Gamepad pad, QteButton expected)
            => (keyboard != null && keyboard[KeyboardKey(expected)].isPressed) || (Button(pad, expected)?.isPressed ?? false)
                || (StickMask(expected) != 0 && (Directions(pad) & StickMask(expected)) != 0);
        public void Read(Keyboard keyboard, Gamepad pad, out bool correct, out bool wrong, QteButton expected = QteButton.South, QteButton? secondary = null)
        {
            Key expectedKey = KeyboardKey(expected);
            var expectedButton = Button(pad, expected);
            var secondButton = secondary.HasValue ? Button(pad, secondary.Value) : null;
            Key secondKey = secondary.HasValue ? KeyboardKey(secondary.Value) : Key.None;
            correct = (keyboard != null && keyboard[expectedKey].wasPressedThisFrame)
                || (expectedButton != null && expectedButton.wasPressedThisFrame)
                || (secondary.HasValue && keyboard != null && keyboard[secondKey].wasPressedThisFrame)
                || (secondButton != null && secondButton.wasPressedThisFrame);
            wrong = false;
            if (keyboard != null)
                foreach (var key in keyboard.allKeys)
                    if (key.wasPressedThisFrame && key.keyCode != expectedKey && key.keyCode != secondKey
                        && key.keyCode != Key.LeftShift && key.keyCode != Key.RightShift
                        && key.keyCode != Key.LeftCtrl && key.keyCode != Key.RightCtrl
                        && key.keyCode != Key.LeftAlt && key.keyCode != Key.RightAlt
                        && key.keyCode != Key.LeftMeta && key.keyCode != Key.RightMeta)
                        wrong = true;
            if (pad != null)
            {
                wrong |= (expected != QteButton.South && secondary != QteButton.South && pad.buttonSouth.wasPressedThisFrame)
                    || (expected != QteButton.East && secondary != QteButton.East && pad.buttonEast.wasPressedThisFrame)
                    || (expected != QteButton.West && secondary != QteButton.West && pad.buttonWest.wasPressedThisFrame)
                    || (expected != QteButton.North && secondary != QteButton.North && pad.buttonNorth.wasPressedThisFrame)
                    || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame
                    || pad.leftTrigger.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame
                    || pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame
                    || (expected != QteButton.DpadUp && secondary != QteButton.DpadUp && pad.dpad.up.wasPressedThisFrame)
                    || (expected != QteButton.DpadDown && secondary != QteButton.DpadDown && pad.dpad.down.wasPressedThisFrame)
                    || (expected != QteButton.DpadLeft && secondary != QteButton.DpadLeft && pad.dpad.left.wasPressedThisFrame)
                    || (expected != QteButton.DpadRight && secondary != QteButton.DpadRight && pad.dpad.right.wasPressedThisFrame);
            }
            // Start/Select and mouse controls are reserved, not failed gameplay attempts.
            int directions = Directions(pad), fresh = directions & ~heldDirections, mask = StickMask(expected);
            correct |= mask != 0 && (fresh & mask) != 0;
            wrong |= (fresh & ~mask) != 0;
            if (mask != 0 && (correct || secondary.HasValue)) wrong |= (directions & ~mask) != 0;
            heldDirections = directions;
        }
    }
}
