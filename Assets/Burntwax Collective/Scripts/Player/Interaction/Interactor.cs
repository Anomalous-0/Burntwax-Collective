using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;


public class Interactor : MonoBehaviour
{


    public CamStateMachine cam;
    public PlayerCrosshair crosshair;

    [SerializeField] float raycastDistance = 3f;
    [SerializeField] LayerMask interactableLayer;
    public InputManager input;


    Interactable interactable;

    void Update()
    {

        RaycastHit hit;
        if (cam.currentState != cam.states.Aim() && Physics.Raycast(transform.position, transform.forward, out hit, raycastDistance, interactableLayer))
        {
            crosshair.SetInteractCrosshair();


            if (hit.collider.GetComponent<Interactable>())
            {
                if (interactable == null || interactable.ID != hit.collider.GetComponent<Interactable>().ID)
                {
                    interactable = hit.collider.GetComponent<Interactable>();

                }
                if (input.interactIsPressed)
                {
                    interactable.onInteract.Invoke();

                    if (!interactable.continuous)
                    {
                        input.interactIsPressed = false;
                    }

                }
            }
        }
        else
        {
            crosshair.ResetInteractCrosshair();
        }
    }
}
