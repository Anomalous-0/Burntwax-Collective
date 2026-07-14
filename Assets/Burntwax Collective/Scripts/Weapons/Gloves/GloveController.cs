using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Burntwax
{
    /// <summary>
    /// Drives the left-hand "glove". Tapping the Gloves input (default: tap F) toggles the left
    /// hand out of the weapon grip into a stop-sign pose; tapping again lowers it onto the gun.
    /// (Holding F is intentionally left free for a future ability.) The blend itself is always
    /// smooth regardless of how fast the toggle flips, because it lerps toward the target.
    ///
    /// Design notes:
    ///  - The glove is deliberately NOT part of the Arsenal / GunStateMachine weapon cycle, so it
    ///    can be raised while any weapon is equipped and while switching weapons.
    ///  - ADS blocking is handled centrally, NOT here: <see cref="InputPrioritySorter.AimIsPriority"/>
    ///    and the gun/camera aim states read <c>InputManager.Instance.useGloves</c>. Shooting,
    ///    switching and reloading are on untouched code paths, so they keep working while raised.
    ///  - The raise is purely visual/mechanical here: a masked (left-arm only) animator override
    ///    layer is blended in by weight, while the LEFT-hand IK constraint weight is lerped to 0 so
    ///    the hand can leave the gun. The right hand's IK is a separate constraint and is untouched,
    ///    so the right hand keeps aiming/shooting/reloading independently.
    /// </summary>
    public class GloveController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The Arms Animator — the same one GunStateMachine drives.")]
        public Animator anim;

        [Tooltip("The LEFT hand TwoBoneIKConstraint (the 'LeftHandIK' child under the hand-IK rig " +
                 "layer). Its weight is lerped to 0 while the glove is up so the left hand can " +
                 "release the weapon. Do NOT assign the whole rig here — that would also release " +
                 "the right hand.")]
        public TwoBoneIKConstraint leftHandIK;

        [Tooltip("Name of the masked left-arm override layer added to the Arms animator controller.")]
        public string gloveLayerName = "Glove";

        [Header("Tuning")]
        [Tooltip("Raise/lower speed in units of raiseAmount per second (e.g. 6 = ~0.17s to full).")]
        public float raiseSpeed = 6f;

        [Header("Runtime (read-only)")]
        [Range(0f, 1f)]
        [Tooltip("Current blend: 0 = hand on gun, 1 = full stop-sign.")]
        public float raiseAmount = 0f;

        int gloveLayerIndex = -1;
        float baseLeftHandWeight = 1f;

        void Start()
        {
            if (anim != null)
            {
                gloveLayerIndex = anim.GetLayerIndex(gloveLayerName);
                if (gloveLayerIndex < 0)
                    Debug.LogWarning($"[GloveController] Animator layer '{gloveLayerName}' not found. " +
                                     "Add a masked left-arm Override layer with that name to the Arms controller.");
            }
            else
            {
                Debug.LogWarning("[GloveController] No Animator assigned.");
            }

            if (leftHandIK != null)
                baseLeftHandWeight = leftHandIK.weight;
        }

        void Update()
        {
            bool wantUp = InputManager.Instance != null && InputManager.Instance.useGloves;

            float target = wantUp ? 1f : 0f;
            raiseAmount = Mathf.MoveTowards(raiseAmount, target, raiseSpeed * Time.deltaTime);

            // Blend the masked left-arm stop-sign pose in/out. Right hand is outside the mask.
            if (gloveLayerIndex >= 0)
                anim.SetLayerWeight(gloveLayerIndex, raiseAmount);

            // Release only the LEFT hand from the weapon as the glove rises.
            if (leftHandIK != null)
                leftHandIK.weight = Mathf.Lerp(baseLeftHandWeight, 0f, raiseAmount);
        }

        /// <summary>True once the glove is meaningfully raised (for VFX/audio/gameplay hooks).</summary>
        public bool GloveIsRaised => raiseAmount > 0.01f;
    }
}
