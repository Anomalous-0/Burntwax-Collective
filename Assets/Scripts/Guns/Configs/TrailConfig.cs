using System;
using Burntwax.Guns;
using UnityEngine;

[CreateAssetMenu(fileName = "Trail Config", menuName = "Guns/Trail Config", order = 4)]
public class TrailConfig : ScriptableObject, ICloneable{
    public Material Material;
    public AnimationCurve WidthCurve;
    public float Duration = 0.5f;
    public float MinVertexDistance = 0.1f;
    public Gradient Color;

    public float MissDistance = 100f;

    public object Clone() {
        TrailConfig config = CreateInstance<TrailConfig>();
        Utilities.CopyValues(this, config);
        return config;
    }
    // public float SimulationSpeed = 100f;
}
