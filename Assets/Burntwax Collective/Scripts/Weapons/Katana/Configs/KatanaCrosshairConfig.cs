using System;
using Burntwax.Guns;
using UnityEngine;

namespace Burntwax
{

    [CreateAssetMenu(fileName = "Katana Crosshair Config", menuName = "Katana/Crosshair Config", order = 6)]
    public class KatanaCrosshairConfig : ScriptableObject
    {
        public Sprite icon;

        public Vector2 dimensions;

       
    }

}