//#if ANIMATION_RIGGING
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Burntwax
{
    public partial class GunStateMachine
    {

        [Header("Animation Rigging Required")]
        public Rig handIK;

        partial void OnGunSetup_Rigging(GameObject currentGunModel)
        {
            if (handIK != null)
                handIK.weight = 1;
        }
    }
}
//#endif
