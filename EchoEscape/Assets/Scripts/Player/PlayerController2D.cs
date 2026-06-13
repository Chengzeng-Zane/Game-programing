using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：玩家基础控制脚本。它负责左右移动、跳跃、朝向、落地检测、重生和开宝箱交互，是玩家操作的核心。
    /// 玩法逻辑：Update 读取水平输入、跳跃键、Q/E Echo 录制回放键和 F 开箱键；FixedUpdate 把水平输入写入 Rigidbody2D.velocity；OnCollisionStay2D 根据碰撞法线判断是否站在当前重力方向的地面上。
    /// 协作关系：ActionRecorder 录制玩家位置；GravityFlipController 改变重力方向；PlayerAnimationController 读取落地和朝向；CameraFollow 跟随玩家；Chest 通过 TryOpenChest 被打开。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerController2D : MonoBehaviour
    {
        public float moveSpeed = 6f;
        public float jumpForce = 10f;
        public float interactRadius = 0.75f;

        [SerializeField]
        private float chestFacingTolerance = 0.18f;

        [SerializeField]
        private float chestVerticalTolerance = 0.75f;
        public bool FacingRight { get; private set; } = true;

        private Rigidbody2D body;
        private ActionRecorder recorder;
        private float moveInput;
        private float groundedUntil;
        /// <summary>
        /// 初始化玩家移动组件。这里缓存 Rigidbody2D，让后续 FixedUpdate 可以直接改速度；同时缓存 ActionRecorder，让 Q/E 录制回放输入可以调用录制系统。
        /// </summary>
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            recorder = GetComponent<ActionRecorder>();
        }
        /// <summary>
        /// 读取玩家这一帧的操作输入。Horizontal 用于左右移动，Space 用于按当前重力方向跳跃，Q/E 分别控制 Echo 录制和回放，F 用于打开附近宝箱。这里不直接做水平物理移动，避免和 FixedUpdate 的物理步不同步。
        /// </summary>
        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                // 剧情介绍、教程弹窗或死亡 UI 会暂停时间；这里清掉输入，避免恢复游戏时玩家沿用上一帧方向继续滑动。
                moveInput = 0f;
                return;
            }

            // Horizontal 来自 Unity 输入轴，A/D 和左右方向键都能控制这个值。
            moveInput = Input.GetAxisRaw("Horizontal");

            if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
            {
                // 跳跃方向跟当前重力相反，所以正常重力向上跳，反重力时会向下方平台跳。
                Vector2 jumpVelocity = -GravityDirection * jumpForce;
                body.velocity = new Vector2(body.velocity.x, jumpVelocity.y);
                EchoEscapeGameManager.Instance?.AudioService?.PlayJump();
            }

            if (Input.GetKeyDown(KeyCode.Q) && recorder != null)
            {
                // 录制逻辑交给 ActionRecorder；玩家控制脚本只负责把按键转发过去。
                recorder.ToggleRecording();
            }

            if (Input.GetKeyDown(KeyCode.E) && recorder != null)
            {
                // Echo 回放会复现刚才录制的路线，用来压按钮或配合机关。
                recorder.PlayEcho();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                // F 只尝试打开附近且面向正确的宝箱，不会直接修改 loot 数据。
                TryOpenChest();
            }
        }
        /// <summary>
        /// 把 Update 里记录的水平输入应用到 Rigidbody2D.velocity。这样玩家左右移动由物理系统驱动，同时根据输入正负更新 FacingRight，供动画和攻击方向使用。
        /// </summary>
        private void FixedUpdate()
        {
            // 水平速度在 FixedUpdate 写入 Rigidbody，保证移动和 Unity 物理步长一致。
            body.velocity = new Vector2(moveInput * moveSpeed, body.velocity.y);

            if (moveInput > 0.05f)
            {
                FacingRight = true;
            }
            else if (moveInput < -0.05f)
            {
                FacingRight = false;
            }
        }
        /// <summary>
        /// 持续检查玩家是否接触可站立表面。它会比较碰撞法线和当前重力反方向，所以正常重力和反重力都能正确判断“脚下”是哪一边。
        /// </summary>
        /// <param name="collision">Unity 传入的碰撞信息，里面包含接触点和法线，用来判断玩家是否站在地面或平台上。</param>
        private void OnCollisionStay2D(Collision2D collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                // 碰撞法线和“重力反方向”越接近，说明玩家越可能踩在当前重力方向对应的地面上。
                if (Vector2.Dot(collision.GetContact(i).normal, -GravityDirection) > 0.45f)
                {
                    // 给落地状态保留 0.12 秒缓冲，减少跳跃、动画和重力翻转检测的边缘抖动。
                    groundedUntil = Time.time + 0.12f;
                    return;
                }
            }
        }
        /// <summary>
        /// 把玩家放回指定重生点，并清空速度。重生时还会重置 GravityFlipController，保证玩家不会以反重力或旋转状态继续死亡循环。
        /// </summary>
        /// <param name="position">目标世界坐标，常用于重生、生成对象或记录 Echo 帧。</param>
        public void Respawn(Vector3 position)
        {
            transform.position = position;
            body.velocity = Vector2.zero;
            GravityFlipController gravityFlip = GetComponent<GravityFlipController>();
            if (gravityFlip != null)
            {
                // 死亡重生必须恢复正常重力，否则玩家可能倒挂着出生并立刻再次掉入死亡区。
                gravityFlip.ResetGravityState();
            }
            else
            {
                // 没挂 GravityFlipController 的测试场景也要至少恢复普通 gravityScale 和旋转。
                body.gravityScale = Mathf.Abs(body.gravityScale);
                transform.rotation = Quaternion.identity;
            }
        }
        public Vector2 GravityDirection => body != null && body.gravityScale < 0f ? Vector2.up : Vector2.down;
        /// <summary>
        /// 返回玩家最近是否接触过地面。使用 groundedUntil 做一个很短的缓冲，让跳跃和动画判断不会因为物理帧间隔产生抖动。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        public bool IsGrounded()
        {
            return Time.time <= groundedUntil;
        }
        /// <summary>
        /// 玩家按 F 时调用。它会搜索交互半径内所有宝箱，过滤掉不能交互的宝箱，然后打开距离最近的一个。
        /// </summary>
        private void TryOpenChest()
        {
            // 用圆形范围搜索，能兼容宝箱 Collider 或视觉大小不同的情况。
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius);
            Chest nearestChest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                Chest chest = hit.GetComponent<Chest>();
                if (chest == null || chest.IsOpened || chest.IsOpening || !CanInteractWithChest(chest))
                {
                    // 跳过非宝箱、已打开宝箱、正在播放开箱动画的宝箱，以及玩家没面向的宝箱。
                    continue;
                }

                float distance = Vector2.Distance(transform.position, chest.transform.position);
                if (distance < nearestDistance)
                {
                    // 如果范围里有多个宝箱，只打开最近的一个，避免一次按键领取多个奖励。
                    nearestDistance = distance;
                    nearestChest = chest;
                }
            }

            if (nearestChest != null)
            {
                nearestChest.Open();
            }
        }
        /// <summary>
        /// 判断玩家是否真的能打开某个宝箱。它会检查宝箱是否存在、是否已经打开、垂直距离是否合理，以及玩家是否面向宝箱方向。
        /// </summary>
        /// <param name="chest">chest 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool CanInteractWithChest(Chest chest)
        {
            Vector2 toChest = chest.transform.position - transform.position;
            if (Mathf.Abs(toChest.y) > chestVerticalTolerance)
            {
                // 垂直距离太大时不允许开箱，避免玩家隔着上下平台误触发。
                return false;
            }

            if (Mathf.Abs(toChest.x) <= chestFacingTolerance)
            {
                // 宝箱几乎在玩家正中间时，不强制要求朝向，避免贴近宝箱时手感太严格。
                return true;
            }

            // 宝箱在玩家左边就必须面朝左，宝箱在右边就必须面朝右。
            return FacingRight ? toChest.x > 0f : toChest.x < 0f;
        }
    }
}
