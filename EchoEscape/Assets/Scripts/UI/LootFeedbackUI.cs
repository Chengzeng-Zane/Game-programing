using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Loot and Death Feedback UI. It is responsible for telling the player what has been obtained, what has not yet been taken away, what has been safely resolved, and what is lost upon death.
/// Gameplay logic: After opening a treasure chest or picking it up, you will be prompted to get a prompt and join. pending; Displayed after arriving at the exit secured; Displayed when death lost loot. It also maintains the upper left corner HUD, allowing players to know their loot risk.
/// Collaborates with: EchoEscapeGameManager Called when acquiring, dying, or clearing a level; GoalZone Waiting for the end of the third level loot The prompt appears first.
    /// </summary>
    public class LootFeedbackUI : MonoBehaviour
    {
        private const string Level2SceneName = "Level 2 - Relics of the Forest";
        private const string Level3SceneName = "Level 3 - Escape from the Silent Forest";
        private const string Level1SceneName = "Level 1 - The First Echo";
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
/// Unity Automatically called when creating an object. This is where components are usually cached, resources are loaded, and the script's internal state is prepared.
        /// </summary>
        private void Awake()
        {
            EnsureUi();
            HidePopup();
        }
        /// <summary>
/// Show correspondence UI or visual status, usually used for pop-up windows, loot Hints, death prompts or settlement feedback.
        /// </summary>
/// <param name="loot">Individual loot data, including item name, rarity, and weight. </param>
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
/// Show correspondence UI or visual status, usually used for pop-up windows, loot Hints, death prompts or settlement feedback.
        /// </summary>
/// <param name="securedLoot">Has been successfully brought to the exit and settled loot list. </param>
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
/// Show correspondence UI or visual status, usually used for pop-up windows, loot Hints, death prompts or settlement feedback.
        /// </summary>
/// <param name="lostLoot">Lost when player dies pending loot list. </param>
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
/// Reread current data and update display or gameplay state.
        /// </summary>
/// <param name="pendingLoot">Currently obtained but not brought to export yet loot list. </param>
/// <param name="securedLoot">Has been successfully brought to the exit and settled loot list. </param>
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
/// Hide correspondence UI Or visual state, usually called when the prompt ends, the pop-up window is closed, or the process is cleaned up.
        /// </summary>
/// <returns>return Unity Coroutine, the caller will use StartCoroutine Let this process be executed in frames. </returns>
        private IEnumerator HidePopupAfterDelay()
        {
            yield return new WaitForSecondsRealtime(displaySeconds);
            HidePopup();
        }
        /// <summary>
/// Hide correspondence UI Or visual state, usually called when the prompt ends, the pop-up window is closed, or the process is cleaned up.
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
/// make sure loot HUD and pop-ups UI Already created. GameManager first refresh loot It will be triggered when the status is reached.
        /// </summary>
        private void EnsureUi()
        {
            if (popupPanel != null)
            {
// Already created UI Directly reuse without repeated generation Canvas。
                return;
            }

// loot UI Use independent Canvas, ranked higher than ordinary HUD, lower than the plot/Tutorial key pop-up window.
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
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
/// Determine whether a certain process should be executed based on the current game state.
        /// </summary>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private static bool ShouldUseOrnateLootPopup()
        {
            string activeSceneName = SceneManager.GetActiveScene().name;
            return activeSceneName == Level1SceneName ||
                activeSceneName == Level2SceneName ||
                activeSceneName == Level3SceneName;
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <returns>Returns a created or found GameObjectto facilitate the caller to continue adding components or setting locations. </returns>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
        private static void CreateBadgeBorder(Transform parent)
        {
            CreateDecorRect("BadgeTop", parent, new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(184f, 2f), OrnateGreenColor);
            CreateDecorRect("BadgeBottom", parent, new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(184f, 2f), OrnateGreenColor);
            CreateGem("BadgeLeftGem", parent, new Vector2(0f, 0.5f), new Vector2(18f, 0f), 8f);
            CreateGem("BadgeRightGem", parent, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), 8f);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="size">size Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <returns>Returns a created or found UI Image components. </returns>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="scale">scale Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private static void CreateCornerOrnament(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float scale)
        {
            CreateDecorRect(name + "_GoldSquare", parent, anchor, anchoredPosition, new Vector2(40f, 40f) * scale, OrnateGoldColor);
            CreateDecorRect(name + "_DarkInset", parent, anchor, anchoredPosition, new Vector2(28f, 28f) * scale, OrnatePanelColor);
            Image center = CreateDecorRect(name + "_GreenInset", parent, anchor, anchoredPosition, new Vector2(14f, 14f) * scale, OrnateGreenColor);
            center.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="size">size Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private static void CreateGem(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, float size)
        {
            Image outer = CreateDecorRect(name + "_Outer", parent, anchor, anchoredPosition, new Vector2(size, size), OrnateGoldColor);
            outer.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);

            Image inner = CreateDecorRect(name + "_Inner", parent, anchor, anchoredPosition, new Vector2(size * 0.5f, size * 0.5f), OrnateGemColor);
            inner.rectTransform.rotation = Quaternion.Euler(0f, 0f, 45f);
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="anchor">anchor Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="anchoredPosition">anchoredPosition Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="mirror">mirror Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="scale">scale Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private static void CreateVineCluster(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, bool mirror, float scale)
        {
            float direction = mirror ? -1f : 1f;
            CreateDecorRect(name + "_Stem", parent, anchor, anchoredPosition, new Vector2(6f, 46f) * scale, new Color(0.14f, 0.42f, 0.12f, 0.78f));
            CreateDecorRect(name + "_LeafA", parent, anchor, anchoredPosition + new Vector2(16f * direction, 12f) * scale, new Vector2(24f, 8f) * scale, new Color(0.2f, 0.55f, 0.16f, 0.82f));
            CreateDecorRect(name + "_LeafB", parent, anchor, anchoredPosition + new Vector2(26f * direction, -5f) * scale, new Vector2(22f, 8f) * scale, new Color(0.16f, 0.48f, 0.14f, 0.82f));
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <returns>Returns a created or found UI Image components. </returns>
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
/// from Resources Or load the required resources from the incoming data and convert it into an object that can be used directly by the script.
        /// </summary>
/// <returns>Returns the loaded or generated Sprite; May be returned when the resource does not exist null。</returns>
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
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <param name="text">text Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="fontSize">fontSize Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="fontStyle">fontStyle Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <returns>Returns a created or found UI Text components. </returns>
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
/// Apply the calculated state to the object, UI, animation or renderer to keep visuals and logic in sync.
        /// </summary>
/// <param name="text">text Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
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
/// Give UI Add shadow to text to make loot The pop-up is clearer on the forest background.
        /// </summary>
/// <param name="text">text Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <param name="distance">distance Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private static void AddTextShadow(Text text, Color color, Vector2 distance)
        {
            if (text == null)
            {
                return;
            }

            Shadow shadow = text.GetComponent<Shadow>();
            if (shadow == null)
            {
// Reuse shadows if they already exist, and add them if they don’t exist to avoid duplication. Shadow Component overlay.
                shadow = text.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }
        /// <summary>
/// Organize the data into a format suitable for players to read or UI The text displayed.
        /// </summary>
/// <param name="loot">Individual loot data, including item name, rarity, and weight. </param>
/// <returns>Returns the sorted text for UI Display, log or status prompts. </returns>
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
/// Apply the calculated state to the object, UI, animation or renderer to keep visuals and logic in sync.
        /// </summary>
/// <param name="icon">icon Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
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
/// put a single LootDefinition Convert it to player-readable display text. Collections are not displayed repeatedly [Collectible]。
        /// </summary>
/// <param name="loot">Individual loot data, including item name, rarity, and weight. </param>
/// <returns>Returns the sorted text for UI Display, log or status prompts. </returns>
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
