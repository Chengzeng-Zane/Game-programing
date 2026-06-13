using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：相机跟随脚本。它让主相机跟随玩家移动，保证玩家在横版关卡中一直处于可见范围。
    /// 玩法逻辑：相机不直接瞬移到玩家身上，而是用 SmoothDamp 平滑靠近目标位置，这样移动和跳跃时画面不会太抖。
    /// 协作关系：挂在 Main Camera 上，target 指向 Player；只改相机位置，不影响玩家速度、碰撞或输入。
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 1.25f, -10f);
        public float followSpeed = 6f;
        /// <summary>
        /// Unity 在 Update 之后调用。这里常用于相机或视觉同步，确保读到的是本帧最终状态。
        /// </summary>
        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
        }
    }
}
