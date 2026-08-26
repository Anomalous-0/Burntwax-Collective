using System;
using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Binds player resource identities to presentation widgets. New resource
    /// types require a source and a widget binding, not another HUD code path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombatResourceHUD : MonoBehaviour
    {
        [Serializable]
        private sealed class ResourceWidgetBinding
        {
            public PlayerResourceDefinition resource;
            public PlayerResourceWidget widget;
        }

        [SerializeField] private PlayerResourceRegistry resourceRegistry;
        [SerializeField] private List<ResourceWidgetBinding> bindings = new();

        private void OnEnable()
        {
            ResolveRegistry();
            if (resourceRegistry == null)
                return;

            resourceRegistry.RegistryRefreshed += BindConfiguredWidgets;
            resourceRegistry.RefreshSources();
        }

        private void OnDisable()
        {
            if (resourceRegistry != null)
            {
                resourceRegistry.RegistryRefreshed -=
                    BindConfiguredWidgets;
            }

            UnbindConfiguredWidgets();
        }

        private void BindConfiguredWidgets()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                ResourceWidgetBinding binding = bindings[i];
                if (binding == null || binding.widget == null)
                    continue;

                if (binding.resource != null
                    && resourceRegistry.TryGetSource(
                        binding.resource,
                        out IPlayerResourceSource source))
                {
                    binding.widget.Bind(source);
                }
                else
                {
                    binding.widget.Unbind();
                }
            }
        }

        private void UnbindConfiguredWidgets()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                ResourceWidgetBinding binding = bindings[i];
                if (binding?.widget != null)
                    binding.widget.Unbind();
            }
        }

        private void ResolveRegistry()
        {
            if (resourceRegistry == null)
            {
                resourceRegistry =
                    GetComponentInParent<PlayerResourceRegistry>();
            }
        }

        private void OnValidate()
        {
            ResolveRegistry();
        }
    }
}
