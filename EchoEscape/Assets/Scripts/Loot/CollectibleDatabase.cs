using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Collection Database. It centrally saves what is available in the game collectible Data such as name, description, and icon.
/// Gameplay logic: other systems require random rewards or UI icon, you can get it from here CollectibleItem, instead of each script writing its own data.
/// Collaborates with: CollectibleItem can be converted to LootDefinition, Enter GameManager of loot process.
    /// </summary>
    public static class CollectibleDatabase
    {
        private const string CollectibleResourceRoot = "Collectibles/";

        private static readonly CollectibleDescriptor[] descriptors =
        {
            new CollectibleDescriptor(
                "echo_crystal",
                "Echo Crystal",
                "A crystal that remembers a faint echo of the past.",
                "EchoCrystal",
                45),
            new CollectibleDescriptor(
                "ancient_acorn_relic",
                "Ancient Acorn Relic",
                "A golden forest relic carved with old runes.",
                "AncientAcornRelic",
                25),
            new CollectibleDescriptor(
                "moonleaf_brooch",
                "Moonleaf Brooch",
                "A silver leaf charm carrying soft moonlight.",
                "MoonleafBrooch",
                15),
            new CollectibleDescriptor(
                "spirit_lantern",
                "Spirit Lantern",
                "A tiny lantern holding a green forest spirit.",
                "SpiritLantern",
                10),
            new CollectibleDescriptor(
                "runic_mushroom_idol",
                "Runic Mushroom Idol",
                "A strange mushroom idol glowing with ancient magic.",
                "RunicMushroomIdol",
                5)
        };

        private static List<CollectibleItem> cachedCollectibles;
        /// <summary>
/// Returns all available collections. GameManager lottery or UI This list is read when the icon is displayed.
        /// </summary>
/// <returns>Returns a read-only collection list. The caller can only read and cannot directly modify the database. </returns>
        public static IReadOnlyList<CollectibleItem> GetAllCollectibles()
        {
            EnsureLoaded();
            return cachedCollectibles;
        }
        /// <summary>
/// Returns a random collectible from the database to be used as a treasure chest reward.
        /// </summary>
/// <returns>Returns a collection data; may be returned if the database is empty null。</returns>
        public static CollectibleItem GetRandomCollectible()
        {
            EnsureLoaded();
            if (cachedCollectibles.Count == 0)
            {
// Return when there are no collections default, the caller will take its own fallback。
                return default;
            }

            int totalWeight = 0;
            for (int i = 0; i < cachedCollectibles.Count; i++)
            {
                totalWeight += Mathf.Max(1, cachedCollectibles[i].weight);
            }

            int roll = Random.Range(0, totalWeight);
            for (int i = 0; i < cachedCollectibles.Count; i++)
            {
                roll -= Mathf.Max(1, cachedCollectibles[i].weight);
                if (roll < 0)
                {
                    return cachedCollectibles[i];
                }
            }

            return cachedCollectibles[0];
        }
        /// <summary>
/// Collection data is loaded and cached on first use. Repeated calls afterward will not reload the resource.
        /// </summary>
        private static void EnsureLoaded()
        {
            if (cachedCollectibles != null)
            {
// Once loaded, reuse the cache directly to avoid re-reading every time you open the box. Resources。
                return;
            }

            cachedCollectibles = new List<CollectibleItem>(descriptors.Length);
            for (int i = 0; i < descriptors.Length; i++)
            {
                CollectibleDescriptor descriptor = descriptors[i];
// Descriptor Save the text and resource name, and then load the image into Sprite。
                Sprite icon = LoadCollectibleIcon(descriptor.resourceName);
                cachedCollectibles.Add(new CollectibleItem(
                    descriptor.id,
                    descriptor.displayName,
                    descriptor.description,
                    icon,
                    descriptor.weight));
            }
        }

        private readonly struct CollectibleDescriptor
        {
            public readonly string id;
            public readonly string displayName;
            public readonly string description;
            public readonly string resourceName;
            public readonly int weight;

            public CollectibleDescriptor(string id, string displayName, string description, string resourceName, int weight)
            {
                this.id = id;
                this.displayName = displayName;
                this.description = description;
                this.resourceName = resourceName;
                this.weight = weight;
            }
        }
        /// <summary>
/// from Resources Or load the required resources from the incoming data and convert it into an object that can be used directly by the script.
        /// </summary>
/// <param name="resourceName">resourceName Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <returns>Returns the loaded or generated Sprite; May be returned when the resource does not exist null。</returns>
        private static Sprite LoadCollectibleIcon(string resourceName)
        {
            string resourcePath = CollectibleResourceRoot + resourceName;
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
// If the resource is already Sprite, used directly.
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
// Allow return when icon is missing null，LootFeedbackUI Will use words to tell the truth.
                return null;
            }

// Some resources are imported into Texture2D, no Sprite; Generated when running here Sprite for UI use.
            Sprite generatedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(texture.width, texture.height));
            generatedSprite.name = resourceName;
            return generatedSprite;
        }
    }
}
