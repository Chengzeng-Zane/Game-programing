using UnityEngine;

namespace EchoEscape
{
    public class TutorialDirector : MonoBehaviour
    {
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

        public string ProgressLabel => $"Step {(int)step + 1}/{totalSteps}";
        public string Title { get; private set; } = "Tutorial loading";
        public string Objective { get; private set; } = "Preparing the first room.";
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

        public void NotifyChestOpened()
        {
            chestOpened = true;
        }

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
                    Hint = "If the timing is wrong, press R to restart and record a cleaner route.";
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

        private bool MovedFromStart()
        {
            return Vector2.Distance(player.transform.position, startPosition) > 0.6f;
        }

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
