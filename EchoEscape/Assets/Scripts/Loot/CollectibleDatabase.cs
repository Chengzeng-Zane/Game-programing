using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：收集物数据库。它集中保存游戏里可用的 collectible 数据，例如名字、描述和图标。
    /// 玩法逻辑：其他系统需要随机奖励或 UI 图标时，可以从这里拿 CollectibleItem，而不是每个脚本自己写一份数据。
    /// 协作关系：CollectibleItem 可以转换成 LootDefinition，进入 GameManager 的 loot 流程。
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
        /// 返回全部可用收藏物。GameManager 抽奖或 UI 显示图标时会读取这个列表。
        /// </summary>
        /// <returns>返回只读收集物列表，调用方只能读取不能直接修改数据库。</returns>
        public static IReadOnlyList<CollectibleItem> GetAllCollectibles()
        {
            EnsureLoaded();
            return cachedCollectibles;
        }
        /// <summary>
        /// 从数据库中随机返回一个收藏物，用作宝箱奖励。
        /// </summary>
        /// <returns>返回一个收集物数据；如果数据库为空可能返回 null。</returns>
        public static CollectibleItem GetRandomCollectible()
        {
            EnsureLoaded();
            if (cachedCollectibles.Count == 0)
            {
                // 没有任何收藏物时返回 default，调用方会走自己的 fallback。
                return default;
            }

            return cachedCollectibles[Random.Range(0, cachedCollectibles.Count)];
        }
        /// <summary>
        /// 首次使用时加载收藏物数据并缓存。之后重复调用不会重新加载资源。
        /// </summary>
        private static void EnsureLoaded()
        {
            if (cachedCollectibles != null)
            {
                // 已加载过就直接复用缓存，避免每次开箱都重新读 Resources。
                return;
            }

            cachedCollectibles = new List<CollectibleItem>(descriptors.Length);
            for (int i = 0; i < descriptors.Length; i++)
            {
                CollectibleDescriptor descriptor = descriptors[i];
                // Descriptor 保存文字和资源名，运行时再把图片加载成 Sprite。
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
        /// <summary>
        /// 从 Resources 或传入数据中加载需要的资源，并转换成脚本可直接使用的对象。
        /// </summary>
        /// <param name="resourceName">resourceName 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回加载或生成的 Sprite；资源不存在时可能返回 null。</returns>
        private static Sprite LoadCollectibleIcon(string resourceName)
        {
            string resourcePath = CollectibleResourceRoot + resourceName;
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                // 如果资源已经是 Sprite，直接使用。
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                // 图标缺失时允许返回 null，LootFeedbackUI 会用文字兜底。
                return null;
            }

            // 有些资源导入成 Texture2D，不是 Sprite；这里运行时生成 Sprite 供 UI 使用。
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
