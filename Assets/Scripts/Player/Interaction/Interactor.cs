using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;


public class Interactor : MonoBehaviour {

    [SerializeField] float raycastDistance = 3f;
    [SerializeField] LayerMask interactableLayer;
    public InputManager input;
    public CamStateMachine cam;
    public bool ableToInteract;
    Interactable interactable;
    [SerializeField] GameObject defaultIcon;
    [SerializeField] GameObject interactIcon;
    [SerializeField] GameObject aimIcon;

    [SerializeField] GloveStateMachine glove;

    // Start is called before the first frame update
    void Awake(){
       
    }

    // Update is called once per frame
    void Update() {

        RaycastHit hit;
        if (cam.currentState != cam.states.Aim() && Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance, interactableLayer)) {
            
            //Crosshair update         
                ableToInteract = true;

            //Actual interaction
            if (hit.collider.GetComponent<Interactable>()) {
                if (interactable == null || interactable.ID != hit.collider.GetComponent<Interactable>().ID) {
                    interactable = hit.collider.GetComponent<Interactable>();

                }
                if (input.interactIsPressed){
                    // Debug.Log("Interacted");
                    interactable.onInteract.Invoke();

                    if(!interactable.continuous) {
                        input.interactIsPressed = false;
                    }
                    
                }
            }
        } 
            
            // if (interactImage.sprite != defaultIcon) {
            //     interactImage.sprite = defaultIcon;
            // }
        
    }
}
