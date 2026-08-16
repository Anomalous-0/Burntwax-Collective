using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Burntwax
{

    [CreateAssetMenu(fileName = "Katana", menuName = "Katana/Katana", order = 0)]
    public class KatanaScriptableObject : ScriptableObject
    {

        

        [Header("General")]

        [HideInInspector] public GameObject Model;


        [Header("Configs")]
        public KatanaDamageConfig DamageConfig;
        public KatanaAudioConfig AudioConfig;
        public KatanaCrosshairConfig CrosshairConfig;

        private MonoBehaviour ActiveMonoBehaviour;

        
        private ObjectPool<TrailRenderer> TrailPool;



        public void Spawn(GameObject player, GameObject currentGun, MonoBehaviour ActiveMonoBehaviour)
        {
            this.ActiveMonoBehaviour = ActiveMonoBehaviour;
            // if (TrailPool != null)
            // {
            //     TrailPool.Dispose();
            // }
            // TrailPool = new ObjectPool<TrailRenderer>(CreateTrail);
            
        }

        public void Despawn(MonoBehaviour ActiveMonoBehaviour)
        {
            this.ActiveMonoBehaviour = ActiveMonoBehaviour;
            // TrailPool.Dispose();
        }

        public void Slash()
        {
            
        }

        public void Parry()
        {
            
        }

    }
}