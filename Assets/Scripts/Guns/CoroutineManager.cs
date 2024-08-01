using UnityEngine;

public class CoroutineManager : MonoBehaviour {
    private static CoroutineManager instance;

    public static CoroutineManager Instance {
        get {
            if (instance == null) {
                GameObject go = new GameObject("CoroutineManager");
                instance = go.AddComponent<CoroutineManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
}