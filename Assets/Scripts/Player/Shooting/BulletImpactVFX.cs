using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SurfaceVFXPair
{
    public string tag;
    public GameObject vfxPrefab;
}

public class BulletImpactVFX : MonoBehaviour
{
    public List<SurfaceVFXPair> surfaceVFX;

    private Dictionary<string, GameObject> vfxDict;

    private void Awake()
    {
        vfxDict = new Dictionary<string, GameObject>();
        foreach (var pair in surfaceVFX)
        {
            if (!vfxDict.ContainsKey(pair.tag))
            {
                vfxDict.Add(pair.tag, pair.vfxPrefab);
            }
        }
    }

    public void SpawnImpactVFX(string tag, Vector3 position, Quaternion rotation) {
        if (vfxDict.TryGetValue(tag, out var prefab)){
            Instantiate(prefab, position, rotation);
            //Destroy(, GetComponent<ParticleSystem>().main.duration); 
        }
    }
}
