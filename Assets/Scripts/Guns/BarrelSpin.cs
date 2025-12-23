using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrelSpin : MonoBehaviour
{
    public int deg = 60;
    public GunStateMachine gun;


    void Update()
    {
        if (gun.CurrentState == gun.states.Shoot())
        {
            Spin();
        }
    }
    private void Spin()
    {
        transform.Rotate(0, 0, transform.rotation.z - deg);

    }
}
