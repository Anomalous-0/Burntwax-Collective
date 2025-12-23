using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCrosshair : MonoBehaviour
{

    [Header("Icon Image References")]
    [SerializeField] Image outerIcon;
    [SerializeField] Image innerIcon;



    [Header("")]
    //Inner
    [SerializeField] Sprite interactIcon;
    Color transparent;


    void Start()
    {
        transparent.a = 0;
        innerIcon.color = transparent;
    }

    public void SetGunCrosshair(GunScriptableObject gun)
    {
        if (gun != null)
        {
            outerIcon.sprite = gun.CrosshairConfig.icon;
            outerIcon.rectTransform.sizeDelta = gun.CrosshairConfig.dimensions;
        }
    }
    public void SetAimCrosshair()
    {
        outerIcon.gameObject.SetActive(false);

        outerIcon.gameObject.SetActive(false);;
    }

    public void ResetAimCrosshair()
    {
        outerIcon.gameObject.SetActive(true);
        innerIcon.gameObject.SetActive(true);
    }

    public void SetInteractCrosshair()
    {
        innerIcon.color = Color.white;
        innerIcon.sprite = interactIcon;

    }

    public void ResetInteractCrosshair()
    {
        innerIcon.color = transparent;
        innerIcon.sprite = null;
    }





}
