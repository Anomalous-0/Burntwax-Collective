using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class InputPrioritySorter : MonoBehaviour {

    [SerializeField] private InputManager input;

    String sprint = "sprint";
    String aim = "aim";
    String shoot = "shoot";

    
    List<string> inputPriority = new List<string>();
    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update(){
        if(input.aimIsPressed && !inputPriority.Contains(aim)) {
            // Debug.Log("Should add aim to list");
            inputPriority.Add(aim);
        }
        else if(!input.aimIsPressed && inputPriority.Contains(aim)) {
            // Debug.Log("Deleted aim from list");
            inputPriority.Remove(aim);
        }

        if(input.sprintIsPressed && !inputPriority.Contains(sprint)) {
            // Debug.Log("Should add sprint to list");
            inputPriority.Add(sprint);
        }
        else if(!input.sprintIsPressed && inputPriority.Contains(sprint)) {
            // Debug.Log("Deleted sprint from list");
            inputPriority.Remove(sprint);
        }
        if(input.shootIsPressed && !inputPriority.Contains(shoot)) {
            // Debug.Log("Should add shoot to list");
            inputPriority.Add(shoot);
        }
        else if(!input.shootIsPressed && inputPriority.Contains(shoot)) {
            // Debug.Log("Deleted shoot from list");
            inputPriority.Remove(shoot);
        }

        if(inputPriority.Count == 1) {
            print(inputPriority[0]);
        }
        else if(inputPriority.Count == 2) {
            print(inputPriority[0] + " " + inputPriority[1]);
        }
        else if(inputPriority.Count == 3) {
            print(inputPriority[0] + " " + inputPriority[1] + " " + inputPriority[2]);   
        }

        
        
    }

    public bool SprintIsPriority() {
        return inputPriority.IndexOf(sprint) == inputPriority.Count-1 && inputPriority.Count >= 1;
    }

    public bool AimIsPriority() {
        return inputPriority.IndexOf(aim) == inputPriority.Count-1 && inputPriority.Count >= 1;
    }

    public bool ShootIsPriority() {
        return inputPriority.IndexOf(shoot) == inputPriority.Count-1 && inputPriority.Count >= 1;
    }

}
