using Cinemachine;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Burntwax
{
    [DisallowMultipleComponent]
    public class GloveStateMachine : MonoBehaviour
    {

        [Header("References")]
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject gloveModel;
        [SerializeField] private Animator anim;
        [SerializeField] private Rig handIK;
        [SerializeField] private CinemachineImpulseSource camShake;

        [Header("Weapon Links")]
        [SerializeField] private Arsenal arsenal;
        [SerializeField] private GunStateMachine gunStateMachine;
        [SerializeField] private KatanaStateMachine katanaStateMachine;

        [Header("Planning Flags")]
        [SerializeField] private bool useDedicatedGloveMode = true;
        [SerializeField] private bool syncToCurrentWeaponSystem = true;

        [Header("Runtime")]
        [SerializeField] private GloveState currentState = GloveState.Hidden;

        public GloveState CurrentState => currentState;
        public bool FullyEquipped { get; private set; }
        public bool IsFiring { get; private set; }
        public bool IsReloading { get; private set; }

        private int aimHash;
        private int fireHash;
        private int reloadHash;
        private int stowHash;

        private void Awake()
        {
            CacheReferences();
            CacheAnimatorHashes();
        }

        private void Update()
        {
            if (!syncToCurrentWeaponSystem)
                return;
        }

        private void CacheReferences()
        {
            if (arsenal == null)
                arsenal = GetComponent<Arsenal>();

            if (gunStateMachine == null)
                gunStateMachine = GetComponent<GunStateMachine>();

            if (katanaStateMachine == null)
                katanaStateMachine = GetComponent<KatanaStateMachine>();
        }

        private void CacheAnimatorHashes()
        {
            aimHash = Animator.StringToHash("aim");
            fireHash = Animator.StringToHash("gloveFire");
            reloadHash = Animator.StringToHash("gloveReload");
            stowHash = Animator.StringToHash("stow");
        }

    }
}
