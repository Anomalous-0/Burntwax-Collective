using UnityEngine;

public class Consumable : MonoBehaviour {
    // Start is called once before the first execution of Update after the MonoBehaviour is created
  

    // Update is called once per frame
    public void Save(ref ConsumableData data) {
        data.position = transform.position;
        data.rotation = transform.rotation;
        data.velocity = GetComponent<Rigidbody>().linearVelocity;
    }

    public void Load(ConsumableData data) {
        transform.position = data.position;
        transform.rotation = data.rotation;
        GetComponent<Rigidbody>().linearVelocity = data.velocity;
    }
}


[System.Serializable]

public struct ConsumableData {
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;    
    
}
