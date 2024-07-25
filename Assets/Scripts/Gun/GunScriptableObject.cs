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
    public Vector3 SpawnPoint;
    public Vector3 SpawnRotation;

    public ShootConfig ShootConfig;
    public TrailConfig TrailConfig;
    public AimConfig AimConfig;
    public RecoilConfig RecoilConfig;
    public bool CamChange;

    private MonoBehaviour ActiveMonoBehaviour;
    private GameObject Model;

    private float LastShootTime = 0;
    private ParticleSystem ShootSystem;
    private ObjectPool<TrailRenderer> TrailPool;
    private ObjectPool<Rigidbody> bullets;
    private bool isADS;
    private bool isRecoiling;

    public void Spawn(Transform Parent, MonoBehaviour ActiveMonoBehaviour) {
        this.ActiveMonoBehaviour = ActiveMonoBehaviour;
        LastShootTime = 0; // in editor this will not be properly reset, in build it's fine
        TrailPool = new ObjectPool<TrailRenderer>(CreateTrail);
        //bullets = new ObjectPool<Rigidbody>(InstantiateBullet);

        Model = Instantiate(ModelPrefab);
        Model.transform.SetParent(Parent, false);
        Model.transform.localPosition = SpawnPoint;
        Model.transform.localRotation = Quaternion.Euler(SpawnRotation);

        ShootSystem = Model.GetComponentInChildren<ParticleSystem>();
    }

    public void Despawn(MonoBehaviour ActiveMonoBehaviour) {
        this.ActiveMonoBehaviour = ActiveMonoBehaviour;
        Destroy(Model);

    }

    public void Shoot() {

        if (Model == null)
        {
            Debug.LogError("Model is not assigned!");
            return;
        }

        // Debug.Log(LastShootTime + " " + ShootConfig.FireRate + " " + (ShootConfig.FireRate + LastShootTime) + " " + Time.time);
        if (Time.time - LastShootTime >= ShootConfig.FireRate)
        {
            LastShootTime = Time.time;
            // Implement shooting logic here (raycasting, bullet instantiation, etc.)
            // Apply recoil
            
            ApplyRecoil();

            Vector3 shootDirection = Camera.main.transform.forward
                + new Vector3(
                    Random.Range(
                        -ShootConfig.Spread.x,
                        ShootConfig.Spread.x
                    ),
                    Random.Range(
                        -ShootConfig.Spread.y,
                        ShootConfig.Spread.y
                    ),
                    Random.Range(
                        -ShootConfig.Spread.z,
                        ShootConfig.Spread.z
                    )
                );
            shootDirection.Normalize();
            
            // Debug.Log(Model.transform.localRotation.x);

            // if (Mathf.Abs(Model.transform.localRotation.x) < /*Hard coded change later*/ 0.01f) {
            //     Model.transform.Rotate(/*Hard coded change later*/ -1/*RecoilConfig.RecoilAngle*/, 0, 0);
            // } else {
            //     Model.transform.localRotation = Quaternion.Euler(
            //         Model.transform.localRotation.x /*-*/ /*Hard coded change later*//*RecoilConfig.RecoilAngle*/,
            //         Model.transform.localRotation.y,
            //         Model.transform.localRotation.z
            //     );
            // }

            // Model.transform.Translate(0, 0, /*Hard coded change later*/-0.02f);

            // Recoil();

            if (Physics.Raycast(Camera.main.transform.position, shootDirection, out RaycastHit hit, float.MaxValue, ShootConfig.HitMask)) {
                ActiveMonoBehaviour.StartCoroutine(PlayTrail(ShootSystem.transform.position, hit.point, hit));
                // Debug.Log("Hit something");
            }
            else {
                ActiveMonoBehaviour.StartCoroutine(PlayTrail(ShootSystem.transform.position, ShootSystem.transform.position + (shootDirection * TrailConfig.MissDistance), new RaycastHit()));
            }
        }
    }

    private void Recoil() {
        
        float timeElapsed = Time.time - LastShootTime;

        //Model.transform.localRotation = Quaternion.Slerp(localRotation, );
        //Model.transform.localPosition = Vector3.Lerp();

    }

    // public void ReturnToInitial() {
    //     if (Time.time - LastShootTime < 0.25f/*RecoilConfig.RecoveryTime*/) {
    //         Model.transform.localRotation = Quaternion.Slerp(
    //             Model.transform.localRotation,
    //             Quaternion.Euler(SpawnRotation),
    //             Time.deltaTime /*Hard coded change later*/ 
    //         );
    //         Model.transform.localPosition = Vector3.Lerp(
    //             Model.transform.localPosition,
    //             (isADS) ? AimConfig.aimPoint : SpawnPoint,
    //             Time.deltaTime /*Hard coded change later*/ 
    //         );
    //     }
    // }

    
    private void ApplyRecoil()
    {
        if (Model != null)
        {
            // Calculate the target displacement and rotation for recoil
            Vector3 recoilDisplacement = Model.transform.localPosition + Vector3.back * RecoilConfig.RecoilDistance;
            Quaternion recoilRotation = Quaternion.Euler(Model.transform.localRotation.eulerAngles + Vector3.right * RecoilConfig.RecoilAngle);

            // Start the recoil coroutine
            CoroutineManager.Instance.StartCoroutine(RecoilCoroutine(recoilDisplacement, recoilRotation));
        }
    }

    private IEnumerator RecoilCoroutine(Vector3 targetPosition, Quaternion targetRotation)
    {
        isRecoiling = true;
        float elapsedTime = 0;

        Vector3 initialPosition = Model.transform.localPosition;
        Quaternion initialRotation = Model.transform.localRotation;

        while (elapsedTime < RecoilConfig.RecoilTime)
        {
            Model.transform.localPosition = Vector3.Lerp(initialPosition, targetPosition, Mathf.Pow(elapsedTime / RecoilConfig.RecoilTime, 0.2f));
            Model.transform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, Mathf.Pow(elapsedTime / RecoilConfig.RecoilTime, 0.2f));

            if (CamChange){
                Camera.main.transform.localRotation = Model.transform.localRotation;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Model.transform.localPosition = targetPosition;
        Model.transform.localRotation = targetRotation;
        isRecoiling = false;
    }
    
    public void ReturnToInitial()
    {
        if (Model != null /*&& !isRecoiling*/)
        {
            // Linearly interpolate the position
            Model.transform.localPosition = Vector3.Lerp(Model.transform.localPosition, (isADS) ? AimConfig.aimPoint : SpawnPoint, Time.deltaTime / RecoilConfig.RecoveryTime);
            
            // Linearly interpolate the rotation
            Quaternion targetRotation = Quaternion.Euler(SpawnRotation);
            Model.transform.localRotation = Quaternion.Slerp(Model.transform.localRotation, targetRotation, Time.deltaTime / RecoilConfig.RecoveryTime);
        }
    }

    public void ADS() {

        Model.transform.localPosition  = Vector3.Lerp(Model.transform.localPosition, AimConfig.aimPoint, AimConfig.speed * Time.deltaTime);
        isADS = true;

    }

    public void DeADS() {
        Model.transform.localPosition  = Vector3.Lerp(Model.transform.localPosition, SpawnPoint, AimConfig.speed * Time.deltaTime);
        isADS = false;
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
            // SurfaceManager.Instance.HandleImpact(
            //     Hit.transform.gameObject,
            //     EndPoint,
            //     Hit.normal,
            //     ImpactType,
            //     0
            // );
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
