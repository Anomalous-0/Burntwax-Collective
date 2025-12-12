using UnityEngine;

public class NewBehaviourScript : MonoBehaviour {

    public int heal;
    [SerializeField] private string guid;

    private PlayerHealth playerHealth;

    void Start()
    {
        guid = System.Guid.NewGuid().ToString();
        ConsumableManager.Instance.Register(gameObject);
    }

    void OnTriggerEnter(Collider other) {
        
        if(other.GetComponentInParent<PlayerHealth>() != false || other.GetComponent<ConsumableGlovePickup>()) {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if(playerHealth.CurrentHealth() < playerHealth.maxHealth) {
                playerHealth.RestoreHealth(heal);
                Destroy(gameObject);
            }
            
        }
    }
   
}
