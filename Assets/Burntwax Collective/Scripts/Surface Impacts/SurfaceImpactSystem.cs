using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    public sealed class SurfaceImpactSystem : MonoBehaviour
    {
        private static SurfaceImpactSystem instance;

        public static SurfaceImpactSystem Instance
        {
            get
            {
                if (instance == null)
                    instance = FindAnyObjectByType<SurfaceImpactSystem>();

                return instance;
            }
        }

        public List<SurfaceEffectSO> surfaceEffects;

        private readonly HashSet<string> missingEffectWarnings =
            new HashSet<string>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public bool PlayImpact(
            Collider hitCollider,
            DamageInfo damage,
            bool playAudio = true)
        {
            if (hitCollider == null)
                return false;

            SurfaceEffectSO effect = FindEffect(
                hitCollider.tag,
                damage.PresentationType);
            if (effect == null)
            {
                WarnAboutMissingEffect(
                    hitCollider.tag,
                    damage.PresentationType);
                return false;
            }

            Vector3 impactNormal = damage.Normal;
            if (impactNormal.sqrMagnitude <= Mathf.Epsilon)
                impactNormal = damage.Direction.sqrMagnitude > Mathf.Epsilon
                    ? -damage.Direction
                    : Vector3.up;

            Vector3 impactPosition = damage.Point + impactNormal * 0.01f;
            Quaternion rotation = GetImpactRotation(
                effect,
                impactNormal,
                damage.Direction);

            SpawnVFX(effect, impactPosition, rotation);
            if (playAudio)
                SpawnAudio(effect, impactPosition);

            return true;
        }

        public bool PlayImpact(
            string surfaceTag,
            DamageType damageType,
            Vector3 position,
            Quaternion rotation,
            bool playAudio = true)
        {
            SurfaceEffectSO effect = FindEffect(surfaceTag, damageType);
            if (effect == null)
            {
                WarnAboutMissingEffect(surfaceTag, damageType);
                return false;
            }

            SpawnVFX(effect, position, rotation);
            if (playAudio)
                SpawnAudio(effect, position);

            return true;
        }

        public bool PlayVFX(
            string surfaceTag,
            DamageType damageType,
            Vector3 position,
            Quaternion rotation)
        {
            SurfaceEffectSO effect = FindEffect(surfaceTag, damageType);
            if (effect == null)
            {
                WarnAboutMissingEffect(surfaceTag, damageType);
                return false;
            }

            SpawnVFX(effect, position, rotation);
            return true;
        }

        public bool PlayAudio(
            string surfaceTag,
            DamageType damageType,
            Vector3 position)
        {
            SurfaceEffectSO effect = FindEffect(surfaceTag, damageType);
            if (effect == null)
            {
                WarnAboutMissingEffect(surfaceTag, damageType);
                return false;
            }

            SpawnAudio(effect, position);
            return true;
        }

        private SurfaceEffectSO FindEffect(
            string surfaceTag,
            DamageType damageType)
        {
            if (surfaceEffects == null)
                return null;

            SurfaceEffectSO overlappingEffect = null;

            for (int i = 0; i < surfaceEffects.Count; i++)
            {
                SurfaceEffectSO effect = surfaceEffects[i];
                if (effect == null || effect.surfaceTag != surfaceTag)
                    continue;

                if (effect.damageTypes == damageType)
                    return effect;

                if (overlappingEffect == null
                    && damageType != DamageType.None
                    && (effect.damageTypes & damageType) != 0)
                {
                    overlappingEffect = effect;
                }
            }

            return overlappingEffect;
        }

        private static Quaternion GetImpactRotation(
            SurfaceEffectSO effect,
            Vector3 surfaceNormal,
            Vector3 hitDirection)
        {
            Quaternion surfaceRotation = Quaternion.LookRotation(surfaceNormal);
            if (!effect.alignVfxToHitDirection)
                return surfaceRotation;

            Vector3 directionOnSurface = Vector3.ProjectOnPlane(
                hitDirection,
                surfaceNormal);
            if (directionOnSurface.sqrMagnitude <= 0.0001f)
                return surfaceRotation;

            Quaternion directionalRotation = Quaternion.LookRotation(
                surfaceNormal,
                directionOnSurface.normalized);

            return directionalRotation
                * Quaternion.AngleAxis(
                    effect.hitDirectionRotationOffset,
                    Vector3.forward);
        }

        private static void SpawnVFX(
            SurfaceEffectSO effect,
            Vector3 position,
            Quaternion rotation)
        {
            if (effect.vfxPrefab != null)
                Instantiate(effect.vfxPrefab, position, rotation);
        }

        private static void SpawnAudio(
            SurfaceEffectSO effect,
            Vector3 position)
        {
            if (effect.impactSounds == null || effect.impactSounds.Count == 0)
                return;

            AudioClip clip = effect.impactSounds[
                Random.Range(0, effect.impactSounds.Count)];
            if (clip == null)
                return;

            GameObject audioObject = new GameObject("SurfaceImpactAudio");
            audioObject.transform.position = position;

            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = effect.volume;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.spatialBlend = 1f;
            audioSource.Play();

            Destroy(audioObject, clip.length / audioSource.pitch);
        }

        private void WarnAboutMissingEffect(
            string surfaceTag,
            DamageType damageType)
        {
            string warningKey = $"{surfaceTag}:{damageType}";
            if (!missingEffectWarnings.Add(warningKey))
                return;

            Debug.LogWarning(
                $"No surface effect found for tag '{surfaceTag}' "
                    + $"and damage type '{damageType}'.",
                this);
        }
    }
}
