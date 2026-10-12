using System;
using UnityEngine;

namespace Burntwax
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaAnimationType")]
    public enum DualBladeAnimationType
    {
        Equip,
        Ready,
        Slash,
        Parry,
        Stow,
        Idle,
        HeavyCharge,
        ParriedRecoil
    }

    /// <summary>
    /// Animation state names and blend times used by one dualBlade. Gameplay
    /// states request an animation directly; Animator transition arrows do not
    /// decide which dualBlade action happens.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DualBlade Animation Config",
        menuName = "DualBlade/Animation Config",
        order = 4)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaAnimationConfig")]
    public sealed class DualBladeAnimationConfig : ScriptableObject
    {
        [Serializable]
        public sealed class State
        {
            [SerializeField] private string animatorStateName;
            [SerializeField, Min(0f)] private float crossFadeDuration;
            [Tooltip("Seconds into the clip to begin playback. Idle reuses the settled part of equip until a dedicated idle clip is available.")]
            [SerializeField, Min(0f)] private float startTime;

            public string AnimatorStateName => animatorStateName;
            public float CrossFadeDuration => crossFadeDuration;
            public float StartTime => startTime;

            public State(string animatorStateName, float crossFadeDuration, float startTime = 0f)
            {
                this.animatorStateName = animatorStateName;
                this.crossFadeDuration = crossFadeDuration;
                this.startTime = startTime;
            }

            public void Validate()
            {
                crossFadeDuration = Mathf.Max(0f, crossFadeDuration);
                startTime = Mathf.Max(0f, startTime);
            }
        }

        [Serializable]
        public sealed class SlashState
        {
            [SerializeField] private string animatorStateName;
            [Tooltip("The clip assigned to this Animator state. Used to keep chain entry before its first damage-opening event.")]
            [SerializeField] private AnimationClip clip;
            [SerializeField, Min(0f)] private float blendDuration = 0.035f;
            [SerializeField, Min(0f)] private float chainBlendDuration = 0.035f;
            [Tooltip("Seconds to skip when chaining. Capped just before OpenDamageWindow so the event still fires. No assigned clip means no skipping.")]
            [SerializeField, Min(0f)] private float chainStartTime;

            public string AnimatorStateName => animatorStateName;
            public float BlendDuration => Mathf.Max(0f, blendDuration);
            public float ChainBlendDuration => Mathf.Max(0f, chainBlendDuration);

            public SlashState(string stateName, float chainStart)
            {
                animatorStateName = stateName;
                chainStartTime = chainStart;
            }

            public float GetChainStartTime()
            {
                if (clip == null)
                    return 0f;

                float firstDamageTime = float.PositiveInfinity;
                foreach (AnimationEvent animationEvent in clip.events)
                {
                    if (animationEvent.functionName == "OpenDamageWindow")
                        firstDamageTime = Mathf.Min(firstDamageTime, animationEvent.time);
                }

                // Do not silently skip the opening event, even after a clip
                // is retimed. Missing events fall back to full playback.
                return float.IsPositiveInfinity(firstDamageTime) ? 0f
                    : Mathf.Clamp(chainStartTime, 0f, Mathf.Max(0f, firstDamageTime - 0.001f));
            }
        }

        [Header("Directional Slashes")]
        [SerializeField] private SlashState left = new SlashState("left_slash_DualBlade", 0.06f);
        [SerializeField] private SlashState right = new SlashState("right_slash_DualBlade", 0.06f);
        [SerializeField] private SlashState upLeft = new SlashState("up_left_slash_DualBlade", 0.075f);
        [SerializeField] private SlashState upRight = new SlashState("up_right_slash_DualBlade", 0.075f);
        [SerializeField] private SlashState downLeft = new SlashState("down_left_slash_DualBlade", 0.045f);
        [SerializeField] private SlashState downRight = new SlashState("down_right_slash_DualBlade", 0.045f);
        [SerializeField] private SlashState down = new SlashState("down_slash_DualBlade", 0.045f);

        public SlashState GetSlashState(DualBladeSlashDirection direction)
        {
            switch (direction)
            {
                case DualBladeSlashDirection.Left: return left;
                case DualBladeSlashDirection.Right: return right;
                case DualBladeSlashDirection.UpLeft: return upLeft;
                case DualBladeSlashDirection.UpRight: return upRight;
                case DualBladeSlashDirection.DownLeft: return downLeft;
                case DualBladeSlashDirection.DownRight: return downRight;
                case DualBladeSlashDirection.Down: return down;
                default: return right;
            }
        }

        public static string GetDefaultSlashName(DualBladeSlashDirection direction)
        {
            switch (direction)
            {
                case DualBladeSlashDirection.Left: return "left_slash_DualBlade";
                case DualBladeSlashDirection.UpLeft: return "up_left_slash_DualBlade";
                case DualBladeSlashDirection.UpRight: return "up_right_slash_DualBlade";
                case DualBladeSlashDirection.DownLeft: return "down_left_slash_DualBlade";
                case DualBladeSlashDirection.DownRight: return "down_right_slash_DualBlade";
                case DualBladeSlashDirection.Down: return "down_slash_DualBlade";
                default: return "right_slash_DualBlade";
            }
        }

        [Header("Animator Layer")]
        [Tooltip("Animator layer containing the dualBlade states.")]
        [SerializeField] private string animatorLayerName = "Default Layer";

        [Header("DualBlade States")]
        [SerializeField]
        private State equip = new State("equip_DualBlade", 0.05f);
        [SerializeField]
        private State ready = new State("ready_DualBlade", 0.05f);
        [SerializeField]
        private State parry = new State("parry_DualBlade", 0.04f);
        [SerializeField]
        private State stow = new State("stow_DualBlade", 0.05f);
        [SerializeField]
        private State idle = new State("equip_DualBlade", 0.15f, 0.275f);
        [SerializeField]
        private State heavyCharge = new State("ready_DualBlade", 0.05f);
        [Tooltip("Temporary resting-pose fallback. Replace the state name when a recoil animation is available.")]
        [SerializeField]
        private State parriedRecoil = new State("equip_DualBlade", 0.05f, 0.275f);

        public string AnimatorLayerName => animatorLayerName;
        public State Equip => equip;
        public State Ready => ready;
        public State Parry => parry;
        public State Stow => stow;

        public State GetState(DualBladeAnimationType animationType)
        {
            switch (animationType)
            {
                case DualBladeAnimationType.Equip:
                    return equip;
                case DualBladeAnimationType.Ready:
                    return ready;
                case DualBladeAnimationType.Parry:
                    return parry;
                case DualBladeAnimationType.Stow:
                    return stow;
                case DualBladeAnimationType.Idle:
                    return idle;
                case DualBladeAnimationType.HeavyCharge:
                    return heavyCharge;
                case DualBladeAnimationType.ParriedRecoil:
                    return parriedRecoil;
                default:
                    return null;
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(animatorLayerName))
                animatorLayerName = "Default Layer";

            equip?.Validate();
            ready?.Validate();
            parry?.Validate();
            stow?.Validate();
            idle?.Validate();
            heavyCharge?.Validate();
            parriedRecoil?.Validate();
        }
    }
}
