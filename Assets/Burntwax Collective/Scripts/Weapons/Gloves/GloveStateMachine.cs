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
        [SerializeField] private GameObject gloveModel;
        private Animator anim;
        [SerializeField] private Rig handIK;

        private Arsenal arsenal;
        private GunStateMachine gunStateMachine;
        private KatanaStateMachine katanaStateMachine;


        public bool FullyEquipped { get; private set; }
        public bool IsFiring { get; private set; }
        public bool IsReloading { get; private set; }

        private int aimHash;
        private int fireHash;
        private int reloadHash;
        private int stowHash;

        public GloveBaseState CurrentState;

        public GloveStateFactory states;

        private void Awake()
        {
            states = new GloveStateFactory(this);
            CurrentState = states.Unequipped();
            CurrentState.EnterState();


            anim = gloveModel.GetComponent<Animator>();
            arsenal = GetComponent<Arsenal>();
            gunStateMachine = GetComponent<GunStateMachine>();
            katanaStateMachine = GetComponent<KatanaStateMachine>();

        }

        private void Update()
        {
            currentStateText = CurrentState.GetType().Name;
            currentSubstateText = CurrentState.CurrentSubstate()?.GetType().Name ?? "None";
        }



    }
}
