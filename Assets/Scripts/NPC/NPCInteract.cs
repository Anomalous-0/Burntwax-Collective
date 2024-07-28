using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NPCInteract : MonoBehaviour {
    private float health;
    private float lerpTimer;
    public float maxHealth = 100;
    public float chipSpeed = 2.0f;

    public Image frontHealthBar;
    public Image backHealthBar;
    // Start is called before the first frame update
    void Start() {
        health = maxHealth;
    }

    // Update is called once per frame
    void Update() {
        
        
    }

    public void UpdateHealthUI(){
        
    }

    public void TakeDamage(float damage){
        Debug.Log("hurting like parth");
    }

    public void RestoreHealth(float healAmount){
        Debug.Log("healing");
    }

    public float CurrentHealth() {
        return 1f;
    }
}