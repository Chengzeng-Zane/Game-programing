using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Defines one collectible reward shown by the loot popup.
    /// </summary>
    [System.Serializable]
    public struct CollectibleItem
    {
        /// <summary>
        /// Stable collectible identifier.
        /// </summary>
        public string id;

        /// <summary>
        /// Player-facing collectible name.
        /// </summary>
        public string displayName;

        /// <summary>
        /// Short flavor text shown in the loot UI.
        /// </summary>
        public string description;

        /// <summary>
        /// Optional icon loaded from Resources.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Creates one collectible definition.
        /// </summary>
        /// <param name="id">Stable collectible identifier.</param>
        /// <param name="displayName">Player-facing collectible name.</param>
        /// <param name="description">Short flavor text.</param>
        /// <param name="icon">Optional icon sprite.</param>
        public CollectibleItem(string id, string displayName, string description, Sprite icon)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.icon = icon;
        }

        /// <summary>
        /// Converts this collectible into the existing loot runtime type.
        /// </summary>
        /// <returns>LootDefinition used by chest and manager systems.</returns>
        public LootDefinition ToLootDefinition()
        {
            return new LootDefinition(id, displayName, description, icon, 1);
        }
    }
}
