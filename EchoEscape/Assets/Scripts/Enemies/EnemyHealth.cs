using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Handles health, damage, and death feedback for a simple enemy.
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        private int currentHealth;
        private bool debugLogs;
        private EnemyAnimationController animationController;
        private EnemyAttack enemyAttack;
        private float deathTimer;

        /// <summary>
        /// True after this enemy has been defeated.
        /// </summary>
        public bool IsDefeated { get; private set; }

        /// <summary>
        /// True while death animation is still playing.
        /// </summary>
        public bool IsPlayingDeathAnimation { get; private set; }

        /// <summary>
        /// Initializes health behavior from serialized SimpleEnemy settings.
        /// </summary>
        public void Configure(int maxHealth, bool logs, EnemyAnimationController enemyAnimation, EnemyAttack attack)
        {
            currentHealth = Mathf.Max(1, maxHealth);
            debugLogs = logs;
            animationController = enemyAnimation;
            enemyAttack = attack;
        }

        /// <summary>
        /// Applies damage to the enemy.
        /// </summary>
        /// <param name="damage">Damage amount.</param>
        public void TakeDamage(int damage)
        {
            if (IsDefeated)
            {
                return;
            }

            currentHealth -= Mathf.Max(1, damage);
            if (currentHealth > 0)
            {
                return;
            }

            Defeat();
        }

        /// <summary>
        /// Advances death animation and deactivates the enemy when it finishes.
        /// </summary>
        /// <returns>True when death animation logic handled this frame.</returns>
        public bool TickDeathAnimation()
        {
            if (!IsPlayingDeathAnimation)
            {
                return false;
            }

            animationController.TickDeath();
            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0f)
            {
                gameObject.SetActive(false);
            }

            return true;
        }

        private void Defeat()
        {
            IsDefeated = true;
            enemyAttack.StopAttack();

            if (debugLogs)
            {
                Debug.Log("Enemy defeated.");
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            if (!animationController.HasDeathAnimation)
            {
                gameObject.SetActive(false);
                return;
            }

            IsPlayingDeathAnimation = true;
            deathTimer = animationController.DeathDuration;
            animationController.PlayDeath();
        }
    }
}
