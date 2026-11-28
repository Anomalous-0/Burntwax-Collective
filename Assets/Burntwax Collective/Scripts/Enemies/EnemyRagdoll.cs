using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class EnemyRagdoll : MonoBehaviour
    {
        [Tooltip("Pelvis or skeleton root containing the ragdoll bodies. Excludes the enemy's root collider.")]
        [SerializeField] private Transform skeletonRoot;

        private Animator animator;
        private Rigidbody[] boneBodies;
        private readonly List<Collider> boneColliders = new();

        private void Awake()
        {
            if (skeletonRoot == null
                || skeletonRoot == transform
                || !skeletonRoot.IsChildOf(transform))
            {
                Debug.LogWarning("Assign a skeleton root below the Animator.", this);
                return;
            }

            boneBodies = skeletonRoot.GetComponentsInChildren<Rigidbody>();
            if (boneBodies.Length == 0)
            {
                Debug.LogWarning("The skeleton root has no ragdoll rigidbodies.", this);
                return;
            }

            animator = GetComponent<Animator>();
            foreach (Rigidbody body in boneBodies)
                body.isKinematic = true;

            foreach (Collider boneCollider in skeletonRoot.GetComponentsInChildren<Collider>())
            {
                if (!boneCollider.enabled || boneCollider.isTrigger)
                    continue;

                boneColliders.Add(boneCollider);
                boneCollider.enabled = false;
            }
        }

        [ContextMenu("Test Ragdoll (Play Mode)")]
        public void ActivateRagdoll()
        {
            if (!Application.isPlaying || animator == null)
                return;

            animator.enabled = false;
            foreach (Rigidbody body in boneBodies)
                body.isKinematic = false;

            foreach (Collider boneCollider in boneColliders)
                boneCollider.enabled = true;
        }
    }
}
