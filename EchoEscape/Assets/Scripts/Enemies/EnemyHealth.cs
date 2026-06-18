using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Enemy Health Component. It handles health deductions after player attacks hit, death animations, and disabling enemy collisions.
/// Gameplay logic: The current enemies generally only have 1 Point blood, which will be called after the player hits the attack. TakeDamage; Stop attacking and turn off after the blood volume returns to zero. Collider, to prevent dead enemies from continuing to harm the player.
/// Collaborates with: PlayerAttack It is ultimately invoked after hitting an enemy; EnemyAnimationController Play Death; EnemyAttack will be stopped.
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        private int currentHealth;
        private bool debugLogs;
        private EnemyAnimationController animationController;
        private EnemyAttack enemyAttack;
        private float deathTimer;
        public bool IsDefeated { get; private set; }
        public bool IsPlayingDeathAnimation { get; private set; }
        /// <summary>
/// Receive the parameters passed in by the external script and configure the current component to the state required by this scene or this enemy.
        /// </summary>
/// <param name="maxHealth">maxHealth Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="logs">logs Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="enemyAnimation">enemyAnimation Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="attack">attack Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        public void Configure(int maxHealth, bool logs, EnemyAnimationController enemyAnimation, EnemyAttack attack)
        {
            currentHealth = Mathf.Max(1, maxHealth);
            debugLogs = logs;
            animationController = enemyAnimation;
            enemyAttack = attack;
        }
        /// <summary>
/// Receive damage and deduct health points. After the health value reaches zero, the player enters the death or defeat process.
        /// </summary>
/// <param name="damage">The amount of damage received this time. </param>
        public void TakeDamage(int damage)
        {
            if (IsDefeated)
            {
// Dead enemies will no longer have their blood deducted repeatedly to avoid death animations/Disable Collider is executed multiple times.
                return;
            }

// At least deduct 1 Spot blood to prevent incoming 0 Or a negative number causes the attack to have no effect.
            currentHealth -= Mathf.Max(1, damage);
            if (currentHealth > 0)
            {
// If there is still blood, continue to live, and the death animation will not be played for the time being.
                return;
            }

            Defeat();
        }
        /// <summary>
/// Advance enemy death animation. Returns if death animation is playing true, let EnemyController This frame stops other actions.
        /// </summary>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        public bool TickDeathAnimation()
        {
            if (!IsPlayingDeathAnimation)
            {
                return false;
            }

// Enemies no longer move during the death animation/Attacking, only advances the death frame.
            animationController.TickDeath();
            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0f)
            {
// Disable the entire enemy object after the animation is finished to avoid corpses Collider Or the script continues to participate in the game.
                gameObject.SetActive(false);
            }

            return true;
        }
        /// <summary>
/// Handles the aftermath of an enemy being defeated, including stopping attacks, disabling collisions, and playing death animations.
        /// </summary>
        private void Defeat()
        {
            IsDefeated = true;
// Stop attacking immediately when defeated by a player to avoid the instant death attack frame killing the player.
            enemyAttack.StopAttack();

            if (debugLogs)
            {
                Debug.Log("Enemy defeated.");
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
// Disable all Collider, dead enemies will no longer block the path or trigger attack detection.
                colliders[i].enabled = false;
            }

            if (!animationController.HasDeathAnimation)
            {
// When there is no death animation material, the enemy is directly hidden to ensure that the defeat logic is still completed.
                gameObject.SetActive(false);
                return;
            }

// When there is a death animation, give it full playback time, and then TickDeathAnimation Close the object.
            IsPlayingDeathAnimation = true;
            deathTimer = animationController.DeathDuration;
            animationController.PlayDeath();
        }
    }
}
