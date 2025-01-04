using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;


[DisallowMultipleComponent]
public class GunStateMachine : MonoBehaviour {

   
    
    public InputManager input;

    public int currentID;
    
    [Header("Recoil")]
    public Cinemachine.CinemachineImpulseSource camShake;


    [Header("Animation")]
    [SerializeField] public Transform GunParent;



    public Animator anim;
    
    public Rig handIK;

   
    // [HideInInspector] public Animator handAnimator;
    // [HideInInspector] public  Animator gunAnimator;

    [HideInInspector] public int shootHash, aimShootHash, stowHash, aimHash, reloadHash;

    public List<GunScriptableObject> Guns;

    [Space]
    [Header("Runtime Filled")] public GunScriptableObject ActiveGun;


    [Space]
    public PlayerStateMachine stateMachine;
    public CamStateMachine camMachine;
    

    private GunBaseState currentState;
    public GunStateFactory states;


  
    public GunBaseState CurrentState {get { return currentState; } set { currentState = value; } }


    public bool isReloading = false;
    public bool isShooting = false;
    



   


    void Awake() {
        
        
        // handAnimator = GetComponent<Animator>();
        states = new GunStateFactory(this);
        currentState = states.Rest();
        currentState.EnterState();
        handIK.weight = 0;
    }

    void Start() {
       

        //stateMachine = GetComponent<PlayerStateMachine>();
        
        

    }



    private void Update() {
        if(ActiveGun != null) {
            currentState.UpdateStates();
            
            if(input.mouseScroll != 0 && !isShooting) {
                // Debug.Log("Helo");
                WeaponSwitch();
            }  
            EmptyClip();
        }
        


    }

    private void WeaponSwitch() {
        ScrollID();
        GunScriptableObject gun = Guns.Find(gun => gun.ID == currentID);

        if (gun == null) {
            Debug.LogError($"No GunScriptableObject found for GunType: {gun}");
            return;
        }
            
        ActiveGun.Despawn(this);
        ActiveGun = gun;
        gun.Spawn(GunParent, this);
        



        SetAnimator();
        // restingHash = Animator.StringToHash("resting");
    }

    private void ScrollID() {
        if(input.mouseScroll > 0) {
            currentID++;
            if(currentID > Guns.Count - 1) {
                currentID = 0;
            }
        }
        else if(input.mouseScroll < 0) {
            currentID--;
            if(currentID < 0) {
                currentID = Guns.Count - 1;
            }
        }

        
        
    }

    public void Pickup(GunScriptableObject gun) {
        if(!Guns.Contains(gun)) {
            Guns.Add(gun);
            if(ActiveGun != null) {
                ActiveGun.Despawn(this);
            }
                
            ActiveGun = gun;
            currentID = gun.ID;
            gun.Spawn(GunParent, this);


            SetAnimator();


        }    
        else {
            // Get ammo from the gun lying on the floor
        }
            
    }


    private void SetAnimator() {
        handIK.weight = 1;
        anim.Play("equip_" + ActiveGun.Name);     
        camShake = ActiveGun.Model.GetComponent<Cinemachine.CinemachineImpulseSource>();
    

        shootHash = Animator.StringToHash("shoot");
        aimShootHash = Animator.StringToHash("aimShoot");
        aimHash = Animator.StringToHash("aim");
        stowHash = Animator.StringToHash("stow");
        reloadHash = Animator.StringToHash("reload");
    }




    private void EmptyClip() {
        if(ClipEmpty() && input.shootIsPressed && CurrentState.CurrentSuperstate() != states.Reload() &&  CurrentState.CurrentSuperstate() != states.Stow() && ClipEmpty() && input.shootIsPressed) {
            ActiveGun.AudioConfig.PlayEmptyClip(ActiveGun.Model.GetComponent<AudioSource>());
            //THE ONE TIME I  WILL CHANGE SHOOT IS PRESSED THEN IG
            input.shootIsPressed = false;
            anim.Play("emptyclip_" + ActiveGun.Name);     
        }
    }

    public bool CanShoot() {
        return ActiveGun.AmmoConfig.currentClip > 0 && !isShooting;
    }

    private bool ClipEmpty() {
        return ActiveGun.AmmoConfig.currentClip == 0 && ActiveGun.AmmoConfig.currentAmmo == 0;
    }

    public void EndReload() {
        ActiveGun.EndReload();
        isReloading = false;
        
    }

    public void EndShoot() {
        isShooting = false;
    }



    public bool ManualReload() {

        return input.reloadIsPressed && ActiveGun.CanReload();
        
        // return  !isReloading && input.reloadIsPressed && GunSelector.ActiveGun.CanReload();
    }

    public bool AutoReload() {
        return ActiveGun.CanReload() && ActiveGun.AmmoConfig.currentClip == 0;
        // return !isReloading && autoReload && GunSelector.ActiveGun.CanReload() && GunSelector.ActiveGun.AmmoConfig.currentClip == 0;
    }



}
