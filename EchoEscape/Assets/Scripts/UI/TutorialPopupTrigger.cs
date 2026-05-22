using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Triggers a tutorial popup when the player enters a question mark area.
    /// </summary>
    /// <remarks>
    /// Attach this script to a question mark trigger object with a 2D trigger collider.
    /// It talks to TutorialPopupManager and is used in Level1_Tutorial for Jump and Record Yourself guidance.
    /// </remarks>
    public class TutorialPopupTrigger : MonoBehaviour
    {
        [Tooltip("The popup manager that controls the tutorial UI.")]
        /// <summary>
        /// Popup manager that displays this trigger's title and message.
        /// </summary>
        public TutorialPopupManager popupManager;

        [Tooltip("The title shown in the tutorial popup.")]
        /// <summary>
        /// Title shown in the tutorial popup.
        /// </summary>
        public string tutorialTitle = "Tutorial";

        [Tooltip("The message shown in the tutorial popup.")]
        [TextArea(3, 8)]
        /// <summary>
        /// Body text shown in the tutorial popup.
        /// </summary>
        public string tutorialMessage = "Read this message, then press Close to continue.";

        [Tooltip("If true, this tutorial only appears once.")]
        /// <summary>
        /// If true, the popup only appears the first time the player enters this trigger.
        /// </summary>
        public bool showOnlyOnce = true;

        [Tooltip("If true, the question mark object disappears after the tutorial is shown.")]
        /// <summary>
        /// If true, this trigger object is hidden after the popup appears.
        /// </summary>
        public bool hideAfterUse = false;

        private bool hasShown = false;

        /// <summary>
        /// Unity physics event called when another 2D collider enters this trigger.
        /// </summary>
        /// <param name="other">The collider that entered the tutorial trigger.</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (showOnlyOnce && hasShown)
            {
                return;
            }

            if (IsPlayer(other))
            {
                Debug.Log("Tutorial triggered: " + tutorialTitle);
                ShowTutorial();
            }
        }

        /// <summary>
        /// Checks whether the collider belongs to the real player.
        /// </summary>
        /// <param name="other">The collider that entered the tutorial trigger.</param>
        /// <returns>True when the collider is on the Player or one of its child objects.</returns>
        private static bool IsPlayer(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.CompareTag("Player"))
            {
                return true;
            }

            Transform root = other.transform.root;
            if (root != null && root.CompareTag("Player"))
            {
                return true;
            }

            return other.GetComponentInParent<PlayerController2D>() != null;
        }

        /// <summary>
        /// Shows this trigger's configured tutorial message.
        /// </summary>
        /// <remarks>
        /// Finds a TutorialPopupManager if one was not assigned in the Inspector.
        /// </remarks>
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
