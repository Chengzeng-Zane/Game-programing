using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EchoEscape
{
    /// <summary>
    /// A class to show and hide tutorial popup messages
    /// </summary>
    public class TutorialPopupManager : MonoBehaviour
    {
        [Tooltip("The panel that contains the tutorial popup UI.")]
        public GameObject popupPanel;

        [Tooltip("The title text shown at the top of the popup.")]
        public Text titleText;

        [Tooltip("The body text shown inside the popup.")]
        public Text bodyText;

        [Tooltip("If true, the game pauses while the popup is open.")]
        public bool pauseGameWhenOpen = true;

        private bool popupOpen = false;
        private float previousTimeScale = 1.0f;

        /// <summary>
        /// Description:
        /// Standard Unity function called when this object first becomes active
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void Awake()
        {
            ClosePopupWithoutTimeChange();
        }

        /// <summary>
        /// Description:
        /// Standard Unity function called once per frame
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void Update()
        {
            if (popupOpen && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape)))
            {
                ClosePopup();
            }
        }

        /// <summary>
        /// Description:
        /// Show a tutorial popup with a title and message
        /// Inputs:
        /// string popupTitle, string popupMessage
        /// Returns:
        /// void (no return)
        /// </summary>
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
        /// Description:
        /// Close the current tutorial popup
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
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
        /// Description:
        /// Hide the popup without changing the game time scale
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
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
