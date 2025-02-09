using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;


[DisallowMultipleComponent]
public class GunStateMachine : MonoBehaviour {

    

    [Header("State Machines")]
    public PlayerStateMachine stateMachine;
    public CamStateMachine camMachine; 
    public PlayerCrosshair crosshair;

    [Space]
    
    public InputManager input;

    public int currentID;
    
    [Header("Recoil")]
    public Cinemachine.CinemachineImpulseSource camShake;


    [Header("Animation")]
    [SerializeField] public Transform GunParent;



    public Animator anim;
    
    public Rig handIK;

    [HideInInspector] public int  stowHash, aimHash, reloadHash, incrementalReloadHash, shotWhileIncReloadHash;

    public List<GunScriptableObject> CurrentGuns;
    public List<GameObject> OrderedGuns;

    [Space]

    
    [Header("Current Gun")] 
    public GunScriptableObject ActiveGunScriptable;
    public GameObject CurrentGunModel;


    [Space]

  

    private GunBaseState currentState;
    public GunStateFactory states;


  
    public GunBaseState CurrentState {get { return currentState; } set { currentState = value; } }


    public bool isReloading = false;
    public bool isIncrementalReloading = false;
        public bool isShooting = false;
    



   


    void Awake() {
        
        
        // handAnimator = GetComponent<Animator>();
        states = new GunStateFactory(this);
        currentState = states.Rest();
        currentState.EnterState();
        handIK.weight = 0;
    }

    void Start() {
    }



    private void Update() {
        if(ActiveGunScriptable != null) {
            WeaponSelect();
            currentState.UpdateStates();
            
            if((input.weaponSelectID || input.mouseScroll != 0) && !isShooting) {
                // Debug.Log("Helo");
                WeaponSwitch();
            }  
            EmptyClip();
        }
        


    }



    private void Despawn(GunScriptableObject activeGun) {
        CurrentGunModel.SetActive(false);
        activeGun.Despawn(this);
    }

    private void Spawn(GunScriptableObject activeGun) {
        CurrentGunModel = OrderedGuns[currentID];
        CurrentGunModel.SetActive(true);
        activeGun.Spawn(CurrentGunModel, this);
        
    }

    // NEED TO DO THIS WITH CONTROLLER SUPPORT SOMEHOW
    private void WeaponSelect() {
        if(input.weaponSelectID) {
            if(Input.GetKeyDown(KeyCode.Alpha1)) {
                currentID = 0;
            }
            else if(Input.GetKeyDown(KeyCode.Alpha2)){
                currentID = 1;
            }
            else if(Input.GetKeyDown(KeyCode.Alpha3)){
                currentID = 2;
            }
            else if(Input.GetKeyDown(KeyCode.Alpha4)){
                currentID = 3;
            }
        }
    }

    private void WeaponSwitch() {
        if(input.mouseScroll != 0) {
            ScrollID();
        }
        else if(input.weaponSelectID) {
            WeaponSelect();
        }
        
        GunScriptableObject gun = CurrentGuns.Find(gun => gun.ID == currentID);
        

        if (gun == null) {
            Debug.LogError($"No GunScriptableObject found for GunType: {gun}");
            return;
        }
            

        Despawn(ActiveGunScriptable);
        
        ActiveGunScriptable = gun;
        // Spawn(ActiveGunScriptable);
        
        // gun.Spawn(GunParent, this);
        // crosshair.SetGunCrosshair(gun);
        SetupGun();
        // restingHash = Animator.StringToHash("resting");
    }

    private void ScrollID() {
        if(input.mouseScroll > 0) {
            currentID++;
            if(currentID > CurrentGuns.Count - 1) {
                currentID = 0;
            }
        }
        else if(input.mouseScroll < 0) {
            currentID--;
            if(currentID < 0) {
                currentID = CurrentGuns.Count - 1;
            }
        }

        
        
    }

    public void Pickup(GunScriptableObject gun) {
        if(!CurrentGuns.Contains(gun)) {
            CurrentGuns.Add(gun);
            if(ActiveGunScriptable != null) {
                Despawn(ActiveGunScriptable);
            }
                
            ActiveGunScriptable = gun;
            currentID = gun.ID;
            
            
            SetupGun();


        }    
        else {
            // Get ammo from the gun lying on the floor
        }
            
    }


    private void SetupGun() {
        Spawn(ActiveGunScriptable);
        crosshair.SetGunCrosshair(ActiveGunScriptable);
        currentState = states.Rest();
        handIK.weight = 1;
        anim.Play("equip_" + ActiveGunScriptable.Name);     
        camShake = CurrentGunModel.GetComponent<Cinemachine.CinemachineImpulseSource>();
        

       
        
        aimHash = Animator.StringToHash("aim");
        stowHash = Animator.StringToHash("stow");
        reloadHash = Animator.StringToHash("reload");
        incrementalReloadHash = Animator.StringToHash("incrementalReload");
        shotWhileIncReloadHash = Animator.StringToHash("shotWhileIncReload");

        anim.SetBool(aimHash, false);
        anim.SetBool(stowHash, false);
        anim.SetBool(reloadHash, false);
        anim.SetBool(incrementalReloadHash, false);
        
    }




    private void EmptyClip() {
        if(ClipEmpty() && input.shootIsPressed && CurrentState.CurrentSuperstate() != states.Reload() &&  CurrentState.CurrentSuperstate() != states.Stow() && ClipEmpty() && input.shootIsPressed) {
            ActiveGunScriptable.AudioConfig.PlayEmptyClip(CurrentGunModel.GetComponent<AudioSource>());
            //THE ONE TIME I  WILL CHANGE SHOOT IS PRESSED THEN IG
            input.shootIsPressed = false;
            anim.Play("emptyclip_" + ActiveGunScriptable.Name);     
        }
    }

    public bool CanShoot() {
        return ActiveGunScriptable.AmmoConfig.currentClip > 0 && !isShooting;
    }

    private bool ClipEmpty() {
        return ActiveGunScriptable.AmmoConfig.currentClip == 0 && ActiveGunScriptable.AmmoConfig.currentAmmo == 0;
    }

    public void EndReload() {
        if(!ActiveGunScriptable.incrementalReload) {

            ActiveGunScriptable.EndReload();
        }
        isReloading = false;
        
        
    }

    public void EndIncrementalReload() {
        ActiveGunScriptable.EndIncrementalReload();
       
    }


    // public void BarrelSpin() {
    //     barrel.Spin();
    // }

    public void EndShoot() {
        isShooting = false;
    }





    public bool ManualReload() {
        return input.reloadIsPressed && ActiveGunScriptable.CanReload();
        // return  !isReloading && input.reloadIsPressed && GunSelector.ActiveGun.CanReload();
    }


    public bool IncrementalReload() {
        return ActiveGunScriptable.CanReload();
    }

    public bool AutoReload() {
        return ActiveGunScriptable.CanReload() && ActiveGunScriptable.AmmoConfig.currentClip == 0;
    }



}
