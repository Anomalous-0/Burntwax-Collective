using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "Gun", menuName = "Guns/Gun", order = 0)]
public class GunScriptableObject : ScriptableObject {

    public int ID;
    public ImpactType ImpactType;
    public GunType Type;
    public string Name;
    public GameObject ModelPrefab;
    // public Vector3 SpawnPoint;
    // public Vector3 SpawnRotation;

    // public Vector3 RestPoint;
    // public Vector3 RestRotation;

    // public Vector3 AimPoint;
    // public Vector3 AimRotation;

    

    [Header("Animation")]

    public AnimationClip weaponAnimation;

    // public RuntimeAnimatorController handController;
    // public Vector3 HandIKRightPos;
    // public Vector3 HandIKLeftPos;

    // public Quaternion HandIKLeftRot;
    // public Quaternion HandIKRightRot;


    // public Vector3 StowPoint;
    // public Vector3 StowRotation;

    public ShootConfig ShootConfig;
    public AmmoConfig AmmoConfig;
    public AudioConfig AudioConfig;
    
    public DamageConfig DamageConfig;
    public TrailConfig TrailConfig;
    public AimConfig AimConfig;
    public bool CamChange;

    private MonoBehaviour ActiveMonoBehaviour;
    [HideInInspector] public GameObject Model;
    private AudioSource ShootingAudioSource;

    public float LastShootTime = 0;
    private ParticleSystem ShootSystem;
    private ObjectPool<TrailRenderer> TrailPool;
    private ObjectPool<Rigidbody> bullets;

    public void Spawn(Transform Parent, MonoBehaviour ActiveMonoBehaviour) {
        this.ActiveMonoBehaviour = ActiveMonoBehaviour;
        LastShootTime = 0; // in editor this will not be properly reset, in build it's fine
        TrailPool = new ObjectPool<TrailRenderer>(CreateTrail);
        //bullets = new ObjectPool<Rigidbody>(InstantiateBullet);

        Model = Instantiate(ModelPrefab);
        Model.transform.SetParent(Parent, false);
        Model.transform.localPosition = Vector3.zero;
        Model.transform.localRotation = Quaternion.identity;
        

        ShootSystem = Model.GetComponentInChildren<ParticleSystem>();
        ShootingAudioSource = Model.GetComponent<AudioSource>();
    }

    public void Despawn(MonoBehaviour ActiveMonoBehaviour) {
        this.ActiveMonoBehaviour = ActiveMonoBehaviour;
        Destroy(Model);

    }

    public void Shoot() {

        
        if (Time.time - LastShootTime > ShootConfig.FireRate) {
            LastShootTime = Time.time;
            

            Vector3 shootDirection = Camera.main.transform.forward + new Vector3(Random.Range(-ShootConfig.Spread.x, ShootConfig.Spread.x), Random.Range(-ShootConfig.Spread.y,ShootConfig.Spread.y),Random.Range(-ShootConfig.Spread.z,ShootConfig.Spread.z));
            shootDirection.Normalize();
            
            if(AmmoConfig.currentClip == 0) {
                AudioConfig.PlayEmptyClip(ShootingAudioSource);
                return; // Empty clip
            }
            
            
            ShootSystem.Play();
            AudioConfig.PlayShootingClip(ShootingAudioSource, AmmoConfig.currentClip == 1);
            AmmoConfig.currentClip--;
            if (Physics.Raycast(Camera.main.transform.position, shootDirection, out RaycastHit hit, float.MaxValue, ShootConfig.HitMask)) {
                ActiveMonoBehaviour.StartCoroutine(PlayTrail(ShootSystem.transform.position, hit.point, hit));
                // Debug.Log("Hit something");
            }
            else {
                ActiveMonoBehaviour.StartCoroutine(PlayTrail(ShootSystem.transform.position, ShootSystem.transform.position + (shootDirection * TrailConfig.MissDistance), new RaycastHit()));
            }
        }
    }

    public bool CanReload() {
        return AmmoConfig.CanReload();
    }

    public void StartReload() {
        AudioConfig.PlayReloadClip(ShootingAudioSource);
    }

    public void EndReload() {
        AmmoConfig.Reload();
    }



   
    public void ADS() {

        // Model.transform.localPosition  = Vector3.Lerp(Model.transform.localPosition, AimPoint, AimConfig.speed * Time.deltaTime);
        // Model.transform.localRotation = Quaternion.Slerp(Model.transform.localRotation, Quaternion.Euler(AimRotation), AimConfig.speed * Time.deltaTime);

    }

    public void Rest() {
        // Model.transform.localPosition  = Vector3.Lerp(Model.transform.localPosition, RestPoint, AimConfig.speed * Time.deltaTime);
        // Model.transform.localRotation = Quaternion.Slerp(Model.transform.localRotation, Quaternion.Euler(RestRotation), AimConfig.speed * Time.deltaTime);
        
    }

    public void Stow() {
    //     Model.transform.localPosition  = Vector3.Lerp(Model.transform.localPosition, StowPoint, AimConfig.speed * Time.deltaTime);
    //     Model.transform.localRotation = Quaternion.Slerp(Model.transform.localRotation, Quaternion.Euler(StowRotation), AimConfig.speed * Time.deltaTime);
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
            remainingDistance -= TrailConfig.SimulationSpeed * Time.deltaTime;

            yield return null;
        }

        instance.transform.position = EndPoint;

        if (Hit.collider != null) {
          
            if(Hit.collider.GetComponent<Rigidbody>()) {
                Hit.collider.GetComponent<Rigidbody>().AddForce((EndPoint - StartPoint).normalized * ShootConfig.bulletForce, ForceMode.Impulse);
            }
            // Debug.Log("Hit something");
            // SurfaceManager.Instance.HandleImpact(
            //     Hit.transform.gameObject,
            //     EndPoint,
            //     Hit.normal,
            //     ImpactType,
            //     0
            // );
        }

        if(Hit.collider.TryGetComponent(out Damageable damageable)) {
           damageable.TakeDamage(DamageConfig.GetDamage(distance));
        }

        yield return new WaitForSeconds(TrailConfig.Duration);
        yield return null;
        instance.emitting = false;
        instance.gameObject.SetActive(false);
        TrailPool.Release(instance);
    }

    private TrailRenderer CreateTrail() {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        trail.colorGradient = TrailConfig.Color;
        trail.material = TrailConfig.Material;
        trail.widthCurve = TrailConfig.WidthCurve;
        trail.time = TrailConfig.Duration;
        trail.minVertexDistance = TrailConfig.MinVertexDistance;

        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return trail;
    }

}
