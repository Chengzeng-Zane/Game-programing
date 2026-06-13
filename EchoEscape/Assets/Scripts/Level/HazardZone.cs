using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：普通危险区脚本，用于坑、河流、地图底部死亡边界等。
    /// 玩法逻辑：玩家进入触发器后走统一死亡流程：播放死亡反馈、显示 You Died、丢失 pending loot，并重新加载当前关卡。它也提供 fallback，避免没有 GameManager 时完全没反应。
    /// 协作关系：EchoEscapeGameManager 是主要死亡入口；GravityFlipVoidKillZone 复用它的公共死亡函数。
    /// </summary>
    public class HazardZone : MonoBehaviour
    {
        public string deathReason = "hit a hazard";

        [SerializeField]
        private bool debugLogs;

        private static bool fallbackDeathInProgress;
        /// <summary>
        /// 玩家进入普通危险区时触发死亡流程。这个函数会过滤 Echo 和非玩家对象，避免机关或回放误杀玩家。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            bool isPlayer = other != null &&
                (other.GetComponent<PlayerController2D>() != null || other.GetComponentInParent<PlayerController2D>() != null);

            if (!isPlayer)
            {
                // 河流、坑底和底部死亡区只杀真正玩家；Echo 或其他触发器进入时不会重载关卡。
                return;
            }

            KillPlayerUsingExistingDeathFlow(this, deathReason, name, debugLogs);
        }
        /// <summary>
        /// 公共死亡入口。优先调用 EchoEscapeGameManager.KillPlayer；如果场景没有 GameManager，就用 fallback UI 和重载流程避免游戏卡死。
        /// </summary>
        /// <param name="runner">runner 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="reason">死亡原因或事件原因，用于死亡 UI、状态提示和调试日志。</param>
        /// <param name="sourceName">sourceName 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="writeDebugLogs">writeDebugLogs 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public static void KillPlayerUsingExistingDeathFlow(MonoBehaviour runner, string reason, string sourceName, bool writeDebugLogs)
        {
            bool hasGameManager = EchoEscapeGameManager.Instance != null;
            if (writeDebugLogs)
            {
                Debug.Log("[DeathFlow] KillPlayer called");
                Debug.Log($"[DeathFlow] reason = {reason}");
                Debug.Log($"[DeathFlow] will respawn/reload = {hasGameManager || runner != null}");
            }

            if (hasGameManager)
            {
                // 正常关卡都应该走 GameManager：它会处理死亡动画、UI、pending loot 丢失和重载当前关卡。
                EchoEscapeGameManager.Instance.KillPlayer(reason);
                return;
            }

            if (fallbackDeathInProgress || runner == null)
            {
                // fallback 只允许启动一次，避免多个危险区同时触发导致重复加载场景。
                return;
            }

            // 没有 GameManager 的测试场景也要能死亡并重载，方便单独测试危险区。
            fallbackDeathInProgress = true;
            runner.StartCoroutine(ShowDeathAndReloadCurrentScene());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        /// <summary>
        /// 重置 fallback 死亡状态。场景重载或测试时避免旧状态影响下一次死亡。
        /// </summary>
        private static void ResetFallbackDeathState()
        {
            fallbackDeathInProgress = false;
        }
        /// <summary>
        /// fallback 死亡协程。显示简单死亡 UI，等待一小段时间后重载当前场景。
        /// </summary>
        /// <returns>返回 Unity 协程，调用方会用 StartCoroutine 让这个流程分帧执行。</returns>
        private static IEnumerator ShowDeathAndReloadCurrentScene()
        {
            // fallback 没有 LootFeedbackUI，所以临时创建一个最简单的 You Died 面板。
            CreateFallbackDeathUi();
            yield return new WaitForSecondsRealtime(1f);
            Time.timeScale = 1f;
            string sceneName = SceneManager.GetActiveScene().name;
            // 死亡重载时跳过本关开场故事，玩家不会每死一次都重新看剧情。
            LevelIntroSequence.SkipNextIntroForScene(sceneName);
            SceneManager.LoadScene(sceneName);
        }
        /// <summary>
        /// 动态创建一个简单 You Died UI。只有在没有 GameManager 的 fallback 情况下才会用到。
        /// </summary>
        private static void CreateFallbackDeathUi()
        {
            GameObject canvasObject = new GameObject("DeathFeedbackUI");

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 130;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = new GameObject("DeathPanel");
            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(420f, 150f);

            Image image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.78f);
            image.raycastTarget = false;

            GameObject textObject = new GameObject("DeathText");
            textObject.transform.SetParent(panel.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(360f, 80f);

            Text text = textObject.AddComponent<Text>();
            text.text = "You Died";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
        }
    }
}
