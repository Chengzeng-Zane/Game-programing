using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// A class to trigger tutorial popup messages when the player enters an area
    /// </summary>
    public class TutorialPopupTrigger : MonoBehaviour
    {
        [Tooltip("The popup manager that controls the tutorial UI.")]
        public TutorialPopupManager popupManager;

        [Tooltip("The title shown in the tutorial popup.")]
        public string tutorialTitle = "Tutorial";

        [Tooltip("The message shown in the tutorial popup.")]
        [TextArea(3, 8)]
        public string tutorialMessage = "Read this message, then press Close to continue.";

        [Tooltip("If true, this tutorial only appears once.")]
        public bool showOnlyOnce = true;

        [Tooltip("If true, the question mark object disappears after the tutorial is shown.")]
        public bool hideAfterUse = false;

        private bool hasShown = false;

        /// <summary>
        /// Description:
        /// Standard Unity function called when another 2D collider enters this trigger
        /// Inputs:
        /// Collider2D other
        /// Returns:
        /// void (no return)
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (showOnlyOnce && hasShown)
            {
                return;
            }

            if (other.GetComponent<PlayerController2D>() != null)
            {
                ShowTutorial();
            }
        }

        /// <summary>
        /// Description:
        /// Show this trigger's tutorial message
        /// Inputs:
        /// none
        /// Returns:
        /// void (no return)
        /// </summary>
        private void ShowTutorial()
        {
            if (popupManager == null)
            {
                popupManager = FindObjectOfType<TutorialPopupManager>();
            }

            if (popupManager == null)
            {
                Debug.LogWarning("No TutorialPopupManager was found in the scene.");
                return;
            }

            popupManager.ShowPopup(tutorialTitle, tutorialMessage);
            hasShown = true;

            if (hideAfterUse)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
