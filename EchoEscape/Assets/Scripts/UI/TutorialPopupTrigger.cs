using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：问号教学触发器。玩家进入问号范围后，它会打开对应教学弹窗。
    /// 玩法逻辑：触发时先确认对象是真正玩家，再判断是否已经显示过；如果满足条件就把 tutorialTitle 和 tutorialMessage 交给 TutorialPopupManager。
    /// 协作关系：和 TutorialPopupManager 配合；hideAfterUse 可以让问号提示使用后隐藏。
    /// </summary>
    public class TutorialPopupTrigger : MonoBehaviour
    {
        [Tooltip("The popup manager that controls the tutorial UI.")]
        public TutorialPopupManager popupManager;

        [Tooltip("The title shown in the tutorial popup.")]
        public string tutorialTitle = "Tutorial";

        [Tooltip("The message shown in the tutorial popup.")]
        [TextArea(3, 8)]
        public string tutorialMessage = "Read this message, then press Close to continue.";

        [Tooltip("If true, this tutorial only appears once.")]
        public bool showOnlyOnce = true;

        [Tooltip("If true, the question mark object disappears after the tutorial is shown.")]
        public bool hideAfterUse = false;

        private bool hasShown = false;
        /// <summary>
        /// 2D Trigger 刚进入时调用。这里根据进入对象决定是否触发教学、机关、宝箱、死亡或通关。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (showOnlyOnce && hasShown)
            {
                // 只显示一次的教程触发过后直接忽略，避免玩家来回走动反复弹窗。
                return;
            }

            if (IsPlayer(other))
            {
                // 只有真正玩家进入问号范围才显示教程，Echo 或敌人不会触发弹窗。
                ShowTutorial();
            }
        }
        /// <summary>
        /// 判断进入问号范围的 Collider 是否属于真正玩家。支持 Player 根对象或玩家子物体 Collider。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private static bool IsPlayer(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.CompareTag("Player"))
            {
                // 标准 Player Tag 是最快路径。
                return true;
            }

            Transform root = other.transform.root;
            if (root != null && root.CompareTag("Player"))
            {
                // 有些触发器碰到的是玩家子对象，根对象带 Player Tag 也算玩家。
                return true;
            }

            // 最后用 PlayerController2D 兜底，避免 Tag 配置不完整时教程失效。
            return other.GetComponentInParent<PlayerController2D>() != null;
        }
        /// <summary>
        /// 显示对应 UI 或视觉状态，通常用于弹窗、loot 提示、死亡提示或结算反馈。
        /// </summary>
        private void ShowTutorial()
        {
            if (popupManager == null)
            {
                // 场景没手动拖引用时自动查找弹窗管理器。
                popupManager = FindObjectOfType<TutorialPopupManager>();
            }

            if (popupManager == null)
            {
                Debug.LogWarning("No TutorialPopupManager was found in the scene.");
                return;
            }

            popupManager.ShowPopup(tutorialTitle, tutorialMessage);
            hasShown = true;

            if (hideAfterUse)
            {
                // 问号提示可以在显示后隐藏，避免玩家以为同一个提示还能重复交互。
                gameObject.SetActive(false);
            }
        }
    }
}
