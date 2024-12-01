using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class GunStateMachine : MonoBehaviour {

   
    
    public InputManager input;

    public int currentID;
    
    [Header("Recoil")]
    public Cinemachine.CinemachineImpulseSource camShake;


    [Header("Animation")]
    [SerializeField] public Transform GunParent;



    public Animator anim;
    
    public TwoBoneIKConstraint right;
    public TwoBoneIKConstraint left;

   
    // [HideInInspector] public Animator handAnimator;
    // [HideInInspector] public  Animator gunAnimator;

    [HideInInspector] public int shootHash, aimShootHash, restingHash, reloadHash;

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
        anim.Play("equip_" + ActiveGun.Name);     
        camShake = ActiveGun.Model.GetComponent<Cinemachine.CinemachineImpulseSource>();
    

        shootHash = Animator.StringToHash("shoot");
        aimShootHash = Animator.StringToHash("aimShoot");
        reloadHash = Animator.StringToHash("reload");
    }


    public bool CanShoot() {
        return ActiveGun.AmmoConfig.currentClip > 0 && !isShooting;
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
