using Cinemachine;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Burntwax
{
    [DisallowMultipleComponent]
    public class GloveStateMachine : MonoBehaviour
    {

        [Header("References")]
        public string currentStateText;
        public string currentSubstateText;
        [SerializeField] private GameObject player;
        private CamStateMachine cam;
        private PlayerCrosshair crosshair;
        [SerializeField] private GameObject gloveModel;
        private Animator anim;
        [SerializeField] private Rig handIK;

        private Arsenal arsenal;
        private GunStateMachine gunStateMachine;
        private KatanaStateMachine katanaStateMachine;

        [Header("Glove Settings")]
        [SerializeField] private float detectDistance = 9f;
        [SerializeField] private LayerMask attractableLayer;



        [Header("State Variables")]
        
        [SerializeField] private float equipWaitTime;
        [SerializeField] private float substateWaitTime;
        private bool canAttractObject;
        public bool FullyEquipped { get; private set; }
        public bool IsFiring { get; private set; }
        public bool IsReloading { get; private set; }

        public float DetectDistance { get { return detectDistance; } }
        public LayerMask AttractableLayer { get { return attractableLayer; } }
        public bool CanAttractObject { get { return canAttractObject; } }
        public float EquipWaitTime { get { return equipWaitTime; } }
        public float SubstateWaitTime { get { return substateWaitTime; } }

        private int idleHash, chargeHash, holdHash, blastHash;

        public GloveBaseState CurrentState;

        public GloveStateFactory states;

        private void Awake()
        {
            states = new GloveStateFactory(this);
            CurrentState = states.Unequipped();
            CurrentState.EnterState();

            cam = player.GetComponentInChildren<CamStateMachine>();
            crosshair = player.GetComponentInChildren<PlayerCrosshair>();
            anim = gloveModel.GetComponent<Animator>();
            arsenal = GetComponent<Arsenal>();
            gunStateMachine = GetComponent<GunStateMachine>();
            katanaStateMachine = GetComponent<KatanaStateMachine>();

        }

        private void Update()
        {
            CurrentState.UpdateStates();
            currentStateText = CurrentState.GetType().Name;
            currentSubstateText = CurrentState.CurrentSubstate()?.GetType().Name ?? "None";
            
        }



    }
}
