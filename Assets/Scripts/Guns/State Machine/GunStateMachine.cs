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

   
    // [HideInInspector] public Animator handAnimator;
    // [HideInInspector] public  Animator gunAnimator;

    [HideInInspector] public int shootHash, aimShootHash, stowHash, aimHash, reloadHash;

    public List<GunScriptableObject> Guns;

    [Space]
    [Header("Runtime Filled")] public GunScriptableObject ActiveGun;


    [Space]

  

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
            WeaponSelect();
            currentState.UpdateStates();
            
            if((input.weaponSelectID || input.mouseScroll != 0) && !isShooting) {
                // Debug.Log("Helo");
                WeaponSwitch();
            }  
            EmptyClip();
        }
        


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
        
        GunScriptableObject gun = Guns.Find(gun => gun.ID == currentID);

        if (gun == null) {
            Debug.LogError($"No GunScriptableObject found for GunType: {gun}");
            return;
        }
            
        ActiveGun.Despawn(this);
        ActiveGun = gun;
        gun.Spawn(GunParent, this);

        crosshair.SetGunCrosshair(gun);
        



        SetupGun();
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

            crosshair.SetGunCrosshair(gun);
            SetupGun();


        }    
        else {
            // Get ammo from the gun lying on the floor
        }
            
    }


    private void SetupGun() {
        handIK.weight = 1;
        anim.Play("equip_" + ActiveGun.Name);     
        camShake = ActiveGun.Model.GetComponent<Cinemachine.CinemachineImpulseSource>();
    

        shootHash = Animator.StringToHash("shoot");
        // equipHash = Animator.StringToHash("equip");
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
