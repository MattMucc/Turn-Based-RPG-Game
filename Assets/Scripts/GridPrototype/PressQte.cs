using System;
using UnityEngine.InputSystem;

namespace Game.GridPrototype
{
    public enum PressQteResult { Preparing, Waiting, Success, WrongInput, TimedOut }

    // A deadline prompt, not a narrow timing-band test. Inputs are fresh press edges.
    public sealed class PressQte
    {
        public float Duration { get; }
        public float Remaining { get; private set; }
        public float PreparationRemaining { get; private set; }
        public PressQteResult Result { get; private set; }
        public bool Complete => Result == PressQteResult.Success || Result == PressQteResult.WrongInput || Result == PressQteResult.TimedOut;
        public float Fraction => Remaining / Duration;
        public PressQte(float duration, float preparation = 0.5f)
        {
            Duration = Math.Max(0.1f, duration); Remaining = Duration;
            PreparationRemaining = Math.Max(0, preparation);
            Result = PreparationRemaining > 0 ? PressQteResult.Preparing : PressQteResult.Waiting;
        }
        public void Advance(float deltaTime, bool correctPress = false, bool wrongPress = false)
        {
            if (Complete) return;
            deltaTime = Math.Max(0, deltaTime);
            if (Result == PressQteResult.Preparing)
            {
                PreparationRemaining = Math.Max(0, PreparationRemaining - deltaTime);
                if (PreparationRemaining == 0) Result = PressQteResult.Waiting;
                return; // Opening frame always displays the full countdown; no carried launch input.
            }
            // Judge the press against the time displayed, before advancing this frame.
            if (wrongPress) { Result = PressQteResult.WrongInput; return; }
            if (correctPress) { Result = PressQteResult.Success; return; }
            Remaining = Math.Max(0, Remaining - deltaTime);
            if (Remaining == 0) Result = PressQteResult.TimedOut;
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
        public void Read(Keyboard keyboard, Gamepad pad, out bool correct, out bool wrong)
        {
            correct = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            wrong = false;
            if (keyboard != null)
                foreach (var key in keyboard.allKeys)
                    if (key.wasPressedThisFrame && key.keyCode != Key.Space
                        && key.keyCode != Key.LeftShift && key.keyCode != Key.RightShift
                        && key.keyCode != Key.LeftCtrl && key.keyCode != Key.RightCtrl
                        && key.keyCode != Key.LeftAlt && key.keyCode != Key.RightAlt
                        && key.keyCode != Key.LeftMeta && key.keyCode != Key.RightMeta)
                        wrong = true;
            if (pad != null)
            {
                wrong |= pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame
                    || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame
                    || pad.leftTrigger.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame
                    || pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame
                    || pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame
                    || pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame;
            }
            // Start/Select and mouse controls are reserved, not failed gameplay attempts.
            int directions = Directions(pad); wrong |= (directions & ~heldDirections) != 0; heldDirections = directions;
        }
    }
}
