using UnityEngine;

namespace EchoEscape
{
    // 这个脚本标记一个可能生成宝箱的位置，供 GameManager 随机选择。
    /// <summary>
    /// Marks a possible location where the game manager can spawn a random chest.
    /// </summary>
    /// <remarks>
    /// Attach this empty marker script to scene objects placed in optional reward routes.
    /// EchoEscapeGameManager searches for these markers at scene start and chooses some of them for chest placement.
    /// </remarks>
    public class ChestSpawnPoint : MonoBehaviour
    {
        [SerializeField]
        private bool hideMarkerVisualsOnPlay = true;

        // 这个函数在对象创建时初始化。
        private void Awake()
        {
            if (hideMarkerVisualsOnPlay && Application.isPlaying)
            {
                HideMarkerVisuals();
            }
        }

        // 这个函数隐藏旧版可见生成点标记，避免它盖住真正生成出来的宝箱。
        /// <summary>
        /// Hides legacy visible marker geometry so it does not cover the spawned chest.
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
