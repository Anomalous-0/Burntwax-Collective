using UnityEngine;

namespace Burntwax
{
    /// <summary>
    /// Stable, designer-facing identity for a player resource. Gameplay owns
    /// the value; HUD widgets use this asset to request the correct source.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Player Resource",
        menuName = "Player/UI/Resource Definition",
        order = 0)]
    public sealed class PlayerResourceDefinition : ScriptableObject
    {
        [Tooltip("Stable identifier used by systems that cannot hold an asset reference.")]
        [SerializeField] private string stableId = "resource";

        [Tooltip("Designer-facing name for this resource.")]
        [SerializeField] private string displayName = "Resource";

        public string StableId => stableId;
        public string DisplayName => displayName;

        private void OnValidate()
        {
            stableId = stableId?.Trim();
            displayName = displayName?.Trim();
        }
    }
}
