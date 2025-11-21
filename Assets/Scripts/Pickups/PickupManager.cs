using UnityEngine;

public class PickupManager : MonoBehaviour {
    
    public static PickupManager Instance;

    private void Awake() {
        if(Instance != null) {
            Debug.LogError("Found more than one Pickup Manager in the scene");
        }
        Instance = this;
    }

    public void Register(string guid) {
    
    }
}
