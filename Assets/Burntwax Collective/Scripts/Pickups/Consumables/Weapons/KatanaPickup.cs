using UnityEngine;

namespace Burntwax
{
    [RequireComponent(typeof(Rigidbody))]
    public class KatanaPickup : MonoBehaviour
    {

        [SerializeField] KatanaScriptableObject katana;
        private KatanaStateMachine weaponSelector;


        void Awake()
        {
        }
        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerStateMachine>() != false)
            {
                weaponSelector = FindFirstObjectByType<KatanaStateMachine>();
                if (weaponSelector.ActiveKatanaScriptable == null)
                {
                    weaponSelector.Pickup(katana);
                }
                
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }

    }
}

