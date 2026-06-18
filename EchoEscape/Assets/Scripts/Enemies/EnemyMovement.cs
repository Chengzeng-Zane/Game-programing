using System;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Enemy Movement Component. It determines whether the enemy patrols, chases the player, attacks in close proximity, or returns to the spawn point.
/// Gameplay logic: The enemy is only detectionRange Track players internally and be subject to leashDistance Limit to prevent chasing all the way out of the design area; if the player is not seen, the enemy will patrol or go home.
/// Collaborates with: EnemyController Called every frame Tick；EnemyTargeting provide goals; EnemyAttack Take over the attack when close enough.
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
/// Receive the parameters passed in by the external script and configure the current component to the state required by this scene or this enemy.
        /// </summary>
/// <param name="patrolEnabled">patrolEnabled Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="patrolSpeedValue">patrolSpeedValue Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="patrolDistanceValue">patrolDistanceValue Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="chaseEnabled">chaseEnabled Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="detectionRangeValue">detectionRangeValue Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="attackRangeValue">attackRangeValue Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="chaseSpeedValue">chaseSpeedValue Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="returnSpeedValue">returnSpeedValue Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="verticalChaseValue">verticalChaseValue Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="leashDistanceValue">leashDistanceValue Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="homePosition">homePosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="facingCallback">facingCallback Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
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
/// Each frame determines the enemy's action: the player pursues within the detection range and pulling range, and returns when close enough. Attack Decision; otherwise patrol or return to spawn point.
        /// </summary>
/// <param name="target">Target Transform or GameObject, the function reads its position, component, or state. </param>
/// <returns>Returns the enemy's movement decision for this frame, telling EnemyController Should the follow-up be attack, pursuit, patrol or standby. </returns>
        public Decision Tick(Transform target)
        {
            if (chasePlayer && target != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, target.position);
                bool withinLeash = Vector2.Distance(startPosition, target.position) <= leashDistance;

                if (distanceToPlayer <= detectionRange && withinLeash)
                {
// Only the player is within the sensing range but not beyond it leash, the enemy will chase you and avoid chasing you out of the level design area.
                    updateFacing(target.position.x - transform.position.x);

                    if (distanceToPlayer <= attackRange)
                    {
// Do not continue to move after entering attack range, hand over to EnemyAttack Do forward swing and attack frame detection.
                        return Decision.Attack;
                    }

// Continue to pursue the player before it reaches it.
                    ChasePlayer(target);
                    return Decision.None;
                }
            }

            if (patrol)
            {
// When no player is seen, press the sine curve to patrol near the spawn point.
                Patrol();
            }
            else
            {
// Enemies not patrolling will slowly return to their spawn point to prevent them from permanently deviating from their position after being lured away.
                ReturnTowardStart();
            }

            return Decision.None;
        }
        /// <summary>
/// Move left and right around the spawn point according to a sinusoidal curve, allowing the enemy to form a simple patrol route.
        /// </summary>
        private void Patrol()
        {
// Sin The output of -1 arrive 1 between, multiply patrolDistance The last is the maximum left and right offset.
            float offset = Mathf.Sin(Time.time * patrolSpeed) * patrolDistance;
            transform.position = new Vector3(startPosition.x + offset, startPosition.y, startPosition.z);
        }
        /// <summary>
/// Moves towards the player. Mainly horizontal pursuit, verticalChaseStrength Only give a little vertical follow-up to prevent enemies from flying up and down.
        /// </summary>
/// <param name="target">Target Transform or GameObject, the function reads its position, component, or state. </param>
        private void ChasePlayer(Transform target)
        {
            Vector3 toPlayer = target.position - transform.position;
// The vertical direction is weakened. Enemies mainly chase players along the ground, making it visually more stable.
            Vector3 direction = new Vector3(toPlayer.x, toPlayer.y * verticalChaseStrength, 0f);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

// normalized The speed is guaranteed to be constant, and the player will not move faster instantly just because the player is far away.
            transform.position += direction.normalized * chaseSpeed * Time.deltaTime;
            updateFacing(direction.x);
        }
        /// <summary>
/// When there is no player target, return to the spawn point and restore the initial layout of the level.
        /// </summary>
        private void ReturnTowardStart()
        {
            if (Vector2.Distance(transform.position, startPosition) <= 0.03f)
            {
// Align directly when very close to the spawn point to avoid jittering back and forth due to decimal errors.
                transform.position = startPosition;
                return;
            }

            Vector3 direction = startPosition - transform.position;
// Home speed is configured separately and is generally slower than pursuit, allowing enemies to behave more naturally.
            transform.position += direction.normalized * returnSpeed * Time.deltaTime;
            updateFacing(direction.x);
        }
    }
}
