// #if CINEMACHINE_2_10_5
using UnityEngine;
using Cinemachine;

namespace Burntwax
{
    public partial class GunStateMachine
    {

        [Header("Cinemachine Required")]
        public CinemachineImpulseSource camShake;

        partial void OnGunSetup_Cinemachine(GameObject currentGunModel)
        {
            camShake = currentGunModel.GetComponent<CinemachineImpulseSource>();
        }
    }
}
// #endif
