using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// Shows and hides the dark tutorial popup UI used by question mark triggers.
    /// </summary>
    /// <remarks>
    /// Attach this script to the tutorial popup Canvas object.
    /// TutorialPopupTrigger calls ShowPopup when the player enters a question mark trigger.
    /// The manager updates the title/body Text components, pauses the game if configured, and closes on Escape or Return.
    /// </remarks>
    public class TutorialPopupManager : MonoBehaviour
    {
        [Tooltip("The panel that contains the tutorial popup UI.")]
        /// <summary>
        /// Root panel GameObject that is enabled while a tutorial popup is visible.
        /// </summary>
        public GameObject popupPanel;

        [Tooltip("The title text shown at the top of the popup.")]
        /// <summary>
        /// UI Text component used for the popup title.
        /// </summary>
        public Text titleText;

        [Tooltip("The body text shown inside the popup.")]
        /// <summary>
        /// UI Text component used for the popup message body.
        /// </summary>
        public Text bodyText;

        [Tooltip("If true, the game pauses while the popup is open.")]
        /// <summary>
        /// If true, Time.timeScale is set to zero while the popup is open.
        /// </summary>
        public bool pauseGameWhenOpen = true;

        private bool popupOpen = false;
        private float previousTimeScale = 1.0f;

        /// <summary>
        /// Unity event method called when this object first becomes active.
        /// </summary>
        /// <remarks>
        /// Ensures the popup starts hidden before the player reaches any question mark trigger.
        /// </remarks>
        private void Awake()
        {
            ClosePopupWithoutTimeChange();
        }

        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        /// <remarks>
        /// Listens for Return or Escape while a popup is open.
        /// </remarks>
        private void Update()
        {
            if (popupOpen && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape)))
            {
                ClosePopup();
            }
        }

        /// <summary>
        /// Shows a tutorial popup with the supplied title and message text.
        /// </summary>
        /// <param name="popupTitle">Title displayed at the top of the popup.</param>
        /// <param name="popupMessage">Body message displayed inside the popup.</param>
        public void ShowPopup(string popupTitle, string popupMessage)
        {
            if (popupPanel == null)
            {
                Debug.LogWarning("Tutorial popup panel is missing.");
                return;
            }

            if (titleText != null)
            {
                titleText.text = popupTitle;
            }

            if (bodyText != null)
            {
                bodyText.text = popupMessage;
            }

            popupPanel.SetActive(true);
            popupOpen = true;

            if (pauseGameWhenOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0.0f;
            }
        }

        /// <summary>
        /// Closes the current tutorial popup and restores gameplay time if it was paused.
        /// </summary>
        public void ClosePopup()
        {
            ClosePopupWithoutTimeChange();

            if (pauseGameWhenOpen)
            {
                Time.timeScale = previousTimeScale;
            }
        }

        /// <summary>
        /// Hides the popup panel without changing Time.timeScale.
        /// </summary>
        /// <remarks>
        /// Used during Awake and by ClosePopup before time scale is restored.
        /// </remarks>
        private void ClosePopupWithoutTimeChange()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }

            popupOpen = false;
        }
    }
}
