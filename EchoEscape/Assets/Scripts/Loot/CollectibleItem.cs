using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：单个可收集物的数据类，保存 id、显示名、描述和图标。
    /// 玩法逻辑：它本身不处理碰撞或 UI，只是把“这是什么物品”表达清楚；需要进入 loot 系统时，可以转换成 LootDefinition。
    /// 协作关系：CollectibleDatabase 创建它；EchoEscapeGameManager 和 LootFeedbackUI 使用转换后的数据。
    /// </summary>
    [System.Serializable]
    public struct CollectibleItem
    {
        public string id;
        public string displayName;
        public string description;
        public Sprite icon;
        /// <summary>
        /// 构造函数：创建这个数据对象，并把传入的字段保存起来，方便其他脚本用统一格式读取。
        /// </summary>
        /// <param name="id">id 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="displayName">displayName 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="description">description 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="icon">icon 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public CollectibleItem(string id, string displayName, string description, Sprite icon)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.icon = icon;
        }
        /// <summary>
        /// 把收藏物数据转换成 GameManager 使用的 LootDefinition。这样宝箱奖励、pendingLoot 和 securedLoot 可以统一处理收藏物。
        /// </summary>
        /// <returns>返回一个战利品定义，后续会进入 pending 或 secured loot 流程。</returns>
        public LootDefinition ToLootDefinition()
        {
            // weight 对已抽中的收藏物没有实际随机意义，这里给 1 只是保持 LootDefinition 字段完整。
            return new LootDefinition(id, displayName, description, icon, 1);
        }
    }
}
