using UnityEngine;

public class PrefabGuid : MonoBehaviour
{
    public string guid;

    [ContextMenu("Generate guid")]
    private void GenerateGuid()
    {
        guid = System.Guid.NewGuid().ToString();
    }
}
