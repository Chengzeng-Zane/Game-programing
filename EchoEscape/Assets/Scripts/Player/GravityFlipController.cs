using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：玩家重力翻转控制器。它让玩家可以从地面翻到上方平台，形成 Echo Escape 的核心空间解谜机制。
    /// 玩法逻辑：玩家按上方向键时，脚本先向上检测有没有可站平台；找到平台后才把 Rigidbody2D.gravityScale 变成负数，并把角色旋转到倒挂状态。按下方向键时恢复正常重力。为了避免卡墙，翻转后会把玩家吸附到平台表面。
    /// 协作关系：PlayerController2D 继续负责水平移动和跳跃；PlayerAnimationController 读取 gravityScale 显示动画参数；GravityFlipVoidKillZone 读取 IsFlipped 处理反重力掉出死亡。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerController2D))]
    public class GravityFlipController : MonoBehaviour
    {
        [SerializeField] private float gravityScale = 2.4f;
        [SerializeField] private float flipCheckDistance = 3.25f;
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private bool debugLogs = true;

        private const float SnapSkin = 0.03f;

        private Rigidbody2D body;
        private Collider2D playerCollider;
        private PlayerController2D playerController;
        private bool isFlipped;
        public bool IsFlipped => isFlipped;
        /// <summary>
        /// 恢复正常重力状态。死亡重生或初始化时调用，防止玩家保留倒挂旋转和负 gravityScale。
        /// </summary>
        public void ResetGravityState()
        {
            SetGravity(false);
        }
        /// <summary>
        /// 缓存 Rigidbody2D、玩家 Collider 和 PlayerController2D，并读取当前 gravityScale 作为基础重力强度。初始化时强制恢复正常重力。
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            playerCollider = GetComponent<Collider2D>();
            playerController = GetComponent<PlayerController2D>();

            if (Mathf.Abs(body.gravityScale) > 0.01f)
            {
                // 使用场景里 Rigidbody 原本的重力强度，避免脚本默认值覆盖 Inspector 调好的手感。
                gravityScale = Mathf.Abs(body.gravityScale);
            }

            // 开局强制正常重力，防止编辑器里残留的倒挂状态影响出生。
            SetGravity(false);
        }
        /// <summary>
        /// 监听上下方向键。按上键尝试翻到上方平台，按下键尝试恢复到下方平台；真正能不能翻由 TrySetGravity 决定。
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                // 上方向键只表示“尝试翻到上方平台”，真正能不能翻还要看上方有没有可站平台。
                TrySetGravity(flipped: true, Vector2.up);
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                // 下方向键恢复正常重力，同样要求下方有平台，避免玩家在空中随意切换。
                TrySetGravity(flipped: false, Vector2.down);
            }
        }
        /// <summary>
        /// 尝试切换重力方向。它必须先确认玩家当前落地，并且目标方向存在可站立平台；否则不翻转，避免玩家在空中乱翻或翻到没有平台的地方。
        /// </summary>
        /// <param name="flipped">是否切换到反重力状态。true 表示倒挂，false 表示恢复正常重力。</param>
        /// <param name="checkDirection">检测平台的方向。向上表示找天花板平台，向下表示找地面平台。</param>
        private void TrySetGravity(bool flipped, Vector2 checkDirection)
        {
            if (isFlipped == flipped || !playerController.IsGrounded())
            {
                // 已经在目标状态，或玩家没站稳时不允许翻转，避免空中连翻破坏关卡设计。
                return;
            }

            if (!TryFindStandablePlatform(checkDirection, out RaycastHit2D platformHit))
            {
                // 目标方向没有 Ground/Platform 时不翻转，防止玩家翻到虚空里。
                return;
            }

            SetGravity(flipped);
            // 翻转后立即贴到目标平台表面，否则 Collider 可能卡进平台或悬空。
            SnapToPlatform(platformHit, checkDirection);
            LogDebug(flipped ? "Gravity flipped." : "Gravity restored.");
        }
        /// <summary>
        /// 真正修改重力状态。它更新内部 isFlipped，改变 Rigidbody2D.gravityScale，清空竖直速度，并把玩家旋转到正常或倒挂朝向。
        /// </summary>
        /// <param name="flipped">是否切换到反重力状态。true 表示倒挂，false 表示恢复正常重力。</param>
        private void SetGravity(bool flipped)
        {
            isFlipped = flipped;
            body.gravityScale = flipped ? -gravityScale : gravityScale;
            // 切换瞬间清掉竖直速度，避免玩家带着原来的下落速度穿过新平台。
            body.velocity = new Vector2(body.velocity.x, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, flipped ? 180f : 0f);
        }
        /// <summary>
        /// 向目标方向发射 Raycast，寻找最近的可站立平台。这个函数负责把“上方有没有平台”这个问题转换成物理检测结果。
        /// </summary>
        /// <param name="direction">方向向量，用于射线检测、移动或朝向判断。</param>
        /// <param name="platformHit">输出参数，返回检测到的可站立平台信息。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool TryFindStandablePlatform(Vector2 direction, out RaycastHit2D platformHit)
        {
            platformHit = default;

            // RaycastAll 可能命中多个 Collider，所以后面要挑最近的可站平台。
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, flipCheckDistance, groundLayer);
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit2D hit = hits[i];
                if (hit.collider == null || !IsStandablePlatform(hit.collider))
                {
                    // 触发器、玩家自己、危险区和非平台物体都不能作为翻转落点。
                    continue;
                }

                if (hit.distance < bestDistance)
                {
                    // 选择最近的平台，让翻转目标符合玩家视觉上看到的那个平台。
                    bestDistance = hit.distance;
                    platformHit = hit;
                }
            }

            return platformHit.collider != null;
        }
        /// <summary>
        /// 过滤 Raycast 命中的对象。触发器、玩家自己的 Collider、危险区或非平台对象都不能作为重力翻转落点。
        /// </summary>
        /// <param name="hitCollider">射线或碰撞检测命中的 Collider，用来判断它能不能作为平台。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool IsStandablePlatform(Collider2D hitCollider)
        {
            if (hitCollider == null || hitCollider.isTrigger)
            {
                // Trigger 通常是死亡区、传送门或提示区，不能当作地面。
                return false;
            }

            if (hitCollider.attachedRigidbody == body || hitCollider.transform.IsChildOf(transform))
            {
                // 不能把玩家自己的 Collider 当成平台。
                return false;
            }

            // 这里用名字和 Tag 双保险，因为关卡里的平台对象命名不完全一致。
            string objectName = hitCollider.gameObject.name;
            string objectTag = hitCollider.gameObject.tag;
            return objectName.Contains("Ground")
                || objectName.Contains("Platform")
                || objectTag == "Ground"
                || objectTag == "Platform";
        }
        /// <summary>
        /// 翻转成功后把玩家贴到平台表面。这样角色不会因为 Collider 尺寸和射线距离误差卡进平台或悬在空中。
        /// </summary>
        /// <param name="platformHit">输出参数，返回检测到的可站立平台信息。</param>
        /// <param name="direction">方向向量，用于射线检测、移动或朝向判断。</param>
        private void SnapToPlatform(RaycastHit2D platformHit, Vector2 direction)
        {
            if (playerCollider == null)
            {
                return;
            }

            Physics2D.SyncTransforms();
            Bounds bounds = playerCollider.bounds;
            Vector2 targetPosition = body.position;

            if (direction.y > 0f)
            {
                // 向上翻转时，玩家头顶要贴近平台下表面，所以用 Collider 顶部偏移计算目标 y。
                float topOffset = bounds.max.y - transform.position.y;
                targetPosition.y = platformHit.point.y - topOffset - SnapSkin;
            }
            else
            {
                // 恢复正常重力时，玩家脚底要站到平台上表面，所以用 Collider 底部偏移计算目标 y。
                float bottomOffset = transform.position.y - bounds.min.y;
                targetPosition.y = platformHit.point.y + bottomOffset + SnapSkin;
            }

            body.position = targetPosition;
            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        }
        /// <summary>
        /// 在 debugLogs 打开时输出重力翻转相关日志，方便测试为什么某次翻转成功或失败。
        /// </summary>
        /// <param name="message">要显示到 HUD 或写入日志的文字。</param>
        private void LogDebug(string message)
        {
            if (debugLogs)
            {
                Debug.Log(message);
            }
        }
    }
}
