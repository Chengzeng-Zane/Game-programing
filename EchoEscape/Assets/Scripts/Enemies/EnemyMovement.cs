using System;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：敌人移动组件。它决定敌人是巡逻、追玩家、靠近攻击，还是回到出生点。
    /// 玩法逻辑：敌人只在 detectionRange 内追踪玩家，并受 leashDistance 限制，防止一路追出设计区域；如果没看到玩家，敌人会巡逻或回家。
    /// 协作关系：SimpleEnemy 每帧调用 Tick；EnemyTargeting 提供目标；EnemyAttack 在距离足够近时接管攻击。
    /// </summary>
    public class EnemyMovement : MonoBehaviour
    {
        public enum Decision
        {
            None,
            Attack
        }

        private bool patrol;
        private float patrolSpeed;
        private float patrolDistance;
        private bool chasePlayer;
        private float detectionRange;
        private float attackRange;
        private float chaseSpeed;
        private float returnSpeed;
        private float verticalChaseStrength;
        private float leashDistance;
        private Vector3 startPosition;
        private Action<float> updateFacing;
        /// <summary>
        /// 接收外部脚本传入的参数，把当前组件配置成这个场景或这个敌人需要的状态。
        /// </summary>
        /// <param name="patrolEnabled">patrolEnabled 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="patrolSpeedValue">patrolSpeedValue 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="patrolDistanceValue">patrolDistanceValue 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="chaseEnabled">chaseEnabled 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="detectionRangeValue">detectionRangeValue 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="attackRangeValue">attackRangeValue 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="chaseSpeedValue">chaseSpeedValue 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="returnSpeedValue">returnSpeedValue 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="verticalChaseValue">verticalChaseValue 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="leashDistanceValue">leashDistanceValue 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="homePosition">homePosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="facingCallback">facingCallback 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        public void Configure(
            bool patrolEnabled,
            float patrolSpeedValue,
            float patrolDistanceValue,
            bool chaseEnabled,
            float detectionRangeValue,
            float attackRangeValue,
            float chaseSpeedValue,
            float returnSpeedValue,
            float verticalChaseValue,
            float leashDistanceValue,
            Vector3 homePosition,
            Action<float> facingCallback)
        {
            patrol = patrolEnabled;
            patrolSpeed = patrolSpeedValue;
            patrolDistance = patrolDistanceValue;
            chasePlayer = chaseEnabled;
            detectionRange = detectionRangeValue;
            attackRange = attackRangeValue;
            chaseSpeed = chaseSpeedValue;
            returnSpeed = returnSpeedValue;
            verticalChaseStrength = verticalChaseValue;
            leashDistance = leashDistanceValue;
            startPosition = homePosition;
            updateFacing = facingCallback;
        }
        /// <summary>
        /// 每帧决定敌人的行动：玩家在侦测范围和牵引范围内就追击，足够近就返回 Attack 决策；否则巡逻或回到出生点。
        /// </summary>
        /// <param name="target">目标 Transform 或 GameObject，函数会读取它的位置、组件或状态。</param>
        /// <returns>返回敌人这一帧的移动决策，告诉 SimpleEnemy 后续应该攻击、追击、巡逻还是待机。</returns>
        public Decision Tick(Transform target)
        {
            if (chasePlayer && target != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, target.position);
                bool withinLeash = Vector2.Distance(startPosition, target.position) <= leashDistance;

                if (distanceToPlayer <= detectionRange && withinLeash)
                {
                    // 只有玩家既在感知范围内又没超出 leash，敌人才会追，避免追出关卡设计区域。
                    updateFacing(target.position.x - transform.position.x);

                    if (distanceToPlayer <= attackRange)
                    {
                        // 进入攻击距离后不继续移动，交给 EnemyAttack 做前摇和攻击框检测。
                        return Decision.Attack;
                    }

                    // 还没够到玩家时继续追击。
                    ChasePlayer(target);
                    return Decision.None;
                }
            }

            if (patrol)
            {
                // 没看到玩家时按正弦曲线在出生点附近巡逻。
                Patrol();
            }
            else
            {
                // 不巡逻的敌人会慢慢回到出生点，防止被引走后永久偏离位置。
                ReturnTowardStart();
            }

            return Decision.None;
        }
        /// <summary>
        /// 按正弦曲线在出生点左右移动，让敌人形成简单巡逻路线。
        /// </summary>
        private void Patrol()
        {
            // Sin 的输出在 -1 到 1 之间，乘 patrolDistance 后就是左右最大偏移。
            float offset = Mathf.Sin(Time.time * patrolSpeed) * patrolDistance;
            transform.position = new Vector3(startPosition.x + offset, startPosition.y, startPosition.z);
        }
        /// <summary>
        /// 朝玩家移动。水平追击为主，verticalChaseStrength 只给一点垂直跟随，避免敌人上下乱飞。
        /// </summary>
        /// <param name="target">目标 Transform 或 GameObject，函数会读取它的位置、组件或状态。</param>
        private void ChasePlayer(Transform target)
        {
            Vector3 toPlayer = target.position - transform.position;
            // 垂直方向削弱，敌人主要沿地面追玩家，视觉上更稳定。
            Vector3 direction = new Vector3(toPlayer.x, toPlayer.y * verticalChaseStrength, 0f);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // normalized 保证速度恒定，不会因为玩家距离远就瞬间移动更快。
            transform.position += direction.normalized * chaseSpeed * Time.deltaTime;
            updateFacing(direction.x);
        }
        /// <summary>
        /// 没有玩家目标时回到出生点，恢复关卡初始布置。
        /// </summary>
        private void ReturnTowardStart()
        {
            if (Vector2.Distance(transform.position, startPosition) <= 0.03f)
            {
                // 非常接近出生点时直接对齐，避免因为小数误差来回抖动。
                transform.position = startPosition;
                return;
            }

            Vector3 direction = startPosition - transform.position;
            // 回家速度单独配置，通常比追击慢，让敌人行为更自然。
            transform.position += direction.normalized * returnSpeed * Time.deltaTime;
            updateFacing(direction.x);
        }
    }
}
