using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Level Manager, yes Echo Escape The central script for each level runtime. It is responsible for connecting players, Echo Recording, tutorials, sound effects, visual reskin, treasure chest, loot, death and clearance.
/// Gameplay logic: Find at the beginning of the level Player and ActionRecorder, prepare audio/Visual service, initialize the fixed treasure chests in the scene and refresh them HUD; The player gets loot put in first pendingLoot, the death will be lost; when reaching the exit, put pendingLoot Convert to securedLoot; Play death animation when death and UI Then reload the current level.
/// Collaborates with: HazardZone、GravityFlipVoidKillZone、EnemyAttack、Chest、CollectibleItem、GoalZone、LootFeedbackUI、PrototypeAudio、PrototypeVisualSkinner will call or depend on it.
    /// </summary>
    public class EchoEscapeGameManager : MonoBehaviour
    {
        public static EchoEscapeGameManager Instance { get; private set; }

        [Header("Scene References")]
        public PlayerController2D player;
        public ActionRecorder recorder;
        public Transform playerSpawn;

        [Header("Loot")]
        public LootDefinition[] lootTable;

        [Header("Tutorial / HUD")]
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
        public PrototypeAudio AudioService { get; private set; }
        public PrototypeVisualSkinner VisualSkinner { get; private set; }
        public LootFeedbackUI LootFeedback { get; private set; }
        public RecordingStatusUI RecordingStatus { get; private set; }
        public IReadOnlyList<LootDefinition> PendingLoot => pendingLoot;
        public IReadOnlyList<LootDefinition> SecuredLoot => securedLoot;
        public int PendingLootCount => pendingLoot.Count;
        public int SecuredLootCount => securedLoot.Count;

        private readonly List<LootDefinition> pendingLoot = new List<LootDefinition>();
        private static readonly List<LootDefinition> securedLoot = new List<LootDefinition>();
        private static readonly HashSet<string> scenesWithClaimedChest = new HashSet<string>();
        private bool deathInProgress;
        /// <summary>
/// Create the current level GameManager Singleton and prepare AudioListener, audio service, visual reskin service and default lootTable。
        /// </summary>
        private void Awake()
        {
// Each level only needs one current GameManager, other scripts through Instance Find the unified entrance.
            Instance = this;
            EnsureAudioListener();
            EnsurePresentationServices();

            if (lootTable == null || lootTable.Length == 0)
            {
// if Inspector Not configured lootTable, just give a minimum playable default table to avoid being empty out of the box.
                lootTable = new[]
                {
                    new LootDefinition("Old Coin", "Common", 60),
                    new LootDefinition("Blue Gem", "Rare", 30),
                    new LootDefinition("Golden Relic", "Epic", 10)
                };
            }
        }
        /// <summary>
/// Finds players and recorders at the start of a level, starts the tutorial system, initializes fixed treasure chests, applies visual skinning, and refreshes the initial state and loot UI。
        /// </summary>
        private void Start()
        {
            if (player == null)
            {
// The scene is automatically found when the reference is not manually dragged. Player, reduce the probability of scene configuration errors.
                player = FindObjectOfType<PlayerController2D>();
            }

            if (recorder == null && player != null)
            {
// Recorder usually hung on Player superior, GameManager Need it to clean up on death Echo。
                recorder = player.GetComponent<ActionRecorder>();
            }

            InitializeFixedChests();
            if (usePrototypeVisualSkinner)
            {
                VisualSkinner?.SkinAll();
            }

            UpdateStatus(startingStatusMessage);
            RefreshLootFeedback();
        }
        /// <summary>
/// Update the current level status text. tutorial or formal UI This status can be read and displayed to the player.
        /// </summary>
/// <param name="message">to be displayed to HUD Or the text written in the log. </param>
        public void UpdateStatus(string message)
        {
            StatusMessage = message;
        }
        /// <summary>
/// Called when the player obtains loot. loot will enter pendingLoot, means "got it but haven't taken it away safely", death will be lost.
        /// </summary>
/// <param name="loot">Individual loot data, including item name, rarity, and weight. </param>
        public void AddPendingLoot(LootDefinition loot)
        {
// pendingLoot It means "I got it in this level but haven't taken it out yet", and it will be cleared when you die.
            pendingLoot.Add(loot);
            UpdateStatus($"Found: {loot.itemName}. Escape safely or lose it on death.");
            RefreshLootFeedback();
            LootFeedback?.ShowLootFound(loot);
            Debug.Log($"Pending loot updated: {loot.itemName}.");
        }
        /// <summary>
/// according to lootTable A reward is randomly selected based on the weight. Higher weight items are more common, lower weight items are rarer.
        /// </summary>
/// <returns>Returns a loot definition, which will be entered later pending or secured loot process. </returns>
        public LootDefinition RollLoot()
        {
            IReadOnlyList<CollectibleItem> collectibles = CollectibleDatabase.GetAllCollectibles();
            if (collectibles.Count > 0)
            {
// Use the current version first CollectibleDatabase, conveniently displaying the official collection icon and name.
                return CollectibleDatabase.GetRandomCollectible().ToLootDefinition();
            }

            int totalWeight = 0;
            foreach (LootDefinition loot in lootTable)
            {
// Mathf. Max Prevent configuration 0 Or negative weights cause the total weight to be abnormal.
                totalWeight += Mathf.Max(1, loot.weight);
            }

            int roll = Random.Range(0, totalWeight);
            foreach (LootDefinition loot in lootTable)
            {
// Deduct weights item by item, roll Whichever range you fall in will be drawn. loot。
                roll -= Mathf.Max(1, loot.weight);
                if (roll < 0)
                {
                    return loot;
                }
            }

            return lootTable[0];
        }
        /// <summary>
/// Check whether the treasure chest reward of the current scene has been claimed. Used to prevent repeated opening of treasure chests to obtain rewards in the same round.
        /// </summary>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        public bool HasClaimedChestInCurrentScene()
        {
            return scenesWithClaimedChest.Contains(GetCurrentSceneChestKey());
        }
        /// <summary>
/// Mark the current scene treasure chest as having been collected. Called after the treasure chest is successfully settled.
        /// </summary>
        public void MarkChestClaimedInCurrentScene()
        {
            scenesWithClaimedChest.Add(GetCurrentSceneChestKey());
        }
        /// <summary>
/// When starting a new game, clear the treasure chest collection records in all scenes so that you can collect treasure chests again in a new game.
        /// </summary>
        public static void ResetChestClaimsForNewRun()
        {
            scenesWithClaimedChest.Clear();
        }
        /// <summary>
/// Called when the exit is reached. it puts pendingLoot move to securedLoot, indicating that the player successfully took the risky items out of the level.
        /// </summary>
/// <returns>Returns an integer result, usually representing the quantity, index, or quantity of this settlement. </returns>
        public int SecurePendingLoot()
        {
            if (pendingLoot.Count == 0)
            {
// Refresh even if there are no items to be settled UI, ensure HUD Old status is not shown.
                RefreshLootFeedback();
                return 0;
            }

// Copy first and then clear pending, avoid UI The cleared list is referenced during display.
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
/// Unify the player death entrance. Danger zones, enemies and anti-gravity deaths should all be called here to ensure death animations, UI、loot The logic of loss and reloading is consistent.
        /// </summary>
/// <param name="reason">cause of death or event, used for death UI, status prompts and debugging logs. </param>
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
// Prevent you from repeatedly entering the death process when you have cleared the level or are dying, and avoid multiple reloads or UI Overlay.
                return;
            }

            deathInProgress = true;
            DeathCount++;
            AudioService?.PlayHurt();
// Death will throw away items not taken out of this level. pending loot, but will not affect the already secured items.
            List<LootDefinition> lostLoot = new List<LootDefinition>(pendingLoot);
            pendingLoot.Clear();

            if (recorder != null)
            {
// Destroyed on death Echo, before reloading Echo Continue to press the switch or trigger a strange state.
                recorder.DestroyActiveEcho();
            }

// Freeze the player before playing the death process to avoid the character being pushed by input or physics during the death animation.
            DisablePlayerForDeath();
            StartCoroutine(HandleDeathSequence(reason, lostLoot));
        }
        /// <summary>
/// Complete death coroutine. It first plays the injury sound effect and death animation, and then displays the missing loot, and finally wait for a while and reload the current scene.
        /// </summary>
/// <param name="reason">cause of death or event, used for death UI, status prompts and debugging logs. </param>
/// <param name="lostLoot">Lost when player dies pending loot list. </param>
/// <returns>return Unity Coroutine, the caller will use StartCoroutine Let this process be executed in frames. </returns>
        private IEnumerator HandleDeathSequence(string reason, List<LootDefinition> lostLoot)
        {
// Play the character's death animation first, then display the death UI; Use if material is missing fallback Waiting time.
            float deathAnimationDuration = PlayPlayerDeathAnimation(reason);
            float deathAnimationWait = Mathf.Clamp(Mathf.Max(deathAnimationFallbackDelay, deathAnimationDuration), 0.6f, 1f);
            yield return new WaitForSecondsRealtime(deathAnimationWait);

// UI The text includes the cause of death and this loss loot Written clearly together, players can understand the risk-reward mechanics.
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
/// The winning entry for the current level. it sets HasWon, settlement pending loot, play the success sound effect, and update the status text.
        /// </summary>
        public void Win()
        {
            if (HasWon || deathInProgress)
            {
// Victory and death are mutually exclusive, preventing players from triggering the danger zone the moment they enter the door and causing status conflicts.
                return;
            }

            HasWon = true;
// When clearing customs pendingLoot convert to securedLoot, this step is the real reward.
            int securedThisRun = SecurePendingLoot();
            AudioService?.PlaySuccess();
            UpdateStatus(securedThisRun > 0
                ? $"Extraction complete. Secured {securedThisRun} collectible(s)."
                : "Extraction complete. No pending loot to secure.");
            RefreshLootFeedback();
            Debug.Log("Extraction successful. Loot secured.");
        }
        /// <summary>
/// Make sure the level has audio, visual skinning, and loot UI Serve. It will automatically hang when components are missing GameManager superior.
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

            RecordingStatus = GetComponent<RecordingStatusUI>();
            if (RecordingStatus == null)
            {
                RecordingStatus = gameObject.AddComponent<RecordingStatusUI>();
            }

            RecordingStatus.enabled = true;
        }
        /// <summary>
/// Make sure the scene has AudioListener, otherwise sound effects and music may play but not be heard.
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
/// put the current pendingLoot and securedLoot Sync to LootFeedbackUI, refresh the screen loot state.
        /// </summary>
        private void RefreshLootFeedback()
        {
            if (useLootFeedbackUi)
            {
                LootFeedback?.RefreshLootState(pendingLoot, securedLoot);
            }
        }
        /// <summary>
/// After the death feedback ends, reload the current scene to start over from the starting point of the level.
        /// </summary>
/// <returns>return Unity Coroutine, the caller will use StartCoroutine Let this process be executed in frames. </returns>
        private IEnumerator ReloadCurrentSceneAfterDeath()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, deathReloadDelay));
            Time.timeScale = 1f;
            string sceneName = SceneManager.GetActiveScene().name;
// After death, restart the current level and no longer play the level story introduction to avoid interrupting the rhythm.
            LevelIntroSequence.SkipNextIntroForScene(sceneName);
            SceneManager.LoadScene(sceneName);
        }
        /// <summary>
/// Find players visually PlayerAnimationController And play the death animation, return the animation duration to wait for the death process.
        /// </summary>
/// <param name="reason">cause of death or event, used for death UI, status prompts and debugging logs. </param>
/// <returns>Returns a floating point result, typically representing time, distance, speed, or animation duration. </returns>
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
/// Freezes player input and physical movement during death to prevent players from dying UI Or keep moving during the death animation.
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
// FreezeAll Lock the position and rotation to ensure that the death animation is displayed at the place of death and will not continue to slide.
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            PlayerAttack attack = player.GetComponent<PlayerAttack>();
            if (attack != null)
            {
// It is forbidden to continue attacking during death to avoid enemies/treasure chest/The mechanism continues to be affected during the death process.
                attack.enabled = false;
            }

            GravityFlipController gravityFlip = player.GetComponent<GravityFlipController>();
            if (gravityFlip != null)
            {
// After death, the player cannot be allowed to press the up and down keys to flip, otherwise the death animation and position will be interrupted.
                gravityFlip.enabled = false;
            }

            if (recorder != null)
            {
// Recording is prohibited during death/play Echo, to avoid leaving new ones before reloading Echo state.
                recorder.enabled = false;
            }

// last disabled PlayerController2D, so Update Inputs such as movement, jumping, and unboxing are no longer read.
            player.enabled = false;
        }
        /// <summary>
/// put a group loot Convert to UI Readable text, such as commas between multiple items.
        /// </summary>
/// <param name="loot">Individual loot data, including item name, rarity, and weight. </param>
/// <returns>Returns the sorted text for UI Display, log or status prompts. </returns>
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
/// put a single LootDefinition Formatted as display text with rarity.
        /// </summary>
/// <param name="loot">Individual loot data, including item name, rarity, and weight. </param>
/// <returns>Returns the sorted text for UI Display, log or status prompts. </returns>
        private static string GetLootLabel(LootDefinition loot)
        {
            if (string.IsNullOrWhiteSpace(loot.rarity) || loot.rarity == "Collectible")
            {
                return loot.itemName;
            }

            return $"{loot.itemName} [{loot.rarity}]";
        }
        /// <summary>
        /// Initializes fixed, manually placed chests in the current scene.
        /// If this scene's chest reward has already been claimed during the current run, the chest is hidden
        /// after a scene reload so the existing one-reward-per-scene rule stays unchanged without random spawning.
        /// </summary>
        private void InitializeFixedChests()
        {
            if (!HasClaimedChestInCurrentScene())
            {
                return;
            }

            Chest[] sceneChests = FindObjectsOfType<Chest>();
            foreach (Chest chest in sceneChests)
            {
                chest.gameObject.SetActive(false);
            }

            UpdateStatus("This level's chest has already been claimed this run.");
        }        /// <summary>
/// Generate the current scene to record the treasure chest collection status key。
        /// </summary>
/// <returns>Returns the sorted text for UI Display, log or status prompts. </returns>
        private static string GetCurrentSceneChestKey()
        {
            Scene scene = SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        /// <summary>
/// Clear the treasure chest status cache for this run.
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
