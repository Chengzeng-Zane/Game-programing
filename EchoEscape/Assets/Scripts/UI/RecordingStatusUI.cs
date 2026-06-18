using UnityEngine;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Recording Status UI. player press Q Start recording Echo, it will appear red at the bottom of the screen REC dots, REC Text and recording timing.
/// This script only reads ActionRecorder of IsRecording、RecordingProgress and maxRecordSeconds, does not change recording, playback or player control logic.
/// it consists of EchoEscapeGameManager Automatically created so that the final game does not depend on Console The log can also clearly tell players that recording is taking place.
    /// </summary>
    public class RecordingStatusUI : MonoBehaviour
    {
        private const string PixelFontResourcePath = "BrackeysPlatformer/Fonts/PixelOperator8-Bold";
        private const int CanvasSortingOrder = 105;
        private const float BlinkSpeed = 5f;
        private static readonly Color RecordingDotColor = new Color(1f, 0.08f, 0.06f, 1f);
        private static readonly Color ReplayIconColor = new Color(0.96f, 0.98f, 1f, 1f);

        private GameObject rootPanel;
        private Image recordDot;
        private Text recordLabel;
        private Text timerLabel;
        private ActionRecorder recorder;
        private Sprite dotSprite;
        private Sprite playSprite;

        /// <summary>
/// Unity Called when creating an object. Here you will find the recorder and create the recording at the bottom of the screen UI。
        /// </summary>
        private void Awake()
        {
            recorder = ResolveRecorder();
            EnsureUi();
            SetVisible(false);
        }

        /// <summary>
/// Refresh recording every frame UI. Display and update the time when recording; hide it when not recording to avoid blocking the player's view.
        /// </summary>
        private void Update()
        {
            if (recorder == null)
            {
                recorder = ResolveRecorder();
            }

            bool playerIsDying = EchoEscapeGameManager.Instance != null && EchoEscapeGameManager.Instance.IsPlayerDeadOrDying;
            bool isRecording = recorder != null && recorder.IsRecording && !playerIsDying;
            EchoReplayController activeEcho = recorder != null ? recorder.ActiveEcho : null;
            bool isReplaying = activeEcho != null && activeEcho.IsReplaying && !isRecording && !playerIsDying;

            SetVisible(isRecording || isReplaying);
            if (!isRecording)
            {
                if (isReplaying)
                {
                    ApplyMode("PLAY", ReplayIconColor, GetPlaySprite(), new Vector2(22f, 24f));
                    timerLabel.text = FormatTime(activeEcho.ReplayElapsedSeconds);
                    PulseDot();
                }

                return;
            }

            ApplyMode("REC", RecordingDotColor, GetDotSprite(), new Vector2(20f, 20f));
            timerLabel.text = FormatTime(recorder.RecordingElapsedSeconds);
            PulseDot();
        }

        /// <summary>
/// from GameManager Or find it on the current player in the scene ActionRecorder。
        /// </summary>
/// <returns>found ActionRecorder; If the scene has not been initialized yet, return null。</returns>
        private static ActionRecorder ResolveRecorder()
        {
            if (EchoEscapeGameManager.Instance != null && EchoEscapeGameManager.Instance.recorder != null)
            {
                return EchoEscapeGameManager.Instance.recorder;
            }

            return FindObjectOfType<ActionRecorder>();
        }

        /// <summary>
/// create Canvas, bottom panel, red recording point, REC Text and timing text.
        /// </summary>
        private void EnsureUi()
        {
            if (rootPanel != null)
            {
                return;
            }

            GameObject canvasObject = new GameObject("RecordingStatusUI");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            rootPanel = new GameObject("RecordingPanel");
            rootPanel.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = rootPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 26f);
            panelRect.sizeDelta = new Vector2(430f, 58f);

            Image panelImage = rootPanel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.58f);

            recordDot = CreateImage("StatusIcon", rootPanel.transform, new Vector2(-166f, 0f), new Vector2(20f, 20f));
            recordDot.sprite = GetDotSprite();
            recordDot.color = RecordingDotColor;

            Font font = LoadFont();
            recordLabel = CreateText("RecordLabel", rootPanel.transform, "REC", font, 28, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(-88f, 0f), new Vector2(128f, 48f));
            timerLabel = CreateText("TimerLabel", rootPanel.transform, "00:00", font, 28, FontStyle.Bold, TextAnchor.MiddleRight, new Vector2(118f, 0f), new Vector2(170f, 48f));
        }

        /// <summary>
/// Show or hide the entire recording panel.
        /// </summary>
/// <param name="visible">true Indicates that it is displayed during recording. false Indicates hidden. </param>
        private void SetVisible(bool visible)
        {
            if (rootPanel != null && rootPanel.activeSelf != visible)
            {
                rootPanel.SetActive(visible);
            }
        }

        /// <summary>
/// Switch the left text and dot color according to the current status. Recording display rec，Echo Playback display play。
        /// </summary>
/// <param name="label">Status text, e. g. rec or play。</param>
/// <param name="dotColor">Status dot color. </param>
        private void ApplyMode(string label, Color dotColor, Sprite iconSprite, Vector2 iconSize)
        {
            if (recordLabel != null)
            {
                recordLabel.text = label;
            }

            if (recordDot != null)
            {
                recordDot.sprite = iconSprite;
                recordDot.rectTransform.sizeDelta = iconSize;
                Color current = dotColor;
                current.a = recordDot.color.a;
                recordDot.color = current;
            }
        }

        /// <summary>
/// Make the status dot flash slightly to indicate that the player is currently recording or Echo Replay this temporary state.
        /// </summary>
        private void PulseDot()
        {
            Color dotColor = recordDot.color;
            dotColor.a = Mathf.Lerp(0.35f, 1f, (Mathf.Sin(Time.unscaledTime * BlinkSpeed) + 1f) * 0.5f);
            recordDot.color = dotColor;
        }

        /// <summary>
/// Create a UI Picture element for red recording dots.
        /// </summary>
/// <param name="name">New object name. </param>
/// <param name="parent">parent Transform。</param>
/// <param name="anchoredPosition">position in the panel. </param>
/// <param name="size">Image size. </param>
/// <returns>Created Image components. </returns>
        private static Image CreateImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            return imageObject.AddComponent<Image>();
        }

        /// <summary>
/// Create a UI text element for REC Label or timer.
        /// </summary>
/// <param name="name">New object name. </param>
/// <param name="parent">parent Transform。</param>
/// <param name="text">Initial text. </param>
/// <param name="font">The font used. </param>
/// <param name="fontSize">Font size. </param>
/// <param name="fontStyle">Font style. </param>
/// <param name="alignment">Text alignment. </param>
/// <param name="anchoredPosition">position in the panel. </param>
/// <param name="size">Text area size. </param>
/// <returns>Created Text components. </returns>
        private static Text CreateText(
            string name,
            Transform parent,
            string text,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text label = textObject.AddComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = new Color(0.96f, 0.98f, 1f, 1f);
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        /// <summary>
/// Format seconds into 00: 02 This recording timing format.
        /// </summary>
/// <param name="seconds">The number of seconds that have elapsed since recording. </param>
/// <returns>Formatted timing text. </returns>
        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }

        /// <summary>
/// Combine the current timer and the maximum duration into one text. During live demonstration, change ActionRecorder of Max records Seconds back, UI The new upper limit will be displayed directly.
        /// </summary>
/// <param name="elapsedSeconds">The current number of seconds that has been recorded or played back. </param>
/// <param name="durationSeconds">Maximum recording duration or current Echo The total playback duration. </param>
/// <returns>For example 00: 01/00: 03 timing text. </returns>
        /// <summary>
/// Load the pixel font in the project. Used when not found Unity Default font, avoid UI An error is reported due to lack of resources.
        /// </summary>
/// <returns>Available for Text font. </returns>
        private static Font LoadFont()
        {
            Font font = Resources.Load<Font>(PixelFontResourcePath);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>
/// Create a runtime dot Sprite, used as a red recording indicator light.
        /// </summary>
/// <returns>round Sprite。</returns>
        private Sprite GetDotSprite()
        {
            if (dotSprite != null)
            {
                return dotSprite;
            }

            const int size = 24;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new Color(1f, 1f, 1f, 0f);
            Color solid = Color.white;
            float center = (size - 1) * 0.5f;
            float radius = center - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    texture.SetPixel(x, y, distance <= radius ? solid : clear);
                }
            }

            texture.Apply();
            dotSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            dotSprite.name = "RecordingDot";
            return dotSprite;
        }

        /// <summary>
/// Create a right-facing pixel triangle Sprite, used as Echo during playback PLAY icon.
        /// </summary>
/// <returns>triangle play Sprite。</returns>
        private Sprite GetPlaySprite()
        {
            if (playSprite != null)
            {
                return playSprite;
            }

            const int width = 24;
            const int height = 24;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new Color(1f, 1f, 1f, 0f);
            Color solid = Color.white;
            float centerY = (height - 1) * 0.5f;
            float halfHeight = 9f;
            float left = 5f;
            float maxWidth = 15f;

            for (int y = 0; y < height; y++)
            {
                float verticalDistance = Mathf.Abs(y - centerY);
                float rowWidth = Mathf.Max(0f, maxWidth * (1f - verticalDistance / halfHeight));
                float right = left + rowWidth;

                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, x >= left && x <= right ? solid : clear);
                }
            }

            texture.Apply();
            playSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height);
            playSprite.name = "ReplayPlayIcon";
            return playSprite;
        }
    }
}
