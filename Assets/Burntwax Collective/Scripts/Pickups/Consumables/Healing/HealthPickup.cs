using UnityEngine;

namespace Burntwax
{

    [RequireComponent(typeof(Rigidbody))]
    public class HealthPickup : MonoBehaviour
    {

        public int heal;


        private PlayerHealth playerHealth;

        void Awake()
        {
            
        }

        //Collect health
        void OnTriggerEnter(Collider other)
        {

            if (other.GetComponentInParent<PlayerHealth>() != false)
            {
                playerHealth = FindAnyObjectByType<PlayerHealth>();
                if (playerHealth.CurrentHealth() < playerHealth.maxHealth)
                {
                    playerHealth.RestoreHealth(heal);
                    gameObject.SetActive(false);
                    Destroy(gameObject);
                }

            }
        }

    }

}

