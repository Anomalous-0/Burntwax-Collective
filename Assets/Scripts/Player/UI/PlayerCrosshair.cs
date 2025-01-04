using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerCrosshair : MonoBehaviour {

    [Header("Icon types")]
    [SerializeField] GameObject outerIcon;
    [SerializeField] GameObject innerIcon;

    [Header("State Machines")]
    [SerializeField] GunStateMachine gun;
    [SerializeField] Interactor interactor;
    [SerializeField] GloveStateMachine glove;
    CamStateMachine cam;
    
    PlayerStateMachine player;
    
    // Start is called before the first frame update
    void Start() {
        player = GetComponent<PlayerStateMachine>();
        cam = GetComponent<CamStateMachine>();
    }

    // Update is called once per frame
    void Update()  {
        if(cam.currentState == cam.states.Aim()) {
            // all icons.SetActive(false);
        }
        else {
            if(interactor.ableToInteract) {
                // Interact "Inside type" icon is active
            }
            else if(glove.looking) {
                // GLove indicator "Insider" icon is active
            }


            
            

            
            


        }
        
    }


    private void SelectGunIcon() {
        // Set the main icon to the icon that will be specific to the current gun, icon will be found in scriptable object
    }
}
