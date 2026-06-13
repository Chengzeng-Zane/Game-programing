using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：战利品和死亡反馈 UI。它负责告诉玩家获得了什么、当前还没带走什么、已经安全结算什么、死亡时丢失什么。
    /// 玩法逻辑：开宝箱或拾取后显示获得提示并加入 pending；到达出口后显示 secured；死亡时显示 lost loot。它还会维护左上角 HUD，让玩家随时知道自己的 loot 风险。
    /// 协作关系：EchoEscapeGameManager 在获得、死亡、通关时调用；GoalZone 第三关结尾会等待 loot 提示先出现。
    /// </summary>
    public class LootFeedbackUI : MonoBehaviour
    {
        private const string Level2SceneName = "Level2_LootTutorial";
        private const string Level3SceneName = "Level3_RiskReward";
        private const string Level1SceneName = "Level1_Tutorial";
        private const string PopupFontResourcePath = "BrackeysPlatformer/Fonts/PixelOperator8-Bold";

        private static readonly Color OrnatePanelColor = new Color(0.005f, 0.025f, 0.02f, 0.96f);
        private static readonly Color OrnateGoldColor = new Color(0.95f, 0.72f, 0.25f, 1f);
        private static readonly Color OrnateGreenColor = new Color(0.2f, 0.75f, 0.28f, 0.9f);
        private static readonly Color OrnateGemColor = new Color(0.1f, 0.95f, 0.58f, 1f);
        private static readonly Color OrnateTextColor = new Color(0.94f, 0.96f, 1f, 1f);

        [SerializeField]
        private float displaySeconds = 2.75f;

        private GameObject popupPanel;
        private Image popupIconImage;
        private Text popupTitleText;
        private Text popupBodyText;
        private Text popupStatusText;
        private GameObject pendingLootHud;
        private Image pendingSlotBackgroundImage;
        private Image pendingItemIconImage;
        private Coroutine hideRoutine;
        private static Sprite pendingSlotSprite;
        /// <summary>
        /// Unity 创建对象时自动调用。这里通常缓存组件、加载资源，并把脚本内部状态准备好。
        /// </summary>
        private void Awake()
        {
            EnsureUi();
            HidePopup();
        }
        /// <summary>
        /// 显示对应 UI 或视觉状态，通常用于弹窗、loot 提示、死亡提示或结算反馈。
        /// </summary>
        /// <param name="loot">单个战利品数据，包含物品名、稀有度和权重。</param>
        public void ShowLootFound(LootDefinition loot)
        {
            EnsureUi();

            popupTitleText.text = $"Found: {loot.itemName}";
            popupBodyText.text = string.IsNullOrWhiteSpace(loot.description)
                ? "Escape alive to keep it."
                : loot.description;
            popupStatusText.text = "Pending Loot";
            ApplyPopupIcon(loot.icon);
            popupPanel.SetActive(true);

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HidePopupAfterDelay());
            Debug.Log("Loot feedback UI shown.");
        }
        /// <summary>
        /// 显示对应 UI 或视觉状态，通常用于弹窗、loot 提示、死亡提示或结算反馈。
        /// </summary>
        /// <param name="securedLoot">已经成功带到出口并结算的 loot 列表。</param>
        public void ShowLootSecured(IReadOnlyList<LootDefinition> securedLoot)
        {
            EnsureUi();

            LootDefinition firstLoot = securedLoot != null && securedLoot.Count > 0 ? securedLoot[0] : default;
            popupTitleText.text = securedLoot != null && securedLoot.Count == 1
                ? $"Secured: {firstLoot.itemName}"
                : $"Secured: {FormatLoot(securedLoot)}";
            popupBodyText.text = !string.IsNullOrWhiteSpace(firstLoot.description)
                ? firstLoot.description
                : "Loot secured.";
            popupStatusText.text = "Loot secured";
            ApplyPopupIcon(firstLoot.icon);
            popupPanel.SetActive(true);

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HidePopupAfterDelay());
        }
        /// <summary>
        /// 显示对应 UI 或视觉状态，通常用于弹窗、loot 提示、死亡提示或结算反馈。
        /// </summary>
        /// <param name="lostLoot">玩家死亡时丢失的 pending loot 列表。</param>
        public void ShowDeath(IReadOnlyList<LootDefinition> lostLoot)
        {
            EnsureUi();

            LootDefinition firstLoot = lostLoot != null && lostLoot.Count > 0 ? lostLoot[0] : default;
            popupTitleText.text = lostLoot != null && lostLoot.Count > 0 ? "Pending loot lost!" : "You Died";
            popupBodyText.text = lostLoot != null && lostLoot.Count > 0
                ? $"Lost: {FormatLoot(lostLoot)}"
                : "Restarting current level...";
            popupStatusText.text = lostLoot != null && lostLoot.Count > 0 ? "Lost" : "Death";
            ApplyPopupIcon(firstLoot.icon);
            popupPanel.SetActive(true);

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }
        }
        /// <summary>
        /// 重新读取当前数据并更新显示或玩法状态。
        /// </summary>
        /// <param name="pendingLoot">当前已经获得但还没带到出口的 loot 列表。</param>
        /// <param name="securedLoot">已经成功带到出口并结算的 loot 列表。</param>
        public void RefreshLootState(IReadOnlyList<LootDefinition> pendingLoot, IReadOnlyList<LootDefinition> securedLoot)
        {
            EnsureUi();

            if (pendingLoot == null || pendingLoot.Count == 0)
            {
                pendingLootHud.SetActive(false);
                pendingItemIconImage.sprite = null;
                pendingItemIconImage.enabled = false;
                return;
            }

            LootDefinition currentLoot = pendingLoot[pendingLoot.Count - 1];
            pendingItemIconImage.sprite = currentLoot.icon;
            pendingItemIconImage.enabled = currentLoot.icon != null;
            pendingLootHud.SetActive(true);
        }
        /// <summary>
        /// 隐藏对应 UI 或视觉状态，通常在提示结束、关闭弹窗或清理流程时调用。
        /// </summary>
        /// <returns>返回 Unity 协程，调用方会用 StartCoroutine 让这个流程分帧执行。</returns>
        private IEnumerator HidePopupAfterDelay()
        {
            yield return new WaitForSecondsRealtime(displaySeconds);
            HidePopup();
        }
        /// <summary>
        /// 隐藏对应 UI 或视觉状态，通常在提示结束、关闭弹窗或清理流程时调用。
        /// </summary>
        private void HidePopup()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }

            hideRoutine = null;
        }
        /// <summary>
        /// 确保 loot HUD 和弹窗 UI 已经创建。GameManager 第一次刷新 loot 状态时会触发它。
        /// </summary>
        private void EnsureUi()
        {
            if (popupPanel != null)
            {
                // 已创建过 UI 时直接复用，不重复生成 Canvas。
                return;
            }

            // loot UI 使用独立 Canvas，排序高于普通 HUD，低于剧情/教程关键弹窗。
            GameObject canvasObject = new GameObject("LootFeedbackUI");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            CreatePendingLootHud(canvasObject.transform);
            CreatePopupPanel(canvasObject.transform);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        private void CreatePendingLootHud(Transform parent)
        {
            pendingLootHud = new GameObject("PendingLootHud");
            pendingLootHud.transform.SetParent(parent, false);

            RectTransform hudRect = pendingLootHud.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(0f, 1f);
            hudRect.pivot = new Vector2(0f, 1f);
            hudRect.anchoredPosition = new Vector2(20f, -20f);
            hudRect.sizeDelta = new Vector2(300f, 95f);

            pendingSlotBackgroundImage = CreateImage("SlotBackgroundImage", pendingLootHud.transform);
            pendingSlotBackgroundImage.sprite = LoadPendingSlotSprite();
            pendingSlotBackgroundImage.enabled = pendingSlotBackgroundImage.sprite != null;
            RectTransform backgroundRect = pendingSlotBackgroundImage.GetComponent<RectTransform>();
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = hudRect.sizeDelta;

            pendingItemIconImage = CreateImage("PendingItemIcon", pendingLootHud.transform);
            RectTransform iconRect = pendingItemIconImage.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(18f, -18f);
            iconRect.sizeDelta = new Vector2(60f, 60f);

            pendingLootHud.SetActive(false);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        private void CreatePopupPanel(Transform parent)
        {
            if (ShouldUseOrnateLootPopup())
            {
                CreateOrnatePopupPanel(parent);
                return;
            }

            popupPanel = CreatePanel("LootPopupPanel", parent, new Color(0f, 0f, 0f, 0.72f));
            RectTransform panelRect = popupPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(440f, 520f);

            popupIconImage = CreateImage("IconImage", popupPanel.transform);
            RectTransform iconRect = popupIconImage.GetComponent<RectTransform>();
            iconRect.anchoredPosition = new Vector2(50f, -32f);
            iconRect.sizeDelta = new Vector2(340f, 340f);

            popupTitleText = CreateText("TitleText", popupPanel.transform, "Loot Found!", 26, FontStyle.Bold, new Color(1f, 0.86f, 0.2f));
            RectTransform titleRect = popupTitleText.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(34f, -384f);
            titleRect.sizeDelta = new Vector2(372f, 34f);

            popupStatusText = CreateText("StatusText", popupPanel.transform, "Pending Loot", 17, FontStyle.Bold, new Color(0.45f, 1f, 0.62f));
            RectTransform statusRect = popupStatusText.GetComponent<RectTransform>();
            statusRect.anchoredPosition = new Vector2(34f, -422f);
            statusRect.sizeDelta = new Vector2(372f, 24f);

            popupBodyText = CreateText("DescriptionText", popupPanel.transform, "Escape alive to keep it.", 18, FontStyle.Normal, Color.white);
            RectTransform bodyRect = popupBodyText.GetComponent<RectTransform>();
            bodyRect.anchoredPosition = new Vector2(34f, -454f);
            bodyRect.sizeDelta = new Vector2(372f, 54f);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        private void CreateOrnatePopupPanel(Transform parent)
        {
            popupPanel = CreatePanel("LootPopupPanel_OrnatePreview", parent, OrnatePanelColor);
            RectTransform panelRect = popupPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(500f, 540f);

            CreateOrnateFrame(popupPanel.transform);

            popupIconImage = CreateImage("IconImage", popupPanel.transform);
            RectTransform iconRect = popupIconImage.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -58f);
            iconRect.sizeDelta = new Vector2(220f, 220f);

            CreateDecorRect("TitleDividerLeft", popupPanel.transform, new Vector2(0.5f, 1f), new Vector2(-105f, -300f), new Vector2(150f, 2f), new Color(0.7f, 0.52f, 0.18f, 0.65f));
            CreateDecorRect("TitleDividerRight", popupPanel.transform, new Vector2(0.5f, 1f), new Vector2(105f, -300f), new Vector2(150f, 2f), new Color(0.7f, 0.52f, 0.18f, 0.65f));
            CreateGem("TitleDividerGem", popupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -300f), 14f);

            popupTitleText = CreateText("TitleText", popupPanel.transform, "Loot Found!", 23, FontStyle.Bold, OrnateGoldColor);
            ApplyPopupFont(popupTitleText);
            popupTitleText.alignment = TextAnchor.MiddleCenter;
            popupTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            popupTitleText.verticalOverflow = VerticalWrapMode.Truncate;
            popupTitleText.resizeTextForBestFit = true;
            popupTitleText.resizeTextMinSize = 18;
            popupTitleText.resizeTextMaxSize = 23;
            AddTextShadow(popupTitleText, new Color(0f, 0f, 0f, 0.86f), new Vector2(2f, -2f));
            RectTransform titleRect = popupTitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, -332f);
            titleRect.sizeDelta = new Vector2(392f, 56f);

            GameObject statusBadge = CreatePanel("StatusBadge", popupPanel.transform, new Color(0.02f, 0.18f, 0.08f, 0.78f));
            RectTransform badgeRect = statusBadge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 1f);
            badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(0f, -382f);
            badgeRect.sizeDelta = new Vector2(226f, 34f);
            CreateBadgeBorder(statusBadge.transform);

            popupStatusText = CreateText("StatusText", statusBadge.transform, "Pending Loot", 15, FontStyle.Bold, new Color(0.45f, 1f, 0.62f, 1f));
            ApplyPopupFont(popupStatusText);
            popupStatusText.alignment = TextAnchor.MiddleCenter;
            popupStatusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            popupStatusText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform statusRect = popupStatusText.GetComponent<RectTransform>();
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = Vector2.one;
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.offsetMin = new Vector2(34f, 0f);
            statusRect.offsetMax = new Vector2(-34f, 0f);

            CreateDecorRect("DescriptionDividerLeft", popupPanel.transform, new Vector2(0.5f, 1f), new Vector2(-105f, -420f), new Vector2(150f, 2f), new Color(0.7f, 0.52f, 0.18f, 0.55f));
            CreateDecorRect("DescriptionDividerRight", popupPanel.transform, new Vector2(0.5f, 1f), new Vector2(105f, -420f), new Vector2(150f, 2f), new Color(0.7f, 0.52f, 0.18f, 0.55f));
            CreateGem("DescriptionDividerGem", popupPanel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -420f), 10f);

            popupBodyText = CreateText("DescriptionText", popupPanel.transform, "Escape alive to keep it.", 17, FontStyle.Bold, OrnateTextColor);
            ApplyPopupFont(popupBodyText);
            popupBodyText.alignment = TextAnchor.MiddleCenter;
            popupBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            popupBodyText.lineSpacing = 1.1f;
            AddTextShadow(popupBodyText, new Color(0f, 0f, 0f, 0.85f), new Vector2(2f, -2f));
            RectTransform bodyRect = popupBodyText.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 1f);
            bodyRect.anchorMax = new Vector2(0.5f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = new Vector2(0f, -462f);
            bodyRect.sizeDelta = new Vector2(400f, 54f);
        }
        /// <summary>
        /// 根据当前游戏状态判断是否应该执行某个流程。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private static bool ShouldUseOrnateLootPopup()
        {
            string activeSceneName = SceneManager.GetActiveScene().name;
            return activeSceneName == Level1SceneName ||
                activeSceneName == Level2SceneName ||
                activeSceneName == Level3SceneName;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <returns>返回创建或找到的 GameObject，方便调用方继续添加组件或设置位置。</returns>
        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<RectTransform>();

            Image image = panel.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return panel;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        private static void CreateOrnateFrame(Transform parent)
        {
            GameObject decorRootObject = new GameObject("OrnateLootFrame");
            decorRootObject.transform.SetParent(parent, false);
            RectTransform decorRoot = decorRootObject.AddComponent<RectTransform>();
            decorRoot.anchorMin = Vector2.zero;
            decorRoot.anchorMax = Vector2.one;
            decorRoot.offsetMin = Vector2.zero;
            decorRoot.offsetMax = Vector2.zero;
            decorRoot.SetAsFirstSibling();

            CreateDecorRect("TopOuterFrame", decorRoot, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(450f, 5f), OrnateGoldColor);
            CreateDecorRect("TopInnerFrame", decorRoot, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(426f, 3f), OrnateGreenColor);
            CreateDecorRect("BottomOuterFrame", decorRoot, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(450f, 5f), OrnateGoldColor);
            CreateDecorRect("BottomInnerFrame", decorRoot, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(426f, 3f), OrnateGreenColor);
            CreateDecorRect("LeftOuterFrame", decorRoot, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(5f, 490f), OrnateGoldColor);
            CreateDecorRect("LeftInnerFrame", decorRoot, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(3f, 466f), OrnateGreenColor);
            CreateDecorRect("RightOuterFrame", decorRoot, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(5f, 490f), OrnateGoldColor);
            CreateDecorRect("RightInnerFrame", decorRoot, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(3f, 466f), OrnateGreenColor);

            CreateCornerOrnament("TopLeftCorner", decorRoot, new Vector2(0f, 1f), new Vector2(30f, -30f), 0.68f);
            CreateCornerOrnament("TopRightCorner", decorRoot, new Vector2(1f, 1f), new Vector2(-30f, -30f), 0.68f);
            CreateCornerOrnament("BottomLeftCorner", decorRoot, new Vector2(0f, 0f), new Vector2(30f, 30f), 0.68f);
            CreateCornerOrnament("BottomRightCorner", decorRoot, new Vector2(1f, 0f), new Vector2(-30f, 30f), 0.68f);

            CreateGem("TopCenterGem", decorRoot, new Vector2(0.5f, 1f), new Vector2(0f, -22f), 24f);
            CreateGem("BottomCenterGem", decorRoot, new Vector2(0.5f, 0f), new Vector2(0f, 22f), 24f);
            CreateVineCluster("TopLeftVines", decorRoot, new Vector2(0f, 1f), new Vector2(62f, -44f), false, 0.65f);
            CreateVineCluster("TopRightVines", decorRoot, new Vector2(1f, 1f), new Vector2(-62f, -44f), true, 0.65f);
            CreateVineCluster("BottomLeftVines", decorRoot, new Vector2(0f, 0f), new Vector2(62f, 44f), false, 0.65f);
            CreateVineCluster("BottomRightVines", decorRoot, new Vector2(1f, 0f), new Vector2(-62f, 44f), true, 0.65f);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        private static void CreateBadgeBorder(Transform parent)
        {
            CreateDecorRect("BadgeTop", parent, new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(184f, 2f), OrnateGreenColor);
            CreateDecorRect("BadgeBottom", parent, new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(184f, 2f), OrnateGreenColor);
            CreateGem("BadgeLeftGem", parent, new Vector2(0f, 0.5f), new Vector2(18f, 0f), 8f);
            CreateGem("BadgeRightGem", parent, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), 8f);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="size">size 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <returns>返回创建或找到的 UI Image 组件。</returns>
        private static Image CreateDecorRect(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject rectObject = new GameObject(name);
            rectObject.transform.SetParent(parent, false);

            RectTransform rect = rectObject.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = rectObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="scale">scale 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private static void CreateCornerOrnament(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float scale)
        {
            CreateDecorRect(name + "_GoldSquare", parent, anchor, anchoredPosition, new Vector2(40f, 40f) * scale, OrnateGoldColor);
            CreateDecorRect(name + "_DarkInset", parent, anchor, anchoredPosition, new Vector2(28f, 28f) * scale, OrnatePanelColor);
            Image center = CreateDecorRect(name + "_GreenInset", parent, anchor, anchoredPosition, new Vector2(14f, 14f) * scale, OrnateGreenColor);
            center.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="size">size 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private static void CreateGem(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float size)
        {
            Image outer = CreateDecorRect(name + "_Outer", parent, anchor, anchoredPosition, new Vector2(size, size), OrnateGoldColor);
            outer.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);

            Image inner = CreateDecorRect(name + "_Inner", parent, anchor, anchoredPosition, new Vector2(size * 0.5f, size * 0.5f), OrnateGemColor);
            inner.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="anchor">anchor 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="anchoredPosition">anchoredPosition 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="mirror">mirror 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="scale">scale 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private static void CreateVineCluster(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, bool mirror, float scale)
        {
            float direction = mirror ? -1f : 1f;
            CreateDecorRect(name + "_Stem", parent, anchor, anchoredPosition, new Vector2(6f, 46f) * scale, new Color(0.14f, 0.42f, 0.12f, 0.78f));
            CreateDecorRect(name + "_LeafA", parent, anchor, anchoredPosition + new Vector2(16f * direction, 12f) * scale, new Vector2(24f, 8f) * scale, new Color(0.2f, 0.55f, 0.16f, 0.82f));
            CreateDecorRect(name + "_LeafB", parent, anchor, anchoredPosition + new Vector2(26f * direction, -5f) * scale, new Vector2(22f, 8f) * scale, new Color(0.16f, 0.48f, 0.14f, 0.82f));
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <returns>返回创建或找到的 UI Image 组件。</returns>
        private static Image CreateImage(string name, Transform parent)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);

            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);

            Image image = imageObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }
        /// <summary>
        /// 从 Resources 或传入数据中加载需要的资源，并转换成脚本可直接使用的对象。
        /// </summary>
        /// <returns>返回加载或生成的 Sprite；资源不存在时可能返回 null。</returns>
        private static Sprite LoadPendingSlotSprite()
        {
            if (pendingSlotSprite != null)
            {
                return pendingSlotSprite;
            }

            string imagePath = Path.Combine(Application.dataPath, "Art", "UI", "PendingLootSlot.png");
            if (!File.Exists(imagePath))
            {
                Debug.LogWarning("Pending loot slot image missing at Assets/Art/UI/PendingLootSlot.png.");
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(imagePath)))
            {
                Debug.LogWarning("Pending loot slot image could not be loaded.");
                return null;
            }

            pendingSlotSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                Mathf.Max(texture.width, texture.height));
            pendingSlotSprite.name = "PendingLootSlot";
            return pendingSlotSprite;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <param name="text">text 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="fontSize">fontSize 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="fontStyle">fontStyle 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <returns>返回创建或找到的 UI Text 组件。</returns>
        private static Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle fontStyle, Color color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);

            Text uiText = textObject.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.fontStyle = fontStyle;
            uiText.color = color;
            uiText.alignment = TextAnchor.UpperLeft;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            uiText.raycastTarget = false;
            return uiText;
        }
        /// <summary>
        /// 把计算好的状态应用到对象、UI、动画或渲染器上，让视觉和逻辑保持同步。
        /// </summary>
        /// <param name="text">text 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private static void ApplyPopupFont(Text text)
        {
            if (text == null)
            {
                return;
            }

            Font popupFont = Resources.Load<Font>(PopupFontResourcePath);
            if (popupFont != null)
            {
                text.font = popupFont;
            }
        }
        /// <summary>
        /// 给 UI 文本添加阴影，让 loot 弹窗在森林背景上更清晰。
        /// </summary>
        /// <param name="text">text 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <param name="distance">distance 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private static void AddTextShadow(Text text, Color color, Vector2 distance)
        {
            if (text == null)
            {
                return;
            }

            Shadow shadow = text.GetComponent<Shadow>();
            if (shadow == null)
            {
                // 已有阴影就复用，没有才添加，避免重复 Shadow 组件叠加。
                shadow = text.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }
        /// <summary>
        /// 把数据整理成适合玩家阅读或 UI 显示的文字。
        /// </summary>
        /// <param name="loot">单个战利品数据，包含物品名、稀有度和权重。</param>
        /// <returns>返回整理后的文字，用于 UI 显示、日志或状态提示。</returns>
        private static string FormatLoot(IReadOnlyList<LootDefinition> loot)
        {
            if (loot == null || loot.Count == 0)
            {
                return "none";
            }

            List<string> labels = new List<string>(loot.Count);
            for (int i = 0; i < loot.Count; i++)
            {
                labels.Add(GetLootLabel(loot[i]));
            }

            return string.Join(", ", labels);
        }
        /// <summary>
        /// 把计算好的状态应用到对象、UI、动画或渲染器上，让视觉和逻辑保持同步。
        /// </summary>
        /// <param name="icon">icon 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private void ApplyPopupIcon(Sprite icon)
        {
            if (popupIconImage == null)
            {
                return;
            }

            popupIconImage.sprite = icon;
            popupIconImage.enabled = icon != null;
        }
        /// <summary>
        /// 把单个 LootDefinition 转成玩家可读的显示文字。收藏物不重复显示 [Collectible]。
        /// </summary>
        /// <param name="loot">单个战利品数据，包含物品名、稀有度和权重。</param>
        /// <returns>返回整理后的文字，用于 UI 显示、日志或状态提示。</returns>
        private static string GetLootLabel(LootDefinition loot)
        {
            if (string.IsNullOrWhiteSpace(loot.rarity) || loot.rarity == "Collectible")
            {
                return loot.itemName;
            }

            return $"{loot.itemName} [{loot.rarity}]";
        }
    }
}
