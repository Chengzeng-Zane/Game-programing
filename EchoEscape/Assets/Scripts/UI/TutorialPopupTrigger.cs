using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Question Mark Tutorial Trigger. After the player enters the question mark range, it will open the corresponding teaching pop-up window.
/// Gameplay logic: When triggering, first confirm that the object is a real player, and then determine whether it has been displayed; if the conditions are met, tutorialTitle and tutorialMessage hand over TutorialPopupManager。
/// Collaborates with: and TutorialPopupManager Cooperate; hideAfterUse You can hide the question mark prompt after use.
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
/// 2D Trigger Called when first entering. Here, it is decided whether to trigger teaching, mechanism, treasure chest, death or clearance based on the entering object.
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (showOnlyOnce && hasShown)
            {
// Tutorials that are only displayed once are ignored immediately after being triggered, to avoid repeated pop-ups of the player's window as they move back and forth.
                return;
            }

            if (IsPlayer(other))
            {
// Only when real players enter the question mark range will the tutorial be displayed. Echo Or the enemy will not trigger the pop-up window.
                ShowTutorial();
            }
        }
        /// <summary>
/// Determine whether it has entered the question mark range Collider Whether it is a real player. support Player root object or player sub-object Collider。
        /// </summary>
/// <param name="other">Unity incoming 2D Collider, representing an entry trigger or a detected object. The function will use it to determine whether the object is a player or not. Echo, enemy or agency. </param>
/// <returns>return true Indicates that the condition is established or the operation is successful and returns false Indicates that the condition is not met or the operation failed. </returns>
        private static bool IsPlayer(Collider2D other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.CompareTag("Player"))
            {
// standard Player Tag is the fastest path.
                return true;
            }

            Transform root = other.transform.root;
            if (root != null && root.CompareTag("Player"))
            {
// Some triggers encounter player sub-objects, with the root object Player Tag Also considered a player.
                return true;
            }

// Use last PlayerController2D Stay safe, avoid Tag The tutorial will not work if the configuration is incomplete.
            return other.GetComponentInParent<PlayerController2D>() != null;
        }
        /// <summary>
/// Show correspondence UI or visual status, usually used for pop-up windows, loot Hints, death prompts or settlement feedback.
        /// </summary>
        private void ShowTutorial()
        {
            if (popupManager == null)
            {
// The pop-up manager is automatically found when the scene does not manually drag the reference.
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
// The question mark prompt can be hidden after being displayed to prevent players from thinking that the same prompt can be interacted with repeatedly.
                gameObject.SetActive(false);
            }
        }
    }
}
