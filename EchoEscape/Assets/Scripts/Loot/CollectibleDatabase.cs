using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Loads collectible rewards from Resources and provides random selection.
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
                "EchoCrystal"),
            new CollectibleDescriptor(
                "ancient_acorn_relic",
                "Ancient Acorn Relic",
                "A golden forest relic carved with old runes.",
                "AncientAcornRelic"),
            new CollectibleDescriptor(
                "moonleaf_brooch",
                "Moonleaf Brooch",
                "A silver leaf charm carrying soft moonlight.",
                "MoonleafBrooch"),
            new CollectibleDescriptor(
                "spirit_lantern",
                "Spirit Lantern",
                "A tiny lantern holding a green forest spirit.",
                "SpiritLantern"),
            new CollectibleDescriptor(
                "runic_mushroom_idol",
                "Runic Mushroom Idol",
                "A strange mushroom idol glowing with ancient magic.",
                "RunicMushroomIdol")
        };

        private static List<CollectibleItem> cachedCollectibles;

        /// <summary>
        /// Returns all collectible rewards.
        /// </summary>
        /// <returns>Loaded collectible list.</returns>
        public static IReadOnlyList<CollectibleItem> GetAllCollectibles()
        {
            EnsureLoaded();
            return cachedCollectibles;
        }

        /// <summary>
        /// Returns one random collectible reward.
        /// </summary>
        /// <returns>Random collectible item.</returns>
        public static CollectibleItem GetRandomCollectible()
        {
            EnsureLoaded();
            if (cachedCollectibles.Count == 0)
            {
                return default;
            }

            return cachedCollectibles[Random.Range(0, cachedCollectibles.Count)];
        }

        private static void EnsureLoaded()
        {
            if (cachedCollectibles != null)
            {
                return;
            }

            cachedCollectibles = new List<CollectibleItem>(descriptors.Length);
            for (int i = 0; i < descriptors.Length; i++)
            {
                CollectibleDescriptor descriptor = descriptors[i];
                Sprite icon = LoadCollectibleIcon(descriptor.resourceName);
                cachedCollectibles.Add(new CollectibleItem(
                    descriptor.id,
                    descriptor.displayName,
                    descriptor.description,
                    icon));
            }
        }

        private readonly struct CollectibleDescriptor
        {
            public readonly string id;
            public readonly string displayName;
            public readonly string description;
            public readonly string resourceName;

            public CollectibleDescriptor(string id, string displayName, string description, string resourceName)
            {
                this.id = id;
                this.displayName = displayName;
                this.description = description;
                this.resourceName = resourceName;
            }
        }

        private static Sprite LoadCollectibleIcon(string resourceName)
        {
            string resourcePath = CollectibleResourceRoot + resourceName;
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

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
