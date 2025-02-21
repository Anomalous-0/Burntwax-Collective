using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ProceduralRecoil : MonoBehaviour {
    
    Vector3 currentRotation, targetRotation, targetPosition, currentPosition, initialGunPosition;


    [SerializeField] float recoilX;
    [SerializeField] float recoilY;
    [SerializeField] float recoilZ;
    [SerializeField] float kickBackZ;

    public float snap, returnAmount;

    void Start() {
        initialGunPosition = transform.localPosition;
    }

    void Update() {
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, Time.deltaTime) * returnAmount;
        currentRotation = Vector3.Slerp(currentRotation, targetRotation,Time.fixedDeltaTime * snap);
        transform.localRotation = Quaternion.Euler(currentRotation);
        Back();
    }

    public void Recoil() {
        targetPosition -= new Vector3(0,0,kickBackZ);
        targetRotation += new Vector3(recoilX, Random.Range(-recoilY, recoilY), recoilZ);
        Debug.Log("Recoil");
    }

    private void Back() {
        targetPosition = Vector3.Lerp(targetPosition, initialGunPosition, Time.deltaTime* returnAmount);
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.fixedDeltaTime * snap);
        transform.localPosition = currentPosition;
    }
}
