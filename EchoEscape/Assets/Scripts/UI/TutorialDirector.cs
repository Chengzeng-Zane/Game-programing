using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Tracks the tutorial objective state for the older prototype HUD flow.
    /// </summary>
    /// <remarks>
    /// Attach this script to the Game Manager when using the full prototype tutorial flow.
    /// It watches Player, ActionRecorder, PressurePlate, Door, Chest, and Goal state,
    /// then exposes readable objective text for PrototypeHud.
    /// </remarks>
    public class TutorialDirector : MonoBehaviour
    {
        /// <summary>
        /// Ordered tutorial steps used by the prototype objective tracker.
        /// </summary>
        private enum TutorialStep
        {
            LearnMovement,
            ReachPlate,
            StartRecording,
            SaveRecording,
            ReplayEcho,
            PassDoor,
            OpenChest,
            Extract
        }

        /// <summary>
        /// Text showing the current tutorial step number.
        /// </summary>
        public string ProgressLabel => $"Step {(int)step + 1}/{totalSteps}";

        /// <summary>
        /// Current tutorial title displayed by PrototypeHud.
        /// </summary>
        public string Title { get; private set; } = "Tutorial loading";

        /// <summary>
        /// Current objective text displayed by PrototypeHud.
        /// </summary>
        public string Objective { get; private set; } = "Preparing the first room.";

        /// <summary>
        /// Short hint text displayed by PrototypeHud.
        /// </summary>
        public string Hint { get; private set; } = "Press Play again if Unity is still compiling scripts.";

        private const int totalSteps = 8;
        private TutorialStep step;
        private EchoEscapeGameManager manager;
        private PlayerController2D player;
        private ActionRecorder recorder;
        private PressurePlate plate;
        private Door door;
        private Vector3 startPosition;
        private bool chestOpened;
        private float stepStartedAt;

        /// <summary>
        /// Unity event method called before the first frame update.
        /// </summary>
        /// <remarks>
        /// Finds gameplay objects and starts the tutorial state machine at the movement step.
        /// </remarks>
        private void Start()
        {
            manager = EchoEscapeGameManager.Instance;
            player = manager != null ? manager.player : FindObjectOfType<PlayerController2D>();
            recorder = manager != null ? manager.recorder : FindObjectOfType<ActionRecorder>();
            plate = FindObjectOfType<PressurePlate>();
            door = FindObjectOfType<Door>();

            if (player != null)
            {
                startPosition = player.transform.position;
            }

            SetStep(TutorialStep.LearnMovement);
        }

        /// <summary>
        /// Unity event method called once per frame.
        /// </summary>
        /// <remarks>
        /// Checks current player and mechanic state to advance tutorial objectives.
        /// </remarks>
        private void Update()
        {
            if (manager == null || player == null || recorder == null)
            {
                return;
            }

            switch (step)
            {
                case TutorialStep.LearnMovement:
                    if (MovedFromStart() || PressedMovementInput())
                    {
                        SetStep(TutorialStep.ReachPlate);
                    }
                    break;

                case TutorialStep.ReachPlate:
                    if (plate != null && plate.IsPressed)
                    {
                        SetStep(TutorialStep.StartRecording);
                    }
                    break;

                case TutorialStep.StartRecording:
                    if (recorder.IsRecording)
                    {
                        SetStep(TutorialStep.SaveRecording);
                    }
                    break;

                case TutorialStep.SaveRecording:
                    if (recorder.HasRecording && !recorder.IsRecording)
                    {
                        SetStep(TutorialStep.ReplayEcho);
                    }
                    break;

                case TutorialStep.ReplayEcho:
                    if (recorder.ActiveEcho != null || (door != null && door.IsOpen))
                    {
                        SetStep(TutorialStep.PassDoor);
                    }
                    break;

                case TutorialStep.PassDoor:
                    if (door != null && door.IsOpen && player.transform.position.x > door.transform.position.x + 0.9f)
                    {
                        SetStep(TutorialStep.OpenChest);
                    }
                    break;

                case TutorialStep.OpenChest:
                    if (chestOpened || manager.PendingLootCount > 0)
                    {
                        SetStep(TutorialStep.Extract);
                    }
                    break;

                case TutorialStep.Extract:
                    if (manager.HasWon)
                    {
                        Title = "Tutorial complete";
                        Objective = "You extracted and banked your loot.";
                        Hint = "The core loop works: record yourself, use the echo, take risk, then escape safely.";
                    }
                    break;
            }
        }

        /// <summary>
        /// Notifies the tutorial that a chest has been opened.
        /// </summary>
        /// <remarks>
        /// Called by Chest.Open so the tutorial can advance to extraction.
        /// </remarks>
        public void NotifyChestOpened()
        {
            chestOpened = true;
        }

        /// <summary>
        /// Changes the active tutorial step and updates title, objective, and hint text.
        /// </summary>
        /// <param name="nextStep">The tutorial step to activate.</param>
        private void SetStep(TutorialStep nextStep)
        {
            step = nextStep;
            stepStartedAt = Time.time;

            switch (step)
            {
                case TutorialStep.LearnMovement:
                    Title = "Learn the body";
                    Objective = "Move the stick figure and make one jump.";
                    Hint = "Use A/D or Arrow Keys to move. Use Space, W, or Up Arrow to jump.";
                    break;

                case TutorialStep.ReachPlate:
                    Title = "Find the first lock";
                    Objective = "Stand on the yellow pressure plate.";
                    Hint = "The red door reacts to the plate. In this game, the level often needs two versions of you.";
                    break;

                case TutorialStep.StartRecording:
                    Title = "Record your past self";
                    Objective = "While standing on or near the plate, press Q to start recording.";
                    Hint = "Your movement is now being taped. Make a simple action the echo can repeat.";
                    break;

                case TutorialStep.SaveRecording:
                    Title = "Make a useful tape";
                    Objective = "Stay on the plate for a moment, then press Q again to stop recording.";
                    Hint = "The best first recording is boring on purpose: walk onto the plate and hold it.";
                    break;

                case TutorialStep.ReplayEcho:
                    Title = "Replay the echo";
                    Objective = "Step away from the plate, then press E to spawn your echo.";
                    Hint = "The blue echo repeats the saved route and can hold the plate while you move ahead.";
                    break;

                case TutorialStep.PassDoor:
                    Title = "Work with yourself";
                    Objective = "Use the echo to keep the door open, then pass through the red door.";
                    Hint = "If the timing is wrong, record a cleaner route and try the echo again.";
                    break;

                case TutorialStep.OpenChest:
                    Title = "Take the risk";
                    Objective = "Find a golden chest and press F beside it.";
                    Hint = "Chest loot is temporary. If you die before extracting, the new item is lost.";
                    break;

                case TutorialStep.Extract:
                    Title = "Bank the reward";
                    Objective = "Avoid the red hazard and reach the green exit.";
                    Hint = manager.PendingLootCount > 0
                        ? "Reach the exit to secure your loot."
                        : "You can still finish, but opening a chest first demonstrates the risk-reward rule.";
                    break;
            }

            manager?.UpdateStatus($"{Title}: {Objective}");
        }

        /// <summary>
        /// Checks whether the player has moved far enough away from the starting position.
        /// </summary>
        /// <returns>True if the player has moved from the start; otherwise false.</returns>
        private bool MovedFromStart()
        {
            return Vector2.Distance(player.transform.position, startPosition) > 0.6f;
        }

        /// <summary>
        /// Checks whether the player has pressed movement or jump input after a short delay.
        /// </summary>
        /// <returns>True if movement or jump input was detected; otherwise false.</returns>
        private bool PressedMovementInput()
        {
            return Time.time - stepStartedAt > 0.2f &&
                (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
                 Input.GetKeyDown(KeyCode.Space) ||
                 Input.GetKeyDown(KeyCode.W) ||
                 Input.GetKeyDown(KeyCode.UpArrow));
        }
    }
}
