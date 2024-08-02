using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerGunSelector : MonoBehaviour {
    // private GunType Gun;

    public InputManager input;
    
    public int currentID;
    [SerializeField] public Transform GunParent;

    [Header("Animation")]
    public  Animator animator;
    public int shootHash, aimShootHash, restingHash, reloadHash;

    public List<GunScriptableObject> Guns;

    
   

    //public PlayerIK InverseKinematics;

    [Space]
    [Header("Runtime Filled")] public GunScriptableObject ActiveGun;

    private void Start() {
        
        // currentID = 0;

        // GunScriptableObject gun = Guns.Find(gun => gun.ID == currentID);

        // if (gun == null) {
        //     Debug.LogError($"No GunScriptableObject found for GunType: {gun}");
        //     return;
        // }

        // ActiveGun = gun;
        // gun.Spawn(GunParent, this);

        // some magic for IK
        // Transform[] allChildren = GunParent.GetComponentsInChildren<Transform>();
        // InverseKinematics.LeftElbowIKTarget = allChildren.FirstOrDefault(child => child.name == "LeftElbow");
        // InverseKinematics.RightElbowIKTarget = allChildren.FirstOrDefault(child => child.name == "RightElbow");
        // InverseKinematics.LeftHandIKTarget = allChildren.FirstOrDefault(child => child.name == "LeftHand");
        // InverseKinematics.RightHandIKTarget = allChildren.FirstOrDefault(child => child.name == "RightHand");
    }

    private void Update() {
        
        // Debug.Log(currentID);
       if(Guns.Count >  0) {
            if(input.mouseScroll != 0) {
                WeaponSwitch();
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
            // Get ammo;
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


    private void SetAnimator() {
        animator = ActiveGun.Model.GetComponent<Animator>();
        shootHash = Animator.StringToHash("shoot");
        aimShootHash = Animator.StringToHash("aimShoot");
        reloadHash = Animator.StringToHash("reload");
    }


}
