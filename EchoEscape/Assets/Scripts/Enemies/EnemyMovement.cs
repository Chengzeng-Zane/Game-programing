using System;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Handles simple enemy patrol, chase, and return movement.
    /// </summary>
    public class EnemyMovement : MonoBehaviour
    {
        /// <summary>
        /// Movement decision returned to the enemy coordinator.
        /// </summary>
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
        /// Initializes movement from serialized SimpleEnemy settings.
        /// </summary>
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
        /// Updates enemy movement for one frame.
        /// </summary>
        /// <param name="target">Player target, or null.</param>
        /// <returns>Movement decision for the coordinator.</returns>
        public Decision Tick(Transform target)
        {
            if (chasePlayer && target != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, target.position);
                bool withinLeash = Vector2.Distance(startPosition, target.position) <= leashDistance;

                if (distanceToPlayer <= detectionRange && withinLeash)
                {
                    updateFacing(target.position.x - transform.position.x);

                    if (distanceToPlayer <= attackRange)
                    {
                        return Decision.Attack;
                    }

                    ChasePlayer(target);
                    return Decision.None;
                }
            }

            if (patrol)
            {
                Patrol();
            }
            else
            {
                ReturnTowardStart();
            }

            return Decision.None;
        }

        private void Patrol()
        {
            float offset = Mathf.Sin(Time.time * patrolSpeed) * patrolDistance;
            transform.position = new Vector3(startPosition.x + offset, startPosition.y, startPosition.z);
        }

        private void ChasePlayer(Transform target)
        {
            Vector3 toPlayer = target.position - transform.position;
            Vector3 direction = new Vector3(toPlayer.x, toPlayer.y * verticalChaseStrength, 0f);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.position += direction.normalized * chaseSpeed * Time.deltaTime;
            updateFacing(direction.x);
        }

        private void ReturnTowardStart()
        {
            if (Vector2.Distance(transform.position, startPosition) <= 0.03f)
            {
                transform.position = startPosition;
                return;
            }

            Vector3 direction = startPosition - transform.position;
            transform.position += direction.normalized * returnSpeed * Time.deltaTime;
            updateFacing(direction.x);
        }
    }
}
