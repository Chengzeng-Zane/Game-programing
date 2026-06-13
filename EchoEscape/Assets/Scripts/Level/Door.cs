using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：门或魔法屏障控制脚本。它根据机关状态打开或关闭通路。
    /// 玩法逻辑：打开时禁用 Collider 并隐藏/淡化视觉，让玩家或 Echo 可以通过；关闭时恢复碰撞和显示，重新阻挡路线。
    /// 协作关系：PressurePlate 或关卡流程调用 OpenDoor、CloseDoor、SetOpen。
    /// </summary>
    public class Door : MonoBehaviour
    {
        public Color closedColor = new Color(0.85f, 0.18f, 0.14f);
        public Color openColor = new Color(0.12f, 0.75f, 0.32f);
        public bool IsOpen => isOpen;

        private Collider2D doorCollider;
        private bool isOpen;
        /// <summary>
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            doorCollider = GetComponent<Collider2D>();
            SetOpen(false);
        }
        /// <summary>
        /// 打开门、宝箱或通路，让玩家可以继续前进或获得奖励。
        /// </summary>
        public void OpenDoor()
        {
            SetOpen(true);
        }
        /// <summary>
        /// 关闭门、面板或通路，恢复阻挡或隐藏状态。
        /// </summary>
        public void CloseDoor()
        {
            SetOpen(false);
        }
        /// <summary>
        /// 设置门的开关状态。打开时禁用碰撞并变成打开颜色；关闭时恢复碰撞并变成关闭颜色。
        /// </summary>
        /// <param name="open">true 表示打开，false 表示关闭。</param>
        public void SetOpen(bool open)
        {
            bool changed = isOpen != open;
            isOpen = open;

            if (doorCollider != null)
            {
                // 门打开时禁用 Collider，玩家和 Echo 才能真正穿过去；关闭时恢复阻挡。
                doorCollider.enabled = !isOpen;
            }

            // PrototypeFactory.Tint 负责改 SpriteRenderer/Renderer 颜色，作为开关状态的快速视觉反馈。
            PrototypeFactory.Tint(gameObject, isOpen ? openColor : closedColor);

            if (changed)
            {
                // 只有状态真的变化时才写 HUD 和日志，避免压力板每帧刷新刷屏。
                string message = isOpen ? "Door opened" : "Door closed";
                EchoEscapeGameManager.Instance?.UpdateStatus(message);
                Debug.Log(message);
            }
        }
    }
}
