using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：关卡出口/传送门脚本。玩家到达出口时，它负责结算 loot、播放通关反馈，并决定是否加载下一关或播放最终结尾。
    /// 玩法逻辑：进入触发器后先确认对象是真玩家；然后把 pending loot 结算成 secured loot；Level1/Level2 会进入下一关；Level3 会先显示获得 loot，再播放巫师结束语。
    /// 协作关系：调用 EchoEscapeGameManager、LootFeedbackUI、LevelIntroSequence 和 SceneManager。
    /// </summary>
    public class GoalZone : MonoBehaviour
    {
        private const string Level3SceneName = "Level3_RiskReward";
        private const string MainMenuSceneName = "MainMenu";
        private const float Level3EndingDelayAfterLoot = 2.75f;

        [SerializeField]
        private string nextSceneName;

        [SerializeField]
        private bool useNextBuildIndex;

        [SerializeField]
        private bool debugLogs = true;

        private bool hasTriggered;
        /// <summary>
        /// 运行时设置通关后要加载的下一关名字。用于场景或代码动态配置出口。
        /// </summary>
        /// <param name="sceneName">目标场景名称，用于检查 Build Settings 或加载下一关。</param>
        public void ConfigureNextScene(string sceneName)
        {
            nextSceneName = sceneName;
            useNextBuildIndex = false;
        }
        /// <summary>
        /// 玩家进入出口触发器时执行通关逻辑：确认玩家、结算 loot、播放反馈、加载下一关或触发结尾。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered || !IsPlayer(other))
            {
                // 终点只能触发一次，而且必须是真玩家；Echo 不能替玩家通关。
                return;
            }

            hasTriggered = true;

            if (debugLogs)
            {
                Debug.Log("Goal reached by Player.");
            }

            if (TryLoadNextScene())
            {
                // 已经成功进入切关/结尾流程时，不再执行“只完成当前关”的备用逻辑。
                return;
            }

            // 没配置下一关或下一关不可用时，至少让当前关按胜利流程结算。
            CompleteCurrentLevelOnly();
        }
        /// <summary>
        /// 判断进入终点触发器的 Collider 是否属于真正玩家。Echo 不应该通关。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool IsPlayer(Collider2D other)
        {
            return HasTag(other, "Player") ||
                other.GetComponent<PlayerController2D>() != null ||
                other.GetComponentInParent<PlayerController2D>() != null;
        }
        /// <summary>
        /// 尝试加载配置好的下一关。如果没有可加载场景，就只完成当前关卡，不强行切换。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool TryLoadNextScene()
        {
            string explicitSceneName = string.IsNullOrWhiteSpace(nextSceneName) ? string.Empty : nextSceneName.Trim();
            if (!string.IsNullOrEmpty(explicitSceneName))
            {
                // 优先使用 Inspector 明确配置的场景名，避免 Build Index 顺序变化导致进错关。
                return LoadSceneIfAvailable(explicitSceneName);
            }

            if (!useNextBuildIndex)
            {
                // 没启用 Build Index 自动切关时，出口只负责结算当前关。
                return false;
            }

            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int nextIndex = currentIndex + 1;
            if (currentIndex < 0 || nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning("Next scene is not available in Build Settings.");
                return false;
            }

            string scenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (debugLogs)
            {
                Debug.Log("Loading next scene: " + sceneName);
            }

            // 通过 Build Index 自动进下一关前，同样要先把 pending loot 结算成 secured loot。
            SecurePendingLootBeforeSceneLoad();
            SceneManager.LoadScene(nextIndex);
            return true;
        }
        /// <summary>
        /// 检查 Build Settings 里是否存在目标场景，存在才调用 SceneManager.LoadScene。
        /// </summary>
        /// <param name="sceneName">目标场景名称，用于检查 Build Settings 或加载下一关。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool LoadSceneIfAvailable(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning("Next scene is not available in Build Settings.");
                return false;
            }

            if (debugLogs)
            {
                Debug.Log("Loading next scene: " + sceneName);
            }

            if (TryShowLevel3EndingBeforeSceneLoad(sceneName))
            {
                // 第三关去主菜单前需要先显示 loot，再显示结尾剧情，所以这里交给协程。
                return true;
            }

            // 普通关卡切换前先结算 loot，确保进入下一关时奖励已保存。
            SecurePendingLootBeforeSceneLoad();
            SceneManager.LoadScene(sceneName);
            return true;
        }
        /// <summary>
        /// 第三关结束专用逻辑。它会先让 loot 结算 UI 出现，再播放最终剧情，保证顺序符合玩家体验。
        /// </summary>
        /// <param name="sceneName">目标场景名称，用于检查 Build Settings 或加载下一关。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool TryShowLevel3EndingBeforeSceneLoad(string sceneName)
        {
            if (SceneManager.GetActiveScene().name != Level3SceneName || sceneName != MainMenuSceneName)
            {
                // 只有 Level3 -> MainMenu 才需要最终剧情，其他切关走普通逻辑。
                return false;
            }

            LevelIntroSequence introSequence = FindObjectOfType<LevelIntroSequence>();
            if (introSequence == null)
            {
                Debug.LogWarning("Level3 ending dialogue could not be shown because LevelIntroSequence is missing.");
                return false;
            }

            StartCoroutine(ShowLevel3EndingAfterLootFeedback(sceneName, introSequence));
            return true;
        }
        /// <summary>
        /// 切换场景前结算 pendingLoot，防止玩家通关后刚拿到的物品没进入 securedLoot。
        /// </summary>
        /// <returns>返回整数结果，通常表示数量、索引或本次结算数量。</returns>
        private int SecurePendingLootBeforeSceneLoad()
        {
            if (EchoEscapeGameManager.Instance != null)
            {
                return EchoEscapeGameManager.Instance.SecurePendingLoot();
            }

            return 0;
        }
        /// <summary>
        /// 协程：等待 loot 反馈展示一段时间，再播放第三关结尾剧情，最后进入目标场景。
        /// </summary>
        /// <param name="targetSceneName">targetSceneName 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="introSequence">introSequence 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回 Unity 协程，调用方会用 StartCoroutine 让这个流程分帧执行。</returns>
        private IEnumerator ShowLevel3EndingAfterLootFeedback(string targetSceneName, LevelIntroSequence introSequence)
        {
            int securedLootCount = SecurePendingLootBeforeSceneLoad();
            if (securedLootCount > 0)
            {
                // 有 loot 时先给 UI 时间展示“获得了什么”，再播放巫师结束语。
                yield return new WaitForSecondsRealtime(Level3EndingDelayAfterLoot);
            }

            if (introSequence != null && introSequence.ShowEndingSequence(targetSceneName))
            {
                // LevelIntroSequence 会自己播放结尾并负责加载目标场景。
                yield break;
            }

            // 如果没有结尾系统或播放失败，仍然切回目标场景，避免玩家卡在终点。
            SceneManager.LoadScene(targetSceneName);
        }
        /// <summary>
        /// 没有下一关可加载时，只标记当前关卡完成并播放通关反馈。
        /// </summary>
        private void CompleteCurrentLevelOnly()
        {
            if (EchoEscapeGameManager.Instance != null)
            {
                // 使用 GameManager.Win 统一播放成功音效、结算 loot 和更新 HUD。
                EchoEscapeGameManager.Instance.Win();
            }
            else
            {
                Debug.Log("Level complete. Player reached the exit.");
            }
        }
        /// <summary>
        /// 安全检查 Collider 或根对象 tag，避免对象没有对应 tag 时抛 UnityException。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        /// <param name="tagName">tagName 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool HasTag(Collider2D other, string tagName)
        {
            try
            {
                return other.CompareTag(tagName) || other.transform.root.CompareTag(tagName);
            }
            catch (UnityException)
            {
                return false;
            }
        }
    }
}
