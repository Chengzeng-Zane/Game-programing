using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：压力板机关脚本。玩家或 Echo 站上去会按下按钮，常用于开门或解除魔法屏障。
    /// 玩法逻辑：脚本维护一个压在板上的 Collider 列表，只要列表里还有有效玩家或 Echo，按钮就是 pressed；按下后会更新门、按钮动画和颜色反馈。Echo 回放结束后停在压力板上，就能替玩家持续开门。
    /// 协作关系：ActionRecorder/EchoReplayController 生成 Echo；Door 接收压力板开关状态。
    /// </summary>
    public class PressurePlate : MonoBehaviour
    {
        public Door linkedDoor;
        public bool enableDebugLogs = true;
        [SerializeField] private bool useLayeredVisualColors;
        [SerializeField] private Transform layeredVisualRoot;
        [SerializeField] private float pressedVisualBoost = 1.35f;
        [SerializeField] private Animator buttonVisualAnimator;
        [SerializeField] private Vector3 pressedLocalOffset = new Vector3(0f, -0.05f, 0f);
        public bool IsPressed => occupants.Count > 0;

        private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
        private readonly Dictionary<SpriteRenderer, Color> layeredVisualBaseColors = new Dictionary<SpriteRenderer, Color>();
        private Transform pressedMotionRoot;
        private Vector3 restingPressedMotionLocalPosition;
        /// <summary>
        /// 初始化压力板组件和初始视觉状态，保证一开始门和按钮状态一致。
        /// </summary>
        private void Awake()
        {
            // 先找到可移动的按钮视觉根节点，后面按下/松开时只移动视觉，不动真实 Collider。
            pressedMotionRoot = ResolvePressedMotionRoot();
            if (pressedMotionRoot != null)
            {
                restingPressedMotionLocalPosition = pressedMotionRoot.localPosition;
            }

            // 缓存原始颜色，按下变亮后才能准确恢复原色。
            CacheLayeredVisualColors();
            Refresh();
        }
        /// <summary>
        /// 玩家或 Echo 进入压力板时加入占用列表，并刷新按钮和门状态。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!CanPress(other))
            {
                return;
            }

            occupants.Add(other);
            LogOccupant(other);
            Refresh();
        }
        /// <summary>
        /// 玩家或 Echo 离开压力板时从占用列表移除，并重新判断是否还有对象压住。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!CanPress(other))
            {
                return;
            }

            occupants.Remove(other);
            Refresh();
        }
        /// <summary>
        /// 判断一个 Collider 是否可以按下压力板。通常 Player 和 Echo 可以，普通道具或敌人不会触发。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool CanPress(Collider2D other)
        {
            // Player 和 Echo 都可以压机关；Echo 是解谜用途，所以不能只判断 Player。
            return HasTag(other, "Player") ||
                HasTag(other, "Echo") ||
                other.GetComponent<PlayerController2D>() != null ||
                other.GetComponentInParent<PlayerController2D>() != null ||
                other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null;
        }
        /// <summary>
        /// 根据当前占用列表计算按钮是否按下，并同步门、动画、颜色和按钮位置。
        /// </summary>
        private void Refresh()
        {
            // Echo 或玩家被销毁时，HashSet 里可能留下空引用；刷新前先清理。
            occupants.RemoveWhere(occupant => occupant == null);

            bool pressed = occupants.Count > 0;
            // pressed 状态同时驱动视觉反馈和真实 Door 逻辑。
            ApplyPressedMotion(pressed);
            UpdateButtonVisualAnimator(pressed);

            if (useLayeredVisualColors)
            {
                ApplyLayeredVisualColors(pressed);
            }
            else if (buttonVisualAnimator == null)
            {
                PrototypeFactory.Tint(gameObject, pressed ? new Color(0.15f, 0.9f, 0.45f) : new Color(1f, 0.85f, 0.15f));
            }

            if (linkedDoor != null)
            {
                if (pressed)
                {
                    // 只要还有任意有效对象压住，门就保持打开。
                    linkedDoor.OpenDoor();
                }
                else
                {
                    // 所有对象离开后门关闭，形成需要 Echo 持续压住的谜题。
                    linkedDoor.CloseDoor();
                }
            }
        }
        /// <summary>
        /// 找到需要做下沉动画的按钮视觉根节点。
        /// </summary>
        /// <returns>返回找到的 Transform；找不到时可能返回 null。</returns>
        private Transform ResolvePressedMotionRoot()
        {
            if (buttonVisualAnimator != null)
            {
                return buttonVisualAnimator.transform;
            }

            if (layeredVisualRoot != null && layeredVisualRoot != transform)
            {
                return layeredVisualRoot;
            }

            return null;
        }
        /// <summary>
        /// 根据 pressed 状态移动按钮视觉，让压力板看起来真的被压下。
        /// </summary>
        /// <param name="pressed">true 表示压力板被按下，false 表示松开。</param>
        private void ApplyPressedMotion(bool pressed)
        {
            if (pressedMotionRoot == null)
            {
                return;
            }

            pressedMotionRoot.localPosition = restingPressedMotionLocalPosition + (pressed ? pressedLocalOffset : Vector3.zero);
        }
        /// <summary>
        /// 把 pressed 状态传给按钮 Animator，用于播放按钮按下或松开的动画。
        /// </summary>
        /// <param name="pressed">true 表示压力板被按下，false 表示松开。</param>
        private void UpdateButtonVisualAnimator(bool pressed)
        {
            if (buttonVisualAnimator == null ||
                !buttonVisualAnimator.isActiveAndEnabled ||
                !buttonVisualAnimator.gameObject.activeInHierarchy)
            {
                // Animator 物体未激活时直接调用会有 Unity 警告，所以先过滤。
                return;
            }

            buttonVisualAnimator.SetBool("Pressed", pressed);
            buttonVisualAnimator.Play(pressed ? "Pressed" : "Idle");
        }
        /// <summary>
        /// 缓存按钮多层 SpriteRenderer 的原始颜色，方便按下时变亮、松开时恢复。
        /// </summary>
        private void CacheLayeredVisualColors()
        {
            if (!useLayeredVisualColors)
            {
                return;
            }

            Transform root = layeredVisualRoot != null ? layeredVisualRoot : transform;
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                layeredVisualBaseColors[renderer] = renderer.color;
            }
        }
        /// <summary>
        /// 根据按钮是否按下，给多层 SpriteRenderer 应用不同颜色反馈。
        /// </summary>
        /// <param name="pressed">true 表示压力板被按下，false 表示松开。</param>
        private void ApplyLayeredVisualColors(bool pressed)
        {
            if (layeredVisualBaseColors.Count == 0)
            {
                CacheLayeredVisualColors();
            }

            foreach (KeyValuePair<SpriteRenderer, Color> entry in layeredVisualBaseColors)
            {
                SpriteRenderer renderer = entry.Key;
                if (renderer == null)
                {
                    continue;
                }

                renderer.color = pressed ? BoostPressedColor(entry.Value) : entry.Value;
            }
        }
        /// <summary>
        /// 把原始颜色调亮，用作按钮被按下时的视觉反馈。
        /// </summary>
        /// <param name="baseColor">baseColor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回处理后的颜色值。</returns>
        private Color BoostPressedColor(Color baseColor)
        {
            float red = Mathf.Min(1f, baseColor.r * 1.08f + 0.03f);
            float green = Mathf.Min(1f, baseColor.g * pressedVisualBoost + 0.08f);
            float blue = Mathf.Min(1f, baseColor.b * pressedVisualBoost + 0.05f);
            return new Color(red, green, blue, baseColor.a);
        }
        /// <summary>
        /// 输出进入压力板的对象信息，方便调试到底是谁压住了按钮。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        private void LogOccupant(Collider2D other)
        {
            if (!enableDebugLogs)
            {
                return;
            }

            string occupantName = IsEcho(other) ? "Echo" : "Player";
            Debug.Log($"PressurePlate pressed by {occupantName}");
        }
        /// <summary>
        /// 判断 Collider 是否来自 Echo 回放体。Echo 可以压机关，但不能被当作玩家死亡或通关。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool IsEcho(Collider2D other)
        {
            return HasTag(other, "Echo") ||
                other.GetComponent<EchoReplayController>() != null ||
                other.GetComponentInParent<EchoReplayController>() != null;
        }
        /// <summary>
        /// 安全检查 Collider 或根对象 tag，避免 tag 不存在导致异常。
        /// </summary>
        /// <param name="other">Unity 传入的 2D Collider，表示进入触发器或被检测到的对象。函数会用它判断对象是不是玩家、Echo、敌人或机关。</param>
        /// <param name="tagName">tagName 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool HasTag(Collider2D other, string tagName)
        {
            try
            {
                return other.CompareTag(tagName) || other.transform.root.CompareTag(tagName);
            }
            catch (UnityException)
            {
                return false;
            }
        }
    }
}
