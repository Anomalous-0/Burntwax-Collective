using System;
using UnityEngine;

namespace Burntwax
{

    [DisallowMultipleComponent]

    public class PlayerAmmo : MonoBehaviour, IPlayerAmmoResourceSource
    {
        [SerializeField] private PlayerResourceDefinition resourceDefinition;
        [SerializeField] private GunStateMachine gunSelector;

        public PlayerResourceDefinition ResourceDefinition =>
            resourceDefinition;
        public float CurrentValue => ClipAmmo;
        public float MaximumValue => ClipCapacity;
        public bool IsResourceAvailable => ActiveAmmoConfig != null;
        public int ClipAmmo => ActiveAmmoConfig != null
            ? ActiveAmmoConfig.currentClip
            : 0;
        public int ClipCapacity => ActiveAmmoConfig != null
            ? ActiveAmmoConfig.clipSize
            : 0;
        public int ReserveAmmo => ActiveAmmoConfig != null
            ? ActiveAmmoConfig.currentAmmo
            : 0;
        public int ReserveCapacity => ActiveAmmoConfig != null
            ? ActiveAmmoConfig.maxAmmo
            : 0;

        public event Action<IPlayerResourceSource> ResourceChanged;

        private GunScriptableObject observedGun;
        private int observedClipAmmo;
        private int observedReserveAmmo;

        private AmmoConfig ActiveAmmoConfig =>
            gunSelector != null
                && gunSelector.ActiveGunScriptable != null
                    ? gunSelector.ActiveGunScriptable.AmmoConfig
                    : null;

        private void Awake()
        {
            ResolveGunSelector();
            CaptureSnapshot();
        }

        private void Update()
        {
            if (gunSelector == null)
                ResolveGunSelector();

            GunScriptableObject activeGun = gunSelector != null
                ? gunSelector.ActiveGunScriptable
                : null;
            int clipAmmo = ClipAmmo;
            int reserveAmmo = ReserveAmmo;

            if (activeGun == observedGun
                && clipAmmo == observedClipAmmo
                && reserveAmmo == observedReserveAmmo)
            {
                return;
            }

            CaptureSnapshot();
            ResourceChanged?.Invoke(this);
        }

        private void ResolveGunSelector()
        {
            if (gunSelector == null)
            {
                gunSelector =
                    GetComponentInChildren<GunStateMachine>(true);
            }
        }

        private void CaptureSnapshot()
        {
            observedGun = gunSelector != null
                ? gunSelector.ActiveGunScriptable
                : null;
            observedClipAmmo = ClipAmmo;
            observedReserveAmmo = ReserveAmmo;
        }
    }

}
