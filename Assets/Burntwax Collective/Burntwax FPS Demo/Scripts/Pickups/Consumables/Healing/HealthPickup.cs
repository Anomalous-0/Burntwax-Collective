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
            // if (GameManager.Instance.IsLoadingFromSave)
            //     return;
            // if (ObjectManager.Instance == null)
            //     return;
            // ObjectManager.Instance.Register(gameObject);
        }

        //Collect health
        void OnTriggerEnter(Collider other)
        {

            if (other.GetComponentInParent<PlayerHealth>() != false)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>();
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

