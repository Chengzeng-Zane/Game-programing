using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EchoEscape
{
    // Runtime UI for loot pickup popups and the persistent loot summary.
    /// <summary>
    /// Shows short loot pickup feedback and a small persistent loot summary.
    /// </summary>
    /// <remarks>
    /// EchoEscapeGameManager owns this component and calls it when pending or secured loot changes.
    /// The UI is built at runtime so generated tutorial scenes keep the same feedback after rebuilding.
    /// </remarks>
    public class LootFeedbackUI : MonoBehaviour
    {
        [SerializeField]
        private float displaySeconds = 2.75f;

        private GameObject popupPanel;
        private Text popupTitleText;
        private Text popupBodyText;
        private Text currentLootText;
        private Text securedLootText;
        private Coroutine hideRoutine;

        // Builds the UI once and starts with the popup hidden.
        private void Awake()
        {
            EnsureUi();
            HidePopup();
        }

        // Shows the temporary reward popup after a chest opens.
        /// <summary>
        /// Displays the short loot pickup popup.
        /// </summary>
        /// <param name="loot">Loot item found by the player.</param>
        public void ShowLootFound(LootDefinition loot)
        {
            EnsureUi();

            popupTitleText.text = "Loot Found!";
            popupBodyText.text = $"You found: {loot.itemName} [{loot.rarity}]\nEscape alive to keep it.";
            popupPanel.SetActive(true);

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HidePopupAfterDelay());
            Debug.Log("Loot feedback UI shown.");
        }

        /// <summary>
        /// Displays the death popup, including any pending loot lost before the scene reloads.
        /// </summary>
        /// <param name="lostLoot">Pending loot that was cleared by death.</param>
        public void ShowDeath(IReadOnlyList<LootDefinition> lostLoot)
        {
            EnsureUi();

            popupTitleText.text = "You Died";
            popupBodyText.text = lostLoot != null && lostLoot.Count > 0
                ? $"Loot Lost: {FormatLoot(lostLoot)}"
                : "Restarting current level...";
            popupPanel.SetActive(true);

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }
        }

        // Updates the persistent current and secured loot labels.
        /// <summary>
        /// Refreshes the persistent current and secured loot summary.
        /// </summary>
        /// <param name="pendingLoot">Loot currently at risk.</param>
        /// <param name="securedLoot">Loot already banked by reaching the exit.</param>
        public void RefreshLootState(IReadOnlyList<LootDefinition> pendingLoot, IReadOnlyList<LootDefinition> securedLoot)
        {
            EnsureUi();

            currentLootText.text = $"Pending Loot: {FormatLoot(pendingLoot)}";
            securedLootText.text = $"Secured Loot: {FormatLoot(securedLoot)}";
        }

        // Hides the popup after a short realtime delay.
        private IEnumerator HidePopupAfterDelay()
        {
            yield return new WaitForSecondsRealtime(displaySeconds);
            HidePopup();
        }

        // Hides the popup immediately.
        private void HidePopup()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }

            hideRoutine = null;
        }

        // Creates the runtime canvas and panels if they do not exist yet.
        private void EnsureUi()
        {
            if (popupPanel != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("LootFeedbackUI");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            CreateStatusPanel(canvasObject.transform);
            CreatePopupPanel(canvasObject.transform);
        }

        // Creates the always-visible loot summary panel.
        private void CreateStatusPanel(Transform parent)
        {
            GameObject panel = CreatePanel("LootStatusPanel", parent, new Color(0f, 0f, 0f, 0.48f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(16f, -16f);
            panelRect.sizeDelta = new Vector2(360f, 72f);

            currentLootText = CreateText("CurrentLootText", panel.transform, "Pending Loot: none", 18, FontStyle.Bold, new Color(1f, 0.9f, 0.35f));
            RectTransform currentRect = currentLootText.GetComponent<RectTransform>();
            currentRect.anchoredPosition = new Vector2(18f, -16f);
            currentRect.sizeDelta = new Vector2(324f, 24f);

            securedLootText = CreateText("SecuredLootText", panel.transform, "Secured Loot: none", 16, FontStyle.Normal, Color.white);
            RectTransform securedRect = securedLootText.GetComponent<RectTransform>();
            securedRect.anchoredPosition = new Vector2(18f, -44f);
            securedRect.sizeDelta = new Vector2(324f, 22f);
        }

        // Creates the temporary loot-found popup panel.
        private void CreatePopupPanel(Transform parent)
        {
            popupPanel = CreatePanel("LootFoundPanel", parent, new Color(0f, 0f, 0f, 0.68f));
            RectTransform panelRect = popupPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -86f);
            panelRect.sizeDelta = new Vector2(520f, 132f);

            popupTitleText = CreateText("LootFoundTitle", popupPanel.transform, "Loot Found!", 28, FontStyle.Bold, new Color(1f, 0.86f, 0.2f));
            RectTransform titleRect = popupTitleText.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(24f, -18f);
            titleRect.sizeDelta = new Vector2(472f, 36f);

            popupBodyText = CreateText("LootFoundBody", popupPanel.transform, "Escape alive to keep it.", 19, FontStyle.Normal, Color.white);
            RectTransform bodyRect = popupBodyText.GetComponent<RectTransform>();
            bodyRect.anchoredPosition = new Vector2(24f, -62f);
            bodyRect.sizeDelta = new Vector2(472f, 58f);
        }

        // Creates one tinted UI panel.
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

        // Creates one UI text element.
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

        // Formats loot entries for compact HUD display.
        private static string FormatLoot(IReadOnlyList<LootDefinition> loot)
        {
            if (loot == null || loot.Count == 0)
            {
                return "none";
            }

            List<string> labels = new List<string>(loot.Count);
            for (int i = 0; i < loot.Count; i++)
            {
                labels.Add($"{loot[i].itemName} [{loot[i].rarity}]");
            }

            return string.Join(", ", labels);
        }
    }
}
