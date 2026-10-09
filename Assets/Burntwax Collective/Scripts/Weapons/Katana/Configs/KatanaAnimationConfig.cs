using System;
using UnityEngine;

namespace Burntwax
{
    public enum KatanaAnimationType
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
    /// Animation state names and blend times used by one katana. Gameplay
    /// states request an animation directly; Animator transition arrows do not
    /// decide which katana action happens.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Katana Animation Config",
        menuName = "Katana/Animation Config",
        order = 4)]
    public sealed class KatanaAnimationConfig : ScriptableObject
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
        [SerializeField] private SlashState left = new SlashState("left_slash_Katana", 0.06f);
        [SerializeField] private SlashState right = new SlashState("right_slash_Katana", 0.06f);
        [SerializeField] private SlashState upLeft = new SlashState("up_left_slash_Katana", 0.075f);
        [SerializeField] private SlashState upRight = new SlashState("up_right_slash_Katana", 0.075f);
        [SerializeField] private SlashState downLeft = new SlashState("down_left_slash_Katana", 0.045f);
        [SerializeField] private SlashState downRight = new SlashState("down_right_slash_Katana", 0.045f);
        [SerializeField] private SlashState down = new SlashState("down_slash_Katana", 0.045f);

        public SlashState GetSlashState(KatanaSlashDirection direction)
        {
            switch (direction)
            {
                case KatanaSlashDirection.Left: return left;
                case KatanaSlashDirection.Right: return right;
                case KatanaSlashDirection.UpLeft: return upLeft;
                case KatanaSlashDirection.UpRight: return upRight;
                case KatanaSlashDirection.DownLeft: return downLeft;
                case KatanaSlashDirection.DownRight: return downRight;
                case KatanaSlashDirection.Down: return down;
                default: return right;
            }
        }

        public static string GetDefaultSlashName(KatanaSlashDirection direction)
        {
            switch (direction)
            {
                case KatanaSlashDirection.Left: return "left_slash_Katana";
                case KatanaSlashDirection.UpLeft: return "up_left_slash_Katana";
                case KatanaSlashDirection.UpRight: return "up_right_slash_Katana";
                case KatanaSlashDirection.DownLeft: return "down_left_slash_Katana";
                case KatanaSlashDirection.DownRight: return "down_right_slash_Katana";
                case KatanaSlashDirection.Down: return "down_slash_Katana";
                default: return "right_slash_Katana";
            }
        }

        [Header("Animator Layer")]
        [Tooltip("Animator layer containing the katana states.")]
        [SerializeField] private string animatorLayerName = "Default Layer";

        [Header("Katana States")]
        [SerializeField]
        private State equip = new State("equip_Katana", 0.05f);
        [SerializeField]
        private State ready = new State("ready_Katana", 0.05f);
        [SerializeField]
        private State parry = new State("parry_Katana", 0.04f);
        [SerializeField]
        private State stow = new State("stow_Katana", 0.05f);
        [SerializeField]
        private State idle = new State("equip_Katana", 0.15f, 0.275f);
        [SerializeField]
        private State heavyCharge = new State("ready_Katana", 0.05f);
        [Tooltip("Temporary resting-pose fallback. Replace the state name when a recoil animation is available.")]
        [SerializeField]
        private State parriedRecoil = new State("equip_Katana", 0.05f, 0.275f);

        public string AnimatorLayerName => animatorLayerName;
        public State Equip => equip;
        public State Ready => ready;
        public State Parry => parry;
        public State Stow => stow;

        public State GetState(KatanaAnimationType animationType)
        {
            switch (animationType)
            {
                case KatanaAnimationType.Equip:
                    return equip;
                case KatanaAnimationType.Ready:
                    return ready;
                case KatanaAnimationType.Parry:
                    return parry;
                case KatanaAnimationType.Stow:
                    return stow;
                case KatanaAnimationType.Idle:
                    return idle;
                case KatanaAnimationType.HeavyCharge:
                    return heavyCharge;
                case KatanaAnimationType.ParriedRecoil:
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
