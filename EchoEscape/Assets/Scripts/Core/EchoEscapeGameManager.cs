using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoEscape
{
    // 这个脚本统一管理原型关卡状态，包括玩家、回声、宝箱奖励、死亡和通关。
    /// <summary>
    /// Coordinates the main prototype game state for Echo Escape.
    /// </summary>
    /// <remarks>
    /// Attach this script to a scene-level Game Manager object.
    /// It connects Player, ActionRecorder, TutorialDirector, audio, visual skinning, loot, death, and win state.
    /// It is the central place other scripts call when the player dies, finds loot, or reaches the exit.
    /// </remarks>
    public class EchoEscapeGameManager : MonoBehaviour
    {
        // 这个属性保存当前场景里的全局 GameManager 引用，方便其他脚本访问。
        /// <summary>
        /// Global reference to the active game manager in the scene.
        /// </summary>
        public static EchoEscapeGameManager Instance { get; private set; }

        [Header("Scene References")]
        // 这个变量指向当前场景里的玩家控制器。
        /// <summary>
        /// Player controller currently used in the scene.
        /// </summary>
        public PlayerController2D player;

        // 这个变量保存玩家身上的录制器，用于 Q/E 录制和播放回声。
        /// <summary>
        /// Recorder attached to the player for Q/E Echo playback.
        /// </summary>
        public ActionRecorder recorder;

        // 这个变量保存玩家死亡后重生的位置。
        /// <summary>
        /// Transform used when respawning the player after death.
        /// </summary>
        public Transform playerSpawn;

        [Header("Loot")]
        // 这个变量控制从 ChestSpawnPoint 标记里生成几个随机宝箱。
        /// <summary>
        /// Number of random chests to create from available ChestSpawnPoint markers.
        /// </summary>
        public int chestsPerRun = 2;

        // 这个数组保存宝箱可开出的奖励，以及每个奖励的随机权重。
        /// <summary>
        /// Weighted loot entries used when a chest is opened.
        /// </summary>
        public LootDefinition[] lootTable;

        [Header("Tutorial / HUD")]
        // 这个选项为 true 时，PrototypeHud 会显示旧版教程目标追踪信息。
        /// <summary>
        /// If true, the older objective tracker is used by PrototypeHud.
        /// </summary>
        public bool useTutorialDirector = true;

        // 这个选项为 true 时，运行时会自动把原型方块替换成像素风 Sprite。
        /// <summary>
        /// If true, prototype blocks are automatically replaced with pixel-art sprites at runtime.
        /// </summary>
        public bool usePrototypeVisualSkinner = true;

        // 这个字符串是场景开始时 HUD 显示的第一条状态提示。
        /// <summary>
        /// First status message shown by the HUD when the scene starts.
        /// </summary>
        public string startingStatusMessage = "Tutorial started. Learn record, replay, loot risk, and extraction.";

        // 这个选项为 true 时，会显示基于 Canvas 的奖励弹窗和奖励状态栏。
        /// <summary>
        /// If true, a Canvas-based loot pickup popup and loot summary are shown.
        /// </summary>
        public bool useLootFeedbackUi = true;

        [Header("Death")]
        [SerializeField]
        private float deathReloadDelay = 1f;

        // 这个属性保存 HUD 当前要显示的短状态信息。
        /// <summary>
        /// Current short message shown by prototype HUD or status systems.
        /// </summary>
        public string StatusMessage { get; private set; } = "Reach the exit. Use your echo to hold the plate.";

        // 这个属性记录本轮游戏中玩家死亡的次数。
        /// <summary>
        /// Number of player deaths during this run.
        /// </summary>
        public int DeathCount { get; private set; }

        // 这个属性表示玩家是否已经到达出口并完成关卡。
        /// <summary>
        /// True after the player has reached the exit.
        /// </summary>
        public bool HasWon { get; private set; }

        // 这个属性保存挂在 GameManager 上的教程状态机。
        /// <summary>
        /// Tutorial state machine attached to the Game Manager.
        /// </summary>
        public TutorialDirector Tutorial { get; private set; }

        // 这个属性保存给游戏脚本播放音效用的辅助组件。
        /// <summary>
        /// Audio helper used by gameplay scripts.
        /// </summary>
        public PrototypeAudio AudioService { get; private set; }

        // 这个属性保存外观替换组件，用来把占位方块换成像素图。
        /// <summary>
        /// Visual skinning helper used to replace placeholder blocks with pixel art.
        /// </summary>
        public PrototypeVisualSkinner VisualSkinner { get; private set; }

        // 这个属性保存原型场景里的 Canvas 奖励反馈 UI。
        /// <summary>
        /// Canvas-based loot feedback UI created for prototype scenes.
        /// </summary>
        public LootFeedbackUI LootFeedback { get; private set; }

        // 这个属性保存已经获得但还没带到出口保存的奖励。
        /// <summary>
        /// Loot found but not yet secured at the exit.
        /// </summary>
        public IReadOnlyList<LootDefinition> PendingLoot => pendingLoot;

        // 这个属性保存玩家到达出口后已经成功保住的奖励。
        /// <summary>
        /// Loot successfully banked by reaching the exit.
        /// </summary>
        public IReadOnlyList<LootDefinition> SecuredLoot => securedLoot;

        // 这个属性返回当前还没有保存到出口的奖励数量。
        /// <summary>
        /// Number of currently unbanked loot items.
        /// </summary>
        public int PendingLootCount => pendingLoot.Count;

        // 这个属性返回已经安全保存的奖励数量。
        /// <summary>
        /// Number of secured loot items.
        /// </summary>
        public int SecuredLootCount => securedLoot.Count;

        private readonly List<LootDefinition> pendingLoot = new List<LootDefinition>();
        private readonly List<LootDefinition> securedLoot = new List<LootDefinition>();
        private static Sprite chestSprite;
        private bool deathInProgress;

        // 这个函数在 GameManager 创建时运行，用来设置单例引用和基础组件。
        /// <summary>
        /// Unity event method called when the Game Manager is created.
        /// </summary>
        /// <remarks>
        /// Sets the singleton reference, ensures audio/visual helper components exist, and creates fallback loot data.
        /// </remarks>
        private void Awake()
        {
            Instance = this;
            EnsureAudioListener();
            EnsurePresentationServices();

            if (lootTable == null || lootTable.Length == 0)
            {
                lootTable = new[]
                {
                    new LootDefinition("Old Coin", "Common", 60),
                    new LootDefinition("Blue Gem", "Rare", 30),
                    new LootDefinition("Golden Relic", "Epic", 10)
                };
            }
        }

        // 这个函数在第一帧更新之前运行，用来做场景或组件的初始设置。
        /// <summary>
        /// Unity event method called before the first frame update.
        /// </summary>
        /// <remarks>
        /// Finds scene references, starts the tutorial, spawns chests, and applies visual skinning.
        /// </remarks>
        private void Start()
        {
            if (player == null)
            {
                player = FindObjectOfType<PlayerController2D>();
            }

            if (recorder == null && player != null)
            {
                recorder = player.GetComponent<ActionRecorder>();
            }

            if (useTutorialDirector)
            {
                Tutorial = GetComponent<TutorialDirector>();
                if (Tutorial == null)
                {
                    Tutorial = gameObject.AddComponent<TutorialDirector>();
                }
            }
            else
            {
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

        // 这个函数每一帧运行一次，用来检查输入或更新当前对象状态。
        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        /// <remarks>
        /// Handles the R key scene restart shortcut for quick playtesting.
        /// </remarks>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        // 这个函数更新 HUD 上显示的短状态信息。
        /// <summary>
        /// Updates the short gameplay status message displayed by HUD systems.
        /// </summary>
        /// <param name="message">Message to store as the current status.</param>
        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }

        // 这个函数把刚从宝箱开出的奖励加入待保存列表。
        /// <summary>
        /// Adds newly opened chest loot to the pending loot list.
        /// </summary>
        /// <param name="loot">Loot definition rolled by a chest.</param>
        public void AddPendingLoot(LootDefinition loot)
        {
            pendingLoot.Add(loot);
            UpdateStatus($"Found {loot.rarity} loot: {loot.itemName}. Escape safely or lose it on death.");
            RefreshLootFeedback();
            LootFeedback?.ShowLootFound(loot);
            Debug.Log($"Pending loot updated: {loot.itemName}.");
        }

        // 这个函数根据权重表随机选出一个宝箱奖励。
        /// <summary>
        /// Selects one loot entry using the weighted loot table.
        /// </summary>
        /// <returns>The selected LootDefinition.</returns>
        public LootDefinition RollLoot()
        {
            int totalWeight = 0;
            foreach (LootDefinition loot in lootTable)
            {
                totalWeight += Mathf.Max(1, loot.weight);
            }

            int roll = Random.Range(0, totalWeight);
            foreach (LootDefinition loot in lootTable)
            {
                roll -= Mathf.Max(1, loot.weight);
                if (roll < 0)
                {
                    return loot;
                }
            }

            return lootTable[0];
        }

        // 这个函数处理玩家死亡：清空未保存奖励、移除当前回声，并把玩家送回复活点。
        /// <summary>
        /// Handles player death, clears pending loot, removes active Echo, and reloads the current scene.
        /// </summary>
        /// <param name="reason">Short text explaining why the player died.</param>
        public void KillPlayer(string reason)
        {
            if (HasWon || deathInProgress)
            {
                return;
            }

            deathInProgress = true;
            DeathCount++;
            AudioService?.PlayHurt();
            List<LootDefinition> lostLoot = new List<LootDefinition>(pendingLoot);
            pendingLoot.Clear();

            if (recorder != null)
            {
                recorder.DestroyActiveEcho();
            }

            DisablePlayerForDeath();

            string lossText = lostLoot.Count > 0 ? $" Loot Lost: {FormatLoot(lostLoot)}" : string.Empty;
            UpdateStatus($"You died: {reason}.{lossText}");
            RefreshLootFeedback();
            LootFeedback?.ShowDeath(lostLoot);

            if (lostLoot.Count > 0)
            {
                Debug.Log("Player died. Pending loot lost.");
            }

            StartCoroutine(ReloadCurrentSceneAfterDeath());
        }

        // 这个函数在玩家到达出口时完成本轮游戏，并把待保存奖励转成已保存奖励。
        /// <summary>
        /// Completes the run and moves pending loot into secured loot.
        /// </summary>
        /// <remarks>
        /// Called by GoalZone when the player reaches the exit trigger.
        /// </remarks>
        public void Win()
        {
            if (HasWon)
            {
                return;
            }

            HasWon = true;
            securedLoot.AddRange(pendingLoot);
            int securedThisRun = pendingLoot.Count;
            pendingLoot.Clear();
            AudioService?.PlaySuccess();
            UpdateStatus($"Extraction complete. Secured {securedThisRun} loot item(s). Press R to replay.");
            RefreshLootFeedback();
            Debug.Log("Extraction successful. Loot secured.");
        }

        // 这个函数确保 GameManager 上有音效服务和像素外观辅助组件。
        /// <summary>
        /// Ensures audio and visual helper components exist on the Game Manager.
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

        // 这个函数在生成场景缺少 AudioListener 时自动补上，保证音效可以播放。
        /// <summary>
        /// Ensures SFX can play in generated scenes that started without an AudioListener.
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

        // 这个函数把当前待保存和已保存奖励同步到 Canvas 奖励 UI。
        /// <summary>
        /// Pushes the current loot lists into the Canvas feedback UI.
        /// </summary>
        private void RefreshLootFeedback()
        {
            if (useLootFeedbackUi)
            {
                LootFeedback?.RefreshLootState(pendingLoot, securedLoot);
            }
        }

        private IEnumerator ReloadCurrentSceneAfterDeath()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, deathReloadDelay));
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void DisablePlayerForDeath()
        {
            if (player == null)
            {
                return;
            }

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.velocity = Vector2.zero;
            }

            PlayerAttack attack = player.GetComponent<PlayerAttack>();
            if (attack != null)
            {
                attack.enabled = false;
            }

            GravityFlipController gravityFlip = player.GetComponent<GravityFlipController>();
            if (gravityFlip != null)
            {
                gravityFlip.enabled = false;
            }

            if (recorder != null)
            {
                recorder.enabled = false;
            }

            player.enabled = false;
        }

        private static string FormatLoot(IReadOnlyList<LootDefinition> loot)
        {
            if (loot == null || loot.Count == 0)
            {
                return "none";
            }

            List<string> labels = new List<string>(loot.Count);
            for (int i = 0; i < loot.Count; i++)
            {
                labels.Add($"{loot[i].itemName} [{loot[i].rarity}]");
            }

            return string.Join(", ", labels);
        }

        // 这个函数从可用 ChestSpawnPoint 中选一部分位置生成宝箱。
        /// <summary>
        /// Spawns random chest objects at a subset of available ChestSpawnPoint markers.
        /// </summary>
        /// <remarks>
        /// Existing chests are removed first so restarting or rebuilding does not duplicate them.
        /// </remarks>
        private void SpawnRandomChests()
        {
            Chest[] existingChests = FindObjectsOfType<Chest>();
            foreach (Chest chest in existingChests)
            {
                Destroy(chest.gameObject);
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
                int index = Random.Range(0, available.Count);
                ChestSpawnPoint point = available[index];
                available.RemoveAt(index);
                point.HideMarkerVisuals();

                GameObject chestObject = CreateChestBlock(point.transform.position, i, spawnCount);
                Chest chest = chestObject.AddComponent<Chest>();
                chest.spawnPoint = point;
            }
        }

        // 这个函数根据宝箱生成点创建可见但不会挡住玩家移动的宝箱对象。
        /// <summary>
        /// Creates the visible, non-blocking chest object spawned from a chest marker.
        /// </summary>
        /// <param name="position">World position for the chest.</param>
        /// <param name="index">Index of this chest in the current spawn pass.</param>
        /// <param name="total">Total number of chests spawned this pass.</param>
        /// <returns>The generated chest GameObject.</returns>
        private static GameObject CreateChestBlock(Vector3 position, int index, int total)
        {
            string chestName = total == 1 ? "Chest_Block" : $"Chest_Block_{index + 1}";
            GameObject chestObject = new GameObject(chestName);
            chestObject.transform.position = position;
            chestObject.transform.localScale = new Vector3(0.85f, 0.55f, 1f);

            SpriteRenderer renderer = chestObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetChestSprite();
            renderer.color = new Color(1f, 0.74f, 0.22f);
            renderer.sortingOrder = 5;

            BoxCollider2D trigger = chestObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;

            return chestObject;
        }

        // 这个函数在运行时创建简单方形 Sprite，给生成出来的宝箱当图片用。
        /// <summary>
        /// Creates a simple runtime square sprite for generated chests.
        /// </summary>
        /// <returns>A shared square sprite.</returns>
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
    }

    // 这个结构定义一种可能的奖励，包括名字、稀有度和随机权重。
    /// <summary>
    /// Defines one possible loot item and its weighted random chance.
    /// </summary>
    /// <remarks>
    /// EchoEscapeGameManager uses this serializable struct as entries in the loot table.
    /// </remarks>
    [System.Serializable]
    public struct LootDefinition
    {
        // 这个变量保存奖励物品显示给玩家看的名字。
        /// <summary>
        /// Display name of the loot item.
        /// </summary>
        public string itemName;

        // 这个变量保存奖励稀有度，用来显示在状态文字里。
        /// <summary>
        /// Rarity label shown in status messages.
        /// </summary>
        public string rarity;

        // 这个变量保存 RollLoot 抽奖时使用的相对权重。
        /// <summary>
        /// Relative random weight used by RollLoot.
        /// </summary>
        public int weight;

        // 这个构造函数创建一条奖励表数据。
        /// <summary>
        /// Creates a loot table entry.
        /// </summary>
        /// <param name="itemName">Display name of the item.</param>
        /// <param name="rarity">Rarity label for the item.</param>
        /// <param name="weight">Relative random selection weight.</param>
        public LootDefinition(string itemName, string rarity, int weight)
        {
            this.itemName = itemName;
            this.rarity = rarity;
            this.weight = weight;
        }
    }
}
