using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NPCInteract : MonoBehaviour {
    

    bool inDanger;

    // Start is called before the first frame update
    void Start() {
        inDanger = false;
    }

    // Update is called once per frame
    void Update() {
        
    }

    public void Interact(){
        if (!inDanger) {
            
        } else {
            FollowDangerPath();
        }
    }

    private void FollowDangerPath() {
        // Update targetPos to some place that is a rendevous in the story
    }

    public void PutInDanger() {
        inDanger = true;
    }

    public void RemoveDanger() {
        inDanger = false;
    }
    
}