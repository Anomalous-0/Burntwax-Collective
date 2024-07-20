using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;


public class Interactor : MonoBehaviour
{

    [SerializeField] float raycastDistance = 3f;
    [SerializeField] LayerMask interactableLayer;
    public InputManager input;
    Interactable interactable;
    [SerializeField] GameObject defaultIcon;
    [SerializeField] GameObject interactIcon;
    [SerializeField] GameObject aimIcon;

    [SerializeField] Glove glove;

    // Start is called before the first frame update
    void Awake()
    {
        defaultIcon.SetActive(true);
        interactIcon.SetActive(false);
        aimIcon.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance, interactableLayer)) {
            
            //Crosshair update         
            
            
                interactIcon.SetActive(true);
                defaultIcon.SetActive(true);
                aimIcon.SetActive(false);
        

            //Actual interaction
            if (hit.collider.GetComponent<Interactable>() != false) {
                if (interactable == null || interactable.ID != hit.collider.GetComponent<Interactable>().ID) {
                    interactable = hit.collider.GetComponent<Interactable>();

                }

                

                // if (interactable.interactIcon != null) {
                //     interactImage.sprite = interactable.interactIcon;
                // } else {
                //     interactImage.sprite = defaultInteractIcon;
                // }

                if (input.interactIsPressed && !input.aimIsPressed){
                    // Debug.Log("Interacted");
                    interactable.onInteract.Invoke();

                    if(!interactable.continuous) {
                        input.interactIsPressed = false;
                    }
                    
                }
            }
        } else {
            if(glove.looking) {
                interactIcon.SetActive(true);
                defaultIcon.SetActive(true);
                aimIcon.SetActive(false);
            }
            else if (input.aimIsPressed) {
                defaultIcon.SetActive(false);
                aimIcon.SetActive(true);
                interactIcon.SetActive(false);
            } else {
                defaultIcon.SetActive(true);
                aimIcon.SetActive(false);
                interactIcon.SetActive(false);
            }
            
            // if (interactImage.sprite != defaultIcon) {
            //     interactImage.sprite = defaultIcon;
            // }
        }
    }
}
