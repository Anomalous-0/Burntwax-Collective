using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

[VolumeComponentMenuForRenderPipeline("Custom/Ben Day Bloom", typeof(UniversalRenderPipeline))]
public class BenDayBloomEffect : VolumeComponent, IPostProcessComponent
{

    [Header("Bloom Settings")]
    public FloatParameter threshold = new FloatParameter(0.9f, true);
    public FloatParameter intensity = new FloatParameter(1, true);
    public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f, true);
    public IntParameter clamp = new IntParameter(65472, true);
    public ClampedIntParameter maxIterations = new ClampedIntParameter(6, 0, 10, true);
    public NoInterpColorParameter tint = new NoInterpColorParameter(Color.white);

    [Header("Benday")]
    public IntParameter dotsDensity = new IntParameter(10, true);
    public ClampedFloatParameter dotsCutoff = new ClampedFloatParameter(0.4f, 0f, 1f, true);
    public Vector2Parameter scrollDirection = new Vector2Parameter(new Vector2());
    public bool IsActive()
    {
        return true;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}
