using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：教程步骤控制器。它按玩家行为推进教学目标，例如移动、跳跃、录制 Echo、开箱和到达出口。
    /// 玩法逻辑：每帧检查玩家是否移动、是否按过输入、Recorder 是否有录制、宝箱是否打开，然后更新当前教程步骤和提示文字。
    /// 协作关系：EchoEscapeGameManager 创建/持有它；PrototypeHud 显示它的目标；Chest 打开时通知它。
    /// </summary>
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
        /// <summary>
        /// Unity 在第一帧前调用。这里通常连接场景对象，启动初始 UI、教程或关卡流程。
        /// </summary>
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
        /// Unity 每帧调用。这里处理输入、计时器、UI 状态或非物理的实时刷新。
        /// </summary>
        private void Update()
        {
            if (manager == null || player == null || recorder == null)
            {
                // 教程依赖 GameManager、Player 和 Recorder；缺少任意一个就不推进，避免空引用报错。
                return;
            }

            switch (step)
            {
                case TutorialStep.LearnMovement:
                    if (MovedFromStart() || PressedMovementInput())
                    {
                        // 玩家移动或按下移动键后，说明已经理解基础操作，进入压力板目标。
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
                        // Q 开始录制后，教程要求玩家保存一段可用 Echo。
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
                        // Echo 已生成或门已打开，都说明玩家开始理解录制回放机关。
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
                        // 开箱后进入撤离目标，强调 pending loot 死亡会丢失。
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
        /// 宝箱打开时由 Chest 通知教程系统。这样教程不需要每帧搜索所有宝箱状态。
        /// </summary>
        public void NotifyChestOpened()
        {
            chestOpened = true;
        }
        /// <summary>
        /// 切换当前教程步骤，并更新 HUD 上显示的标题、目标和提示。
        /// </summary>
        /// <param name="nextStep">nextStep 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private void SetStep(TutorialStep nextStep)
        {
            step = nextStep;
            // 记录进入步骤的时间，用于过滤刚开始时的误输入。
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
        /// 计算并应用移动，让对象追踪目标、巡逻或回到出生点。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
        private bool MovedFromStart()
        {
            return Vector2.Distance(player.transform.position, startPosition) > 0.6f;
        }
        /// <summary>
        /// 判断玩家是否主动按过移动或跳跃输入，用于第一步教程推进。
        /// </summary>
        /// <returns>返回 true 表示条件成立或操作成功，返回 false 表示条件不满足或操作失败。</returns>
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
