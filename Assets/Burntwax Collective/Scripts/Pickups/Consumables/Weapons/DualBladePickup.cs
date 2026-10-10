using UnityEngine;

namespace Burntwax
{
    [RequireComponent(typeof(Rigidbody))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaPickup")]
    public class DualBladePickup : MonoBehaviour
    {

        [UnityEngine.Serialization.FormerlySerializedAs("katana")]
        [SerializeField] DualBladeScriptableObject dualBlade;
        private DualBladeStateMachine weaponSelector;


        void Awake()
        {
        }
        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerStateMachine>() != false)
            {
                weaponSelector = FindAnyObjectByType<DualBladeStateMachine>();
                if (weaponSelector.ActiveDualBladeScriptable == null)
                {
                    weaponSelector.Pickup(dualBlade);
                }
                
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }

    }
}

