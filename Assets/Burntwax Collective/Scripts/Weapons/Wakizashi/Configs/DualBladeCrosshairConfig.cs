using System;
using Burntwax.Guns;
using UnityEngine;

namespace Burntwax
{

    [CreateAssetMenu(fileName = "DualBlade Crosshair Config", menuName = "DualBlade/Crosshair Config", order = 6)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Burntwax", "Burntwax.Core", "KatanaCrosshairConfig")]
    public class DualBladeCrosshairConfig : ScriptableObject
    {
        public Sprite icon;

        public Vector2 dimensions;

       
    }

}