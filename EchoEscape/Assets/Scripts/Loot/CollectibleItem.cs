using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Data structure of a single collection.
/// It is not responsible for collisions, unboxing or UI, only save collections id, display name, description, icon and extraction weight.
/// The treasure chest system starts from CollectibleDatabase draw one CollectibleItem Afterwards, it will be converted into LootDefinition，
/// Give it to again EchoEscapeGameManager Enter pending loot / secured loot process.
    /// </summary>
    [System.Serializable]
    public struct CollectibleItem
    {
        public string id;
        public string displayName;
        public string description;
        public Sprite icon;
        public int weight;

        /// <summary>
/// Create a collection data object.
        /// </summary>
/// <param name="id">The only collection id, used to distinguish different items. </param>
/// <param name="displayName">The name of the collection displayed to the player. </param>
/// <param name="description">The collection description shown to the player. </param>
/// <param name="icon">UI Collection icon shown in. </param>
/// <param name="weight">The weight used when randomly drawing treasure chests. The larger the value, the easier it is to draw. </param>
        public CollectibleItem(string id, string displayName, string description, Sprite icon, int weight)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.icon = icon;
            this.weight = weight;
        }

        /// <summary>
/// Convert collection to GameManager used LootDefinition。
/// In this way, treasure chest rewards and death are lost. pending loot, export settlement secured loot They can all follow the same logic.
        /// </summary>
/// <returns>can enter loot procedural LootDefinition。</returns>
        public LootDefinition ToLootDefinition()
        {
            return new LootDefinition(id, displayName, description, icon, weight);
        }
    }
}
