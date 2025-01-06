using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCrosshair : MonoBehaviour {

    [Header("Icon Image References")]
    [SerializeField] Image outerIcon;
    [SerializeField] Image innerIcon;



    [Header("Icon Types (Guns, interaction, etc.)")]

    //Outer
    [SerializeField] Sprite pistolIcon;
    [SerializeField] Sprite smgIcon;
    [SerializeField] Sprite shotgunIcon;
    [SerializeField] Sprite revolverIcon;


    //Inner
    [SerializeField] Sprite interactIcon;
    Color transparent;



    [Header("State Machines")]
    [SerializeField] GunStateMachine gun;
    [SerializeField] Interactor interactor;
    CamStateMachine cam;
    
    PlayerStateMachine player;
    
    // Start is called before the first frame update
    void Start() {
        transparent.a = 0;

        outerIcon.sprite = pistolIcon;


        innerIcon.sprite = null;
        innerIcon.color = transparent;
        player = GetComponent<PlayerStateMachine>();
        cam = GetComponent<CamStateMachine>();
    }

    // Update is called once per frame
    void Update()  {
        if(cam.currentState == cam.states.Aim()) {
            outerIcon.sprite = null;
            outerIcon.color = transparent;
            innerIcon.sprite = null;
            innerIcon.color = transparent;
        }
        else {
            if(interactor.ableToInteract) {
                // Interact "Inside type" icon is active
                innerIcon.sprite = interactIcon;
                innerIcon.color = Color.white;  
            }
            else {
                innerIcon.sprite = null;
                innerIcon.color = transparent;
            }

            
            
           


            
            

            
            


        }

        
        
    }
    public void SetAimCrosshair() {
        outerIcon.color = transparent;
        innerIcon.color = transparent;
    }

    public void SetInteractCrosshair() {
        innerIcon.color = Color.white;
        innerIcon.sprite = interactIcon;
    }

    public void SetOuterCrosshair() {
        // outerIcon.sprite = gun.ActiveGun.CrosshairConfig.icon;
    }


    
}
