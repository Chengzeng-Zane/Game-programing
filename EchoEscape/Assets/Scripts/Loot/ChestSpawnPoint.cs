using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：宝箱出生点标记。它不自己生成宝箱，只告诉 GameManager 哪些位置可以生成宝箱。
    /// 玩法逻辑：关卡开始时 GameManager 会收集所有 ChestSpawnPoint，然后根据 chestsPerRun 随机挑选一部分生成 Chest。标记物本身运行时会隐藏，避免玩家看到占位符。
    /// 协作关系：EchoEscapeGameManager.SpawnRandomChests 读取它。
    /// </summary>
    public class ChestSpawnPoint : MonoBehaviour
    {
        [SerializeField]
        private bool hideMarkerVisualsOnPlay = true;
        /// <summary>
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            if (hideMarkerVisualsOnPlay && Application.isPlaying)
            {
                HideMarkerVisuals();
            }
        }
        /// <summary>
        /// 隐藏对应 UI 或视觉状态，通常在提示结束、关闭弹窗或清理流程时调用。
        /// </summary>
        public void HideMarkerVisuals()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer markerRenderer in renderers)
            {
                markerRenderer.enabled = false;
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D markerCollider in colliders)
            {
                markerCollider.enabled = false;
            }
        }
    }
}
