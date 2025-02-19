using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "Gun", menuName = "Guns/Gun", order = 0)]
public class GunScriptableObject : ScriptableObject, ICloneable {

    public int ID;
    
    
    
    [Header("General")]
    public GunType Type;
    //public ImpactType ImpactType;
    public string Name;
    
    // public GameObject ModelPrefab;
    public bool incrementalReload;
    [HideInInspector] public GameObject Model;


    [Header("Configs")]
    public ShootConfig ShootConfig;
    public AmmoConfig AmmoConfig;
    public AudioConfig AudioConfig;
    public DamageConfig DamageConfig;
    public TrailConfig TrailConfig;
    public CrosshairConfig CrosshairConfig;


    private MonoBehaviour ActiveMonoBehaviour;
    private AudioSource ShootingAudioSource;
    private ParticleSystem ShootSystem;
    private ObjectPool<TrailRenderer> TrailPool;

    public void Spawn(GameObject currentGun, MonoBehaviour ActiveMonoBehaviour) {
        this.ActiveMonoBehaviour = ActiveMonoBehaviour;
        TrailPool = new ObjectPool<TrailRenderer>(CreateTrail);
        ShootSystem = currentGun.GetComponentInChildren<ParticleSystem>();
        ShootingAudioSource = currentGun.GetComponent<AudioSource>();
    }

    public void Despawn(MonoBehaviour ActiveMonoBehaviour) {
        this.ActiveMonoBehaviour = ActiveMonoBehaviour;
        TrailPool.Clear();

    }

    public void Shoot() {
        ShootSystem.Play();
        AudioConfig.PlayShootingClip(ShootingAudioSource, AmmoConfig.currentClip == 1);
        AmmoConfig.currentClip--;
        for(int i = 0; i < ShootConfig.bulletsPerShot;i++) {
            Vector3 shootDirection = Camera.main.transform.forward + new Vector3(Random.Range(-ShootConfig.Spread.x, ShootConfig.Spread.x), Random.Range(-ShootConfig.Spread.y,ShootConfig.Spread.y),Random.Range(-ShootConfig.Spread.z,ShootConfig.Spread.z));
            shootDirection.Normalize();


            
            if (Physics.Raycast(Camera.main.transform.position, shootDirection, out RaycastHit hit, float.MaxValue, ShootConfig.HitMask)) {
                ActiveMonoBehaviour.StartCoroutine(PlayTrail(ShootSystem.transform.position, hit.point, hit));
                // Debug.Log("Hit something");
            }
            else {
                ActiveMonoBehaviour.StartCoroutine(PlayTrail(ShootSystem.transform.position, ShootSystem.transform.position + (shootDirection * TrailConfig.MissDistance), new RaycastHit()));
            // }
            }
        }

            
    }

    public bool CanReload() {
        return AmmoConfig.CanReload();
    }

    public void StartReloadAudio() {
        AudioConfig.PlayReloadClip(ShootingAudioSource);
    }

    public void EndReload() {
        AmmoConfig.Reload();
    }

    public void EndIncrementalReload() {
        AmmoConfig.IncrementalReload();
    }

    private IEnumerator InstantiateBullet() {
        yield return null;
    }
    private IEnumerator PlayTrail(Vector3 StartPoint, Vector3 EndPoint, RaycastHit Hit) {
        TrailRenderer instance = TrailPool.Get();
        instance.gameObject.SetActive(true);
        instance.transform.position = StartPoint;
        yield return null; // avoid position carry-over from last frame if reused

        instance.emitting = true;

        float distance = Vector3.Distance(StartPoint, EndPoint);
        float remainingDistance = distance;
        while (remainingDistance > 0) {
            instance.transform.position = Vector3.Lerp(
                StartPoint,
                EndPoint,
                Mathf.Clamp01(1 - (remainingDistance / distance))
            );
            remainingDistance -= ShootConfig.bulletSpeed * Time.deltaTime;

            yield return null;
        }

        instance.transform.position = EndPoint;

        if (Hit.collider != null) {
            // SurfaceManager.Instance.HandleImpact(
            //     Hit.transform.gameObject,
            //     EndPoint,
            //     Hit.normal,
            //     ImpactType,
            //     0
            // );
            if(Hit.collider.GetComponent<Rigidbody>()) {
                Hit.collider.GetComponent<Rigidbody>().AddForce((EndPoint - StartPoint).normalized * DamageConfig.GetDamage(distance)/2, ForceMode.Impulse);
            }

        }

        if(Hit.collider.TryGetComponent(out Destructable destructable)) {
           destructable.TakeDamage(DamageConfig.GetDamage(distance));
        }

        yield return new WaitForSeconds(TrailConfig.Duration);
        yield return null;
        // instance.emitting = false;
        // instance.gameObject.SetActive(false);
        // TrailPool.Release(instance);
    }

    private TrailRenderer CreateTrail() {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        trail.colorGradient = TrailConfig.Color;
        trail.material = TrailConfig.Material;
        trail.widthCurve = TrailConfig.WidthCurve;
        trail.time = TrailConfig.Duration;
        trail.minVertexDistance = TrailConfig.MinVertexDistance;
        trail.autodestruct = true;
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return trail;
    }

    public object Clone() {
        GunScriptableObject config = CreateInstance<GunScriptableObject>();

        config.ID = ID;
        //config.ImpactType = ImpactType;
        config.Type = Type;
        config.Name = Name;
        // config.ModelPrefab = ModelPrefab;
        config.incrementalReload = incrementalReload;
        config.ShootConfig = ShootConfig.Clone() as ShootConfig;
        config.AmmoConfig = AmmoConfig.Clone() as AmmoConfig;
        config.AudioConfig = AudioConfig.Clone() as AudioConfig;
        config.DamageConfig = DamageConfig.Clone() as DamageConfig;
        config.TrailConfig = TrailConfig.Clone() as TrailConfig;
        config.CrosshairConfig = CrosshairConfig.Clone() as CrosshairConfig;

        return config;

    }
}
