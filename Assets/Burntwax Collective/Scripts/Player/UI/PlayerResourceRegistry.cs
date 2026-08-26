using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Discovers player resource sources and gives the HUD one lookup point.
    /// It never changes resource values or owns gameplay rules.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerResourceRegistry : MonoBehaviour
    {
        [Tooltip("Hierarchy searched for resource sources. Defaults to this player.")]
        [SerializeField] private Transform resourceRoot;

        public event Action<IPlayerResourceSource> ResourceRegistered;
        public event Action<IPlayerResourceSource> ResourceChanged;
        public event Action RegistryRefreshed;

        private readonly Dictionary<PlayerResourceDefinition,
            IPlayerResourceSource> sources = new();
        private readonly List<IPlayerResourceSource> subscribedSources = new();
        private readonly List<IPlayerResourceSource> externalSources = new();

        private void OnEnable()
        {
            RefreshSources();
        }

        private void OnDisable()
        {
            ClearSources();
        }

        /// <summary>
        /// Rebuilds the registry. Call this if a resource-provider component is
        /// added to or removed from the player hierarchy at runtime.
        /// </summary>
        public void RefreshSources()
        {
            ClearSources();

            Transform root = resourceRoot != null
                ? resourceRoot
                : transform;
            MonoBehaviour[] behaviours =
                root.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IPlayerResourceSource source
                    || source.ResourceDefinition == null)
                {
                    continue;
                }

                AddSource(source, behaviours[i]);
            }

            for (int i = externalSources.Count - 1; i >= 0; i--)
            {
                IPlayerResourceSource source = externalSources[i];
                if (source is UnityEngine.Object unityObject
                    && unityObject == null)
                {
                    externalSources.RemoveAt(i);
                    continue;
                }

                AddSource(source, source as UnityEngine.Object);
            }

            RegistryRefreshed?.Invoke();
        }

        /// <summary>
        /// Registers a source outside the player hierarchy, such as a
        /// first-person equipment rig.
        /// </summary>
        public void RegisterExternalSource(IPlayerResourceSource source)
        {
            if (source == null || externalSources.Contains(source))
                return;

            externalSources.Add(source);
            AddSource(source, source as UnityEngine.Object);
            RegistryRefreshed?.Invoke();
        }

        public void UnregisterExternalSource(IPlayerResourceSource source)
        {
            if (source == null || !externalSources.Remove(source))
                return;

            if (isActiveAndEnabled)
                RefreshSources();
            else
                ClearSources();
        }

        public bool TryGetSource(
            PlayerResourceDefinition definition,
            out IPlayerResourceSource source)
        {
            if (definition == null)
            {
                source = null;
                return false;
            }

            return sources.TryGetValue(definition, out source);
        }

        private void OnResourceChanged(IPlayerResourceSource source)
        {
            ResourceChanged?.Invoke(source);
        }

        private void AddSource(
            IPlayerResourceSource source,
            UnityEngine.Object context)
        {
            if (source == null || source.ResourceDefinition == null)
                return;

            PlayerResourceDefinition definition =
                source.ResourceDefinition;
            if (sources.TryGetValue(
                definition,
                out IPlayerResourceSource existingSource))
            {
                if (!ReferenceEquals(existingSource, source))
                {
                    Debug.LogWarning(
                        $"Duplicate player resource source for "
                        + $"'{definition.DisplayName}' on {name}.",
                        context);
                }

                return;
            }

            sources.Add(definition, source);
            subscribedSources.Add(source);
            source.ResourceChanged += OnResourceChanged;
            ResourceRegistered?.Invoke(source);
        }

        private void ClearSources()
        {
            for (int i = 0; i < subscribedSources.Count; i++)
                subscribedSources[i].ResourceChanged -= OnResourceChanged;

            subscribedSources.Clear();
            sources.Clear();
        }

        private void OnValidate()
        {
            if (resourceRoot == null)
                resourceRoot = transform;
        }
    }
}
