using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempObject : MonoBehaviour {

    public float time = 10;
    void Start(){
        DestroyObject();
    }

    

    private IEnumerator DestroyObject(){
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}
