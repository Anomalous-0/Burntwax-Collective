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

       

        // private IEnumerator PlayTrail(List<string> surfaces, int i, Vector3 StartPoint, Vector3 EndPoint, RaycastHit Hit)
        // {
        //     TrailRenderer instance = TrailPool.Get();
        //     instance.gameObject.SetActive(true);
        //     instance.transform.position = StartPoint;
        //     yield return null; // avoid position carry-over from last frame if reused

        //     instance.emitting = true;

        //     float distance = Vector3.Distance(StartPoint, EndPoint);
        //     float remainingDistance = distance;
        //     while (remainingDistance > 0)
        //     {
        //         instance.transform.position = Vector3.Lerp(
        //             StartPoint,
        //             EndPoint,
        //             Mathf.Clamp01(1 - (remainingDistance / distance))
        //         );
        //         remainingDistance -= ShootConfig.bulletSpeed * Time.deltaTime;

        //         yield return null;
        //     }

        //     instance.transform.position = EndPoint;

        //     if (Hit.collider != null)
        //     {

        //         if (Hit.collider.GetComponent<Rigidbody>())
        //         {
        //             Hit.collider.GetComponent<Rigidbody>().AddForce((EndPoint - StartPoint).normalized * DamageConfig.GetDamage(distance), ForceMode.Impulse);
        //         }
        //         if (Hit.collider.TryGetComponent(out Destructable destructable))
        //         {
        //             destructable.TakeDamage(DamageConfig.GetDamage(distance) / 2);
        //         }
        //         if (Hit.collider.TryGetComponent(out Target target))
        //         {
        //             target.Teleport();
        //         }

        //         string surfaceTag = Hit.collider.tag;
        //         Vector3 impactPosition = Hit.point + Hit.normal * 0.01f;
        //         Quaternion impactRotation = Quaternion.LookRotation(Hit.normal);
        //         BulletImpactEffects.Instance.ImpactVFX(surfaceTag, impactPosition, impactRotation);
        //         if (!surfaces.Contains(surfaceTag))
        //         {
        //             surfaces.Add(surfaceTag);
        //             BulletImpactEffects.Instance.ImpactAudio(surfaceTag, impactPosition, impactRotation);
        //         }



        //     }



        //     yield return new WaitForSeconds(TrailConfig.Duration);
        //     yield return null;
        //     instance.emitting = false;
        //     instance.gameObject.SetActive(false);
        //     TrailPool.Release(instance);
        // }

        // private TrailRenderer CreateTrail()
        // {
        //     GameObject instance = new GameObject("Bullet Trail");
        //     TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        //     trail.colorGradient = TrailConfig.Color;
        //     trail.material = TrailConfig.Material;
        //     trail.widthCurve = TrailConfig.WidthCurve;
        //     trail.time = TrailConfig.Duration;
        //     trail.minVertexDistance = TrailConfig.MinVertexDistance;
        //     trail.emitting = false;
        //     trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        //     return trail;
        // }
    }
}