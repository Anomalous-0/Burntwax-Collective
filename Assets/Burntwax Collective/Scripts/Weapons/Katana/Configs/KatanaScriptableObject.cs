using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Burntwax
{

    [CreateAssetMenu(fileName = "Katana", menuName = "Katana/Katana", order = 0)]
    public class KatanaScriptableObject : ScriptableObject
    {

        

        [Tooltip("Stable save/load ID. Every katana asset must use a unique value.")]
        public int ID;

        [Header("General")]

        [HideInInspector] public GameObject Model;

        [Header("Dash")]
        [Min(1)] public int MaxDashCharges = 3;
        [Min(0f)] public float DashSpeed = 20f;
        [Min(0.01f)] public float DashDuration = 0.2f;
        [Min(0.01f)] public float DashRechargeDuration = 2f;


        [Header("Configs")]
        public KatanaDamageConfig DamageConfig;
        public KatanaImpactConfig ImpactConfig;
        public KatanaParryConfig ParryConfig;
        public KatanaGestureConfig GestureConfig;
        [FormerlySerializedAs("AudioConfig")]
        public KatanaCombatFeedbackConfig FeedbackConfig;
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
