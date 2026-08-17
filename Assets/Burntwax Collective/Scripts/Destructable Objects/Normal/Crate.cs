using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Burntwax
{

    /// <summary>
    /// One-component crate specialization. It inherits reusable health and break
    /// rules from Breakable and adds the crate's loot behavior.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Crate : Breakable
    {
        [Header("Loot")]
        [SerializeField, Min(0)] private int itemsToSpawn = 3;
        public List<GameObject> resources_prefabs;

        protected override void OnBreak(DamageInfo cause)
        {
            if (itemsToSpawn <= 0
                || resources_prefabs == null
                || resources_prefabs.Count == 0)
            {
                return;
            }

            Vector3 spawnPosition = transform.position;

            for (int i = 0; i < itemsToSpawn; i++)
            {
                GameObject prefab = resources_prefabs[
                    Random.Range(0, resources_prefabs.Count)];
                if (prefab == null)
                    continue;

                GameObject spawned = Instantiate(
                    prefab,
                    spawnPosition,
                    Quaternion.identity);
                ObjectManager.Instance?.Register(spawned);
            }
        }
    }
}
