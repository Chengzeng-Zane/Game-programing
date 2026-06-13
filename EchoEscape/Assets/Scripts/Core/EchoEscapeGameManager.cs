using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：关卡总管理器，是 Echo Escape 每一关运行时的中心脚本。它负责连接玩家、Echo 录制、教程、音效、视觉换皮、宝箱、loot、死亡和通关。
    /// 玩法逻辑：关卡开始时查找 Player 和 ActionRecorder，准备音频/视觉服务，生成随机宝箱并刷新 HUD；玩家获得 loot 时先放入 pendingLoot，死亡会丢失；到达出口时把 pendingLoot 转成 securedLoot；死亡时播放死亡动画和 UI 后重载当前关卡。
    /// 协作关系：HazardZone、GravityFlipVoidKillZone、EnemyAttack、Chest、CollectibleItem、GoalZone、LootFeedbackUI、PrototypeAudio、PrototypeVisualSkinner 都会调用或依赖它。
    /// </summary>
    public class EchoEscapeGameManager : MonoBehaviour
    {
        public static EchoEscapeGameManager Instance { get; private set; }

        [Header("Scene References")]
        public PlayerController2D player;
        public ActionRecorder recorder;
        public Transform playerSpawn;

        [Header("Loot")]
        public int chestsPerRun = 2;
        public LootDefinition[] lootTable;

        [Header("Tutorial / HUD")]
        public bool useTutorialDirector = true;
        public bool usePrototypeVisualSkinner = true;
        public string startingStatusMessage = "Tutorial started. Learn record, replay, loot risk, and extraction.";
        public bool useLootFeedbackUi = true;

        [Header("Death")]
        [SerializeField]
        [FormerlySerializedAs("deathHurtDelay")]
        private float deathAnimationFallbackDelay = 0.8f;

        [SerializeField]
        private float deathReloadDelay = 1f;

        [SerializeField]
        private bool debugDeathFlow;
        public string StatusMessage { get; private set; } = "Reach the exit. Use your echo to hold the plate.";
        public int DeathCount { get; private set; }
        public bool HasWon { get; private set; }
        public bool IsPlayerDeadOrDying => deathInProgress;
        public TutorialDirector Tutorial { get; private set; }
        public PrototypeAudio AudioService { get; private set; }
        public PrototypeVisualSkinner VisualSkinner { get; private set; }
        public LootFeedbackUI LootFeedback { get; private set; }
        public IReadOnlyList<LootDefinition> PendingLoot => pendingLoot;
        public IReadOnlyList<LootDefinition> SecuredLoot => securedLoot;
        public int PendingLootCount => pendingLoot.Count;
        public int SecuredLootCount => securedLoot.Count;

        private readonly List<LootDefinition> pendingLoot = new List<LootDefinition>();
        private static readonly List<LootDefinition> securedLoot = new List<LootDefinition>();
        private static readonly HashSet<string> scenesWithClaimedChest = new HashSet<string>();
        private const float ChestVisualScale = 0.55f;
        private const float ChestVisualYOffset = -0.4f;
        private const int ChestSortingOrder = 5;
        private static Sprite chestSprite;
        private bool deathInProgress;
        /// <summary>
        /// 建立当前关卡的 GameManager 单例，并准备 AudioListener、音频服务、视觉换皮服务和默认 lootTable。
        /// </summary>
        private void Awake()
        {
            // 每个关卡只需要一个当前 GameManager，其他脚本都通过 Instance 找到统一入口。
            Instance = this;
            EnsureAudioListener();
            EnsurePresentationServices();

            if (lootTable == null || lootTable.Length == 0)
            {
                // 如果 Inspector 没配置 lootTable，就给一个最小可玩的默认表，避免开箱为空。
                lootTable = new[]
                {
                    new LootDefinition("Old Coin", "Common", 60),
                    new LootDefinition("Blue Gem", "Rare", 30),
                    new LootDefinition("Golden Relic", "Epic", 10)
                };
            }
        }
        /// <summary>
        /// 关卡开始时查找玩家和录制器，启动教程系统，随机生成宝箱，应用视觉换皮，并刷新初始状态和 loot UI。
        /// </summary>
        private void Start()
        {
            if (player == null)
            {
                // 场景没手动拖引用时自动找 Player，降低场景配置出错概率。
                player = FindObjectOfType<PlayerController2D>();
            }

            if (recorder == null && player != null)
            {
                // Recorder 通常挂在 Player 上，GameManager 需要它来清理死亡时的 Echo。
                recorder = player.GetComponent<ActionRecorder>();
            }

            if (useTutorialDirector)
            {
                // 老版目标追踪需要 TutorialDirector；没有就运行时补上。
                Tutorial = GetComponent<TutorialDirector>();
                if (Tutorial == null)
                {
                    Tutorial = gameObject.AddComponent<TutorialDirector>();
                }
            }
            else
            {
                // 关闭教程时把已有 TutorialDirector 禁用，避免它继续改 HUD 状态。
                TutorialDirector existingTutorial = GetComponent<TutorialDirector>();
                if (existingTutorial != null)
                {
                    existingTutorial.enabled = false;
                }

                Tutorial = null;
            }

            SpawnRandomChests();
            if (usePrototypeVisualSkinner)
            {
                VisualSkinner?.SkinAll();
            }

            UpdateStatus(startingStatusMessage);
            RefreshLootFeedback();
        }
        /// <summary>
        /// 更新当前关卡状态文字。PrototypeHud、教程或 UI 可以读取这个状态显示给玩家。
        /// </summary>
        /// <param name="message">要显示到 HUD 或写入日志的文字。</param>
        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }
        /// <summary>
        /// 玩家获得战利品时调用。loot 会进入 pendingLoot，表示“拿到了但还没安全带走”，死亡会丢失。
        /// </summary>
        /// <param name="loot">单个战利品数据，包含物品名、稀有度和权重。</param>
        public void AddPendingLoot(LootDefinition loot)
        {
            // pendingLoot 表示“本关拿到了但还没带出去”，死亡时会被清空。
            pendingLoot.Add(loot);
            UpdateStatus($"Found: {loot.itemName}. Escape safely or lose it on death.");
            RefreshLootFeedback();
            LootFeedback?.ShowLootFound(loot);
            Debug.Log($"Pending loot updated: {loot.itemName}.");
        }
        /// <summary>
        /// 根据 lootTable 的权重随机抽一个奖励。权重越高越常见，权重低的物品更稀有。
        /// </summary>
        /// <returns>返回一个战利品定义，后续会进入 pending 或 secured loot 流程。</returns>
        public LootDefinition RollLoot()
        {
            IReadOnlyList<CollectibleItem> collectibles = CollectibleDatabase.GetAllCollectibles();
            if (collectibles.Count > 0)
            {
                // 当前版本优先使用 CollectibleDatabase，方便显示正式收藏品图标和名字。
                return CollectibleDatabase.GetRandomCollectible().ToLootDefinition();
            }

            int totalWeight = 0;
            foreach (LootDefinition loot in lootTable)
            {
                // Mathf.Max 防止配置了 0 或负数权重导致总权重异常。
                totalWeight += Mathf.Max(1, loot.weight);
            }

            int roll = Random.Range(0, totalWeight);
            foreach (LootDefinition loot in lootTable)
            {
                // 逐项扣权重，roll 落在哪个区间就抽中哪个 loot。
                roll -= Mathf.Max(1, loot.weight);
                if (roll < 0)
                {
                    return loot;
                }
            }

            return lootTable[0];
        }
        /// <summary>
        /// 检查当前场景的宝箱奖励是否已经被领取。用于防止同一局里重复开宝箱刷奖励。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        public bool HasClaimedChestInCurrentScene()
        {
            return scenesWithClaimedChest.Contains(GetCurrentSceneChestKey());
        }
        /// <summary>
        /// 标记当前场景宝箱已经领取。宝箱成功结算后调用。
        /// </summary>
        public void MarkChestClaimedInCurrentScene()
        {
            scenesWithClaimedChest.Add(GetCurrentSceneChestKey());
        }
        /// <summary>
        /// 开始新游戏时清空所有场景的宝箱领取记录，让新一局可以重新拿宝箱。
        /// </summary>
        public static void ResetChestClaimsForNewRun()
        {
            scenesWithClaimedChest.Clear();
        }
        /// <summary>
        /// 到达出口时调用。它把 pendingLoot 移到 securedLoot，表示玩家成功把风险物品带出关卡。
        /// </summary>
        /// <returns>返回整数结果，通常表示数量、索引或本次结算数量。</returns>
        public int SecurePendingLoot()
        {
            if (pendingLoot.Count == 0)
            {
                // 没有待结算物品也刷新 UI，保证 HUD 不显示旧状态。
                RefreshLootFeedback();
                return 0;
            }

            // 先复制再清空 pending，避免 UI 展示时引用到已被清掉的列表。
            List<LootDefinition> securedNow = new List<LootDefinition>(pendingLoot);
            securedLoot.AddRange(securedNow);
            pendingLoot.Clear();

            UpdateStatus($"Loot secured: {FormatLoot(securedNow)}.");
            RefreshLootFeedback();
            LootFeedback?.ShowLootSecured(securedNow);
            Debug.Log($"Loot secured: {FormatLoot(securedNow)}.");
            return securedNow.Count;
        }
        /// <summary>
        /// 统一玩家死亡入口。危险区、敌人和反重力死亡都应该调用这里，保证死亡动画、UI、loot 丢失和重载逻辑一致。
        /// </summary>
        /// <param name="reason">死亡原因或事件原因，用于死亡 UI、状态提示和调试日志。</param>
        public void KillPlayer(string reason)
        {
            if (debugDeathFlow)
            {
                Debug.Log("[DeathFlow] KillPlayer called");
                Debug.Log($"[DeathFlow] reason = {reason}");
                Debug.Log($"[DeathFlow] will respawn/reload = {!HasWon && !deathInProgress}");
            }

            if (HasWon || deathInProgress)
            {
                // 防止已经通关或正在死亡时重复进入死亡流程，避免多次重载或 UI 叠加。
                return;
            }

            deathInProgress = true;
            DeathCount++;
            AudioService?.PlayHurt();
            // 死亡会丢掉本关未带出的 pending loot，但不会影响已经 secured 的物品。
            List<LootDefinition> lostLoot = new List<LootDefinition>(pendingLoot);
            pendingLoot.Clear();

            if (recorder != null)
            {
                // 死亡时销毁 Echo，避免重载前 Echo 继续压机关或触发奇怪状态。
                recorder.DestroyActiveEcho();
            }

            // 冻结玩家后再播放死亡流程，避免死亡动画期间角色还能被输入或物理推动。
            DisablePlayerForDeath();
            StartCoroutine(HandleDeathSequence(reason, lostLoot));
        }
        /// <summary>
        /// 完整死亡协程。它先播放受伤音效和死亡动画，再显示丢失 loot，最后等待一段时间并重新加载当前场景。
        /// </summary>
        /// <param name="reason">死亡原因或事件原因，用于死亡 UI、状态提示和调试日志。</param>
        /// <param name="lostLoot">玩家死亡时丢失的 pending loot 列表。</param>
        /// <returns>返回 Unity 协程，调用方会用 StartCoroutine 让这个流程分帧执行。</returns>
        private IEnumerator HandleDeathSequence(string reason, List<LootDefinition> lostLoot)
        {
            // 先播放角色死亡动画，再显示死亡 UI；如果素材缺失就用 fallback 等待时间。
            float deathAnimationDuration = PlayPlayerDeathAnimation(reason);
            float deathAnimationWait = Mathf.Clamp(Mathf.Max(deathAnimationFallbackDelay, deathAnimationDuration), 0.6f, 1f);
            yield return new WaitForSecondsRealtime(deathAnimationWait);

            // UI 文本里把死亡原因和本次丢失 loot 一起写清楚，玩家能理解风险回报机制。
            string lossText = lostLoot.Count > 0 ? $" Loot Lost: {FormatLoot(lostLoot)}" : string.Empty;
            UpdateStatus($"You died: {reason}.{lossText}");
            RefreshLootFeedback();
            LootFeedback?.ShowDeath(lostLoot);

            if (lostLoot.Count > 0)
            {
                Debug.Log("Player died. Pending loot lost.");
            }

            yield return ReloadCurrentSceneAfterDeath();
        }
        /// <summary>
        /// 当前关卡获胜入口。它设置 HasWon，结算 pending loot，播放成功音效，并更新状态文字。
        /// </summary>
        public void Win()
        {
            if (HasWon || deathInProgress)
            {
                // 胜利和死亡互斥，避免玩家进门瞬间又触发危险区导致状态冲突。
                return;
            }

            HasWon = true;
            // 通关时把 pendingLoot 转为 securedLoot，这一步才算真正带走奖励。
            int securedThisRun = SecurePendingLoot();
            AudioService?.PlaySuccess();
            UpdateStatus(securedThisRun > 0
                ? $"Extraction complete. Secured {securedThisRun} collectible(s)."
                : "Extraction complete. No pending loot to secure.");
            RefreshLootFeedback();
            Debug.Log("Extraction successful. Loot secured.");
        }
        /// <summary>
        /// 确保关卡里有音频、视觉换皮和 loot UI 服务。缺少组件时会自动挂到 GameManager 上。
        /// </summary>
        private void EnsurePresentationServices()
        {
            AudioService = GetComponent<PrototypeAudio>();
            if (AudioService == null)
            {
                AudioService = gameObject.AddComponent<PrototypeAudio>();
            }

            VisualSkinner = GetComponent<PrototypeVisualSkinner>();
            if (usePrototypeVisualSkinner && VisualSkinner == null)
            {
                VisualSkinner = gameObject.AddComponent<PrototypeVisualSkinner>();
            }
            else if (!usePrototypeVisualSkinner && VisualSkinner != null)
            {
                VisualSkinner.enabled = false;
                VisualSkinner = null;
            }

            LootFeedback = GetComponent<LootFeedbackUI>();
            if (useLootFeedbackUi)
            {
                if (LootFeedback == null)
                {
                    LootFeedback = gameObject.AddComponent<LootFeedbackUI>();
                }

                LootFeedback.enabled = true;
            }
            else if (LootFeedback != null)
            {
                LootFeedback.enabled = false;
            }
        }
        /// <summary>
        /// 确保场景里有 AudioListener，否则音效和音乐可能播放了但听不到。
        /// </summary>
        private static void EnsureAudioListener()
        {
            if (FindObjectOfType<AudioListener>() != null)
            {
                return;
            }

            Camera camera = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (camera != null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }
        }
        /// <summary>
        /// 把当前 pendingLoot 和 securedLoot 同步到 LootFeedbackUI，刷新屏幕上的 loot 状态。
        /// </summary>
        private void RefreshLootFeedback()
        {
            if (useLootFeedbackUi)
            {
                LootFeedback?.RefreshLootState(pendingLoot, securedLoot);
            }
        }
        /// <summary>
        /// 死亡反馈结束后重新加载当前场景，实现从本关起点重来。
        /// </summary>
        /// <returns>返回 Unity 协程，调用方会用 StartCoroutine 让这个流程分帧执行。</returns>
        private IEnumerator ReloadCurrentSceneAfterDeath()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, deathReloadDelay));
            Time.timeScale = 1f;
            string sceneName = SceneManager.GetActiveScene().name;
            // 死亡后重开当前关，不再播放一次关卡故事介绍，避免节奏被打断。
            LevelIntroSequence.SkipNextIntroForScene(sceneName);
            SceneManager.LoadScene(sceneName);
        }
        /// <summary>
        /// 查找玩家视觉上的 PlayerAnimationController 并播放死亡动画，返回动画时长给死亡流程等待。
        /// </summary>
        /// <param name="reason">死亡原因或事件原因，用于死亡 UI、状态提示和调试日志。</param>
        /// <returns>返回浮点数结果，通常表示时间、距离、速度或动画时长。</returns>
        private float PlayPlayerDeathAnimation(string reason)
        {
            if (player == null)
            {
                return 0f;
            }

            PlayerAnimationController animationController = player.GetComponentInChildren<PlayerAnimationController>();
            return animationController != null ? animationController.PlayDeath(reason) : 0f;
        }
        /// <summary>
        /// 死亡期间冻结玩家输入和物理运动，避免玩家在死亡 UI 或死亡动画期间继续移动。
        /// </summary>
        private void DisablePlayerForDeath()
        {
            if (player == null)
            {
                return;
            }

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                // FreezeAll 锁住位置和旋转，保证死亡动画显示在死亡地点，不会继续滑动。
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            PlayerAttack attack = player.GetComponent<PlayerAttack>();
            if (attack != null)
            {
                // 死亡期间禁止继续攻击，避免敌人/宝箱/机关在死亡流程中被继续影响。
                attack.enabled = false;
            }

            GravityFlipController gravityFlip = player.GetComponent<GravityFlipController>();
            if (gravityFlip != null)
            {
                // 死亡后不能再让玩家按上下键翻转，否则死亡动画和位置会被打断。
                gravityFlip.enabled = false;
            }

            if (recorder != null)
            {
                // 死亡期间禁止继续录制/播放 Echo，避免重载前留下新的 Echo 状态。
                recorder.enabled = false;
            }

            // 最后禁用 PlayerController2D，这样 Update 不再读取移动、跳跃、开箱等输入。
            player.enabled = false;
        }
        /// <summary>
        /// 把一组 loot 转成 UI 可读的文字，例如多个物品用逗号连接。
        /// </summary>
        /// <param name="loot">单个战利品数据，包含物品名、稀有度和权重。</param>
        /// <returns>返回整理后的文字，用于 UI 显示、日志或状态提示。</returns>
        private static string FormatLoot(IReadOnlyList<LootDefinition> loot)
        {
            if (loot == null || loot.Count == 0)
            {
                return "none";
            }

            List<string> labels = new List<string>(loot.Count);
            for (int i = 0; i < loot.Count; i++)
            {
                labels.Add(GetLootLabel(loot[i]));
            }

            return string.Join(", ", labels);
        }
        /// <summary>
        /// 把单个 LootDefinition 格式化成带稀有度的显示文字。
        /// </summary>
        /// <param name="loot">单个战利品数据，包含物品名、稀有度和权重。</param>
        /// <returns>返回整理后的文字，用于 UI 显示、日志或状态提示。</returns>
        private static string GetLootLabel(LootDefinition loot)
        {
            if (string.IsNullOrWhiteSpace(loot.rarity) || loot.rarity == "Collectible")
            {
                return loot.itemName;
            }

            return $"{loot.itemName} [{loot.rarity}]";
        }
        /// <summary>
        /// 读取场景里的 ChestSpawnPoint，并根据 chestsPerRun 随机生成宝箱。
        /// </summary>
        private void SpawnRandomChests()
        {
            Chest[] existingChests = FindObjectsOfType<Chest>();
            foreach (Chest chest in existingChests)
            {
                // 运行时统一生成宝箱，先清掉场景里旧的 Chest，避免手放宝箱和随机宝箱重复。
                Destroy(chest.gameObject);
            }

            if (HasClaimedChestInCurrentScene())
            {
                // 如果本关宝箱已领取，重载场景后也不再生成，防止死亡/切场景后刷奖励。
                UpdateStatus("This level's chest has already been claimed this run.");
                return;
            }

            ChestSpawnPoint[] points = FindObjectsOfType<ChestSpawnPoint>();
            if (points.Length == 0)
            {
                return;
            }

            List<ChestSpawnPoint> available = new List<ChestSpawnPoint>(points);
            int spawnCount = Mathf.Min(chestsPerRun, available.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                // 从剩余点位随机抽一个并移除，保证同一轮不会两个宝箱生成在同一个点。
                int index = Random.Range(0, available.Count);
                ChestSpawnPoint point = available[index];
                available.RemoveAt(index);
                point.HideMarkerVisuals();

                GameObject chestObject = CreateChestBlock(point.transform.position, i, spawnCount);
                Chest chest = chestObject.AddComponent<Chest>();
                chest.spawnPoint = point;
            }
        }
        /// <summary>
        /// 在指定位置创建运行时宝箱对象，补齐视觉、动画、Collider 和 Chest 脚本。
        /// </summary>
        /// <param name="position">目标世界坐标，常用于重生、生成对象或记录 Echo 帧。</param>
        /// <param name="index">index 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="total">total 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回创建或找到的 GameObject，方便调用方继续添加组件或设置位置。</returns>
        private static GameObject CreateChestBlock(Vector3 position, int index, int total)
        {
            string chestName = total == 1 ? "Chest_Block" : $"Chest_Block_{index + 1}";
            GameObject chestObject = new GameObject(chestName);
            chestObject.transform.position = position;

            GameObject visualObject = new GameObject("ChestVisual");
            visualObject.transform.SetParent(chestObject.transform, false);
            visualObject.transform.localPosition = new Vector3(0f, ChestVisualYOffset, 0f);
            visualObject.transform.localScale = new Vector3(ChestVisualScale, ChestVisualScale, 1f);

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetChestSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = ChestSortingOrder;
            visualObject.AddComponent<ChestAnimationController>();

            BoxCollider2D trigger = chestObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;

            return chestObject;
        }
        /// <summary>
        /// 创建或返回一个运行时宝箱占位 Sprite，给动态生成的宝箱使用。
        /// </summary>
        /// <returns>返回加载或生成的 Sprite；资源不存在时可能返回 null。</returns>
        private static Sprite GetChestSprite()
        {
            if (chestSprite != null)
            {
                return chestSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            float pixelsPerUnit = Mathf.Max(texture.width, texture.height);
            chestSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            chestSprite.name = "RuntimeChestSquare";
            return chestSprite;
        }
        /// <summary>
        /// 生成当前场景用于记录宝箱领取状态的 key。
        /// </summary>
        /// <returns>返回整理后的文字，用于 UI 显示、日志或状态提示。</returns>
        private static string GetCurrentSceneChestKey()
        {
            Scene scene = SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        /// <summary>
        /// 清理本次运行中的宝箱状态缓存。
        /// </summary>
        private static void ResetRunChestState()
        {
            ResetChestClaimsForNewRun();
        }
    }
    [System.Serializable]
    public struct LootDefinition
    {
        public string itemName;
        public string id;
        public string rarity;
        public string description;
        public Sprite icon;
        public int weight;
        public LootDefinition(string itemName, string rarity, int weight)
        {
            id = itemName;
            this.itemName = itemName;
            this.rarity = rarity;
            description = string.Empty;
            icon = null;
            this.weight = weight;
        }
        public LootDefinition(string id, string itemName, string description, Sprite icon, int weight)
        {
            this.id = id;
            this.itemName = itemName;
            rarity = "Collectible";
            this.description = description;
            this.icon = icon;
            this.weight = weight;
        }
    }
}
