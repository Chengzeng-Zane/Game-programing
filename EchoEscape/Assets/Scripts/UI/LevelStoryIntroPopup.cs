using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：简单关卡故事弹窗脚本。它用于进入关卡后延迟显示一段故事文本。
    /// 玩法逻辑：Start 后等待一小段时间，再找到或创建 TutorialPopupManager，然后显示配置好的标题和内容。
    /// 协作关系：和 TutorialPopupManager 配合。当前更完整的多页介绍主要由 LevelIntroSequence 负责。
    /// </summary>
    public class LevelStoryIntroPopup : MonoBehaviour
    {
        [SerializeField] private TutorialPopupManager popupManager;
        [SerializeField] private string storyTitle = "Echo Wizard";
        [SerializeField] [TextArea(3, 8)] private string storyMessage;
        [SerializeField] private float delayBeforeShow = 0.3f;

        private bool hasShown;
        /// <summary>
        /// Unity 在第一帧前调用。这里通常连接场景对象，启动初始 UI、教程或关卡流程。
        /// </summary>
        private void Start()
        {
            // 延迟一点显示，让场景和 UI 先初始化完成。
            StartCoroutine(ShowStoryAfterDelay());
        }
        /// <summary>
        /// 显示对应 UI 或视觉状态，通常用于弹窗、loot 提示、死亡提示或结算反馈。
        /// </summary>
        /// <returns>返回 Unity 协程，调用方会用 StartCoroutine 让这个流程分帧执行。</returns>
        private IEnumerator ShowStoryAfterDelay()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delayBeforeShow));

            if (hasShown)
            {
                // 防止协程被重复启动时同一个故事弹出多次。
                yield break;
            }

            hasShown = true;
            TutorialPopupManager manager = ResolvePopupManager();
            if (manager != null)
            {
                // 具体暂停、显示和关闭逻辑交给 TutorialPopupManager。
                manager.ShowPopup(storyTitle, storyMessage);
            }
        }
        /// <summary>
        /// 找到可用的 TutorialPopupManager。Inspector 没配置时先在场景里找，找不到再运行时创建。
        /// </summary>
        /// <returns>返回 TutorialPopupManager 类型结果，供调用方继续判断或使用。</returns>
        private TutorialPopupManager ResolvePopupManager()
        {
            if (popupManager != null)
            {
                // 优先使用 Inspector 指定的弹窗管理器。
                return popupManager;
            }

            popupManager = FindObjectOfType<TutorialPopupManager>();
            if (popupManager != null)
            {
                // 场景已有弹窗管理器时复用，避免重复 Canvas。
                return popupManager;
            }

            // 没有任何弹窗 UI 时创建一个最小可用版本。
            popupManager = CreateRuntimePopupManager();
            return popupManager;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <returns>返回 TutorialPopupManager 类型结果，供调用方继续判断或使用。</returns>
        private static TutorialPopupManager CreateRuntimePopupManager()
        {
            GameObject canvasObject = new GameObject("TutorialPopupUI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject panelObject = new GameObject("TutorialPopupPanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(740f, 380f);
            panelObject.AddComponent<CanvasRenderer>();
            panelObject.AddComponent<Image>();

            Text titleText = CreateText("TitleText", panelObject.transform);
            Text bodyText = CreateText("BodyText", panelObject.transform);
            CreateText("CloseHintText", panelObject.transform);

            TutorialPopupManager manager = canvasObject.AddComponent<TutorialPopupManager>();
            manager.popupPanel = panelObject;
            manager.titleText = titleText;
            manager.bodyText = bodyText;
            manager.pauseGameWhenOpen = true;
            return manager;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="objectName">要创建或查找的 GameObject 名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <returns>返回创建或找到的 UI Text 组件。</returns>
        private static Text CreateText(string objectName, Transform parent)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            textObject.AddComponent<RectTransform>();
            textObject.AddComponent<CanvasRenderer>();
            Text text = textObject.AddComponent<Text>();
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }
    }
}
