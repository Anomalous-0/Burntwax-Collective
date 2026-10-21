using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Recognizes one press-flick-release dualBlade gesture. Mouse deltas are
    /// converted to velocity over a short real-time window so thresholds do
    /// not change with the rendered frame rate.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaGestureRecognizer")]
    public sealed class DualBladeGestureRecognizer
    {
        public bool IsInitialSlashQualified { get; private set; }
        public Vector2 InitialSlashDirection { get; private set; }
        public float InitialQualificationSpeed { get; private set; }
        public Vector2 CurrentLookVelocity { get; private set; }
        public float CurrentLookSpeed => CurrentLookVelocity.magnitude;
        public float LastHeavyReversalSpeed { get; private set; }
        public float LastReversalAlignment { get; private set; }

        private readonly VelocityWindow velocityWindow = new();
        private bool wasAttackHeld;
        private bool emittedIntentDuringCurrentPress;

        public bool TryRecognize(
            Vector2 lookDelta,
            bool attackHeld,
            float unscaledDeltaTime,
            DualBladeGestureConfig config,
            out DualBladeAttackIntent intent)
        {
            intent = default;

            if (config == null)
            {
                Reset();
                return false;
            }

            if (!attackHeld)
                return ResolveRelease(out intent);

            if (!wasAttackHeld)
            {
                ResetGestureTracking();
                InitialQualificationSpeed = 0f;
                LastHeavyReversalSpeed = 0f;
                LastReversalAlignment = 0f;
                wasAttackHeld = true;
            }

            if (emittedIntentDuringCurrentPress)
                return false;

            velocityWindow.Add(
                lookDelta,
                unscaledDeltaTime,
                config.VelocitySampleWindow);
            CurrentLookVelocity = velocityWindow.Velocity;

            // if (!IsInitialSlashQualified)
            // {
            //     if (CurrentLookSpeed < config.MinimumInitialSlashSpeed)
            //         return false;

            //     IsInitialSlashQualified = true;
            //     InitialSlashDirection = CurrentLookVelocity.normalized;
            //     InitialQualificationSpeed = CurrentLookSpeed;

            //     // The initial flick must not dilute the following reversal.
            //     // Start a fresh short velocity window for heavy recognition.
            //     velocityWindow.Reset();
            //     return false;
            // }

            // LastHeavyReversalSpeed = CurrentLookSpeed;
            // if (CurrentLookSpeed <= Mathf.Epsilon)
            // {
            //     LastReversalAlignment = 0f;
            //     return false;
            // }

            // Vector2 reversalDirection = CurrentLookVelocity.normalized;
            // LastReversalAlignment = -Vector2.Dot(
            //     InitialSlashDirection,
            //     reversalDirection);

            // bool isHeavyReversal =
            //     CurrentLookSpeed >= config.MinimumHeavyReversalSpeed
            //     && LastReversalAlignment >= config.MinimumReversalAlignment;

            // if (!isHeavyReversal)
            //     return false;

            // intent = new DualBladeAttackIntent(
            //     DualBladeAttackStrength.Heavy,
            //     CurrentLookVelocity,
            //     InitialSlashDirection);
            // emittedIntentDuringCurrentPress = intent.IsValid;
            // return intent.IsValid;

            // Wait until the mouse movement qualifies as a directional attack.
            if (CurrentLookSpeed <= Mathf.Epsilon
                || CurrentLookSpeed < config.MinimumInitialSlashSpeed)
            {
                return false;
            }

            IsInitialSlashQualified = true;
            InitialSlashDirection = CurrentLookVelocity.normalized;
            InitialQualificationSpeed = CurrentLookSpeed;

            // A qualifying flick now commits immediately. No reversal required.
            intent = new DualBladeAttackIntent(
                DualBladeAttackStrength.Heavy,
                CurrentLookVelocity,
                InitialSlashDirection);

            // Prevent this same mouse press from producing another attack.
            emittedIntentDuringCurrentPress = intent.IsValid;
            return intent.IsValid;
        }

        public void Reset()
        {
            ResetGestureTracking();
            wasAttackHeld = false;
            InitialQualificationSpeed = 0f;
            LastHeavyReversalSpeed = 0f;
            LastReversalAlignment = 0f;
        }

        private bool ResolveRelease(out DualBladeAttackIntent intent)
        {
            intent = default;

            // A press that hasn't already produced Heavy becomes Light on release.
            bool shouldEmitLight = wasAttackHeld
                && !emittedIntentDuringCurrentPress;

            ResetGestureTracking();
            wasAttackHeld = false;

            if (!shouldEmitLight)
                return false;

            intent = new DualBladeAttackIntent(
                DualBladeAttackStrength.Light,
                Vector2.zero);

            return intent.IsValid;
        }
        private void ResetGestureTracking()
        {
            velocityWindow.Reset();
            IsInitialSlashQualified = false;
            InitialSlashDirection = Vector2.zero;
            CurrentLookVelocity = Vector2.zero;
            emittedIntentDuringCurrentPress = false;
        }

        /// <summary>
        /// Fixed-capacity rolling window. Samples are trimmed by elapsed time,
        /// including partial trimming of the oldest frame, and allocate no
        /// garbage during gameplay.
        /// </summary>
        private sealed class VelocityWindow
        {
            private const int Capacity = 128;
            private const float MinimumDuration = 0.0001f;

            private readonly Vector2[] deltas = new Vector2[Capacity];
            private readonly float[] durations = new float[Capacity];

            private int head;
            private int count;
            private Vector2 accumulatedDelta;
            private float accumulatedDuration;

            public Vector2 Velocity => accumulatedDuration > Mathf.Epsilon
                ? accumulatedDelta / accumulatedDuration
                : Vector2.zero;

            public void Add(
                Vector2 delta,
                float duration,
                float windowDuration)
            {
                duration = Mathf.Max(MinimumDuration, duration);
                windowDuration = Mathf.Max(MinimumDuration, windowDuration);

                if (duration > windowDuration)
                {
                    // A long rendered frame contains more accumulated mouse
                    // delta. Preserve its velocity while retaining only the
                    // portion represented by the rolling window.
                    delta *= windowDuration / duration;
                    duration = windowDuration;
                    Reset();
                }

                if (count == Capacity)
                    RemoveOldest();

                int tail = (head + count) % Capacity;
                deltas[tail] = delta;
                durations[tail] = duration;
                count++;
                accumulatedDelta += delta;
                accumulatedDuration += duration;

                TrimTo(windowDuration);
            }

            public void Reset()
            {
                head = 0;
                count = 0;
                accumulatedDelta = Vector2.zero;
                accumulatedDuration = 0f;
            }

            private void TrimTo(float windowDuration)
            {
                float excessDuration =
                    accumulatedDuration - windowDuration;

                while (count > 0 && excessDuration > Mathf.Epsilon)
                {
                    float oldestDuration = durations[head];
                    if (oldestDuration <= excessDuration + Mathf.Epsilon)
                    {
                        excessDuration -= oldestDuration;
                        RemoveOldest();
                        continue;
                    }

                    float removedFraction =
                        excessDuration / oldestDuration;
                    Vector2 removedDelta =
                        deltas[head] * removedFraction;

                    deltas[head] -= removedDelta;
                    durations[head] -= excessDuration;
                    accumulatedDelta -= removedDelta;
                    accumulatedDuration -= excessDuration;
                    excessDuration = 0f;
                }
            }

            private void RemoveOldest()
            {
                if (count == 0)
                    return;

                accumulatedDelta -= deltas[head];
                accumulatedDuration -= durations[head];
                deltas[head] = Vector2.zero;
                durations[head] = 0f;
                head = (head + 1) % Capacity;
                count--;
            }
        }
    }
}
