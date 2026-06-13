using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：原型 HUD。它用 OnGUI 显示当前教程目标、录制状态、pending loot 和 secured loot，方便测试玩法是否正常。
    /// 玩法逻辑：它只读取状态并画文字，不改变玩家、敌人、loot 或关卡流程。正式 UI 逐步完善后，这个脚本更多是调试和原型辅助。
    /// 协作关系：读取 EchoEscapeGameManager、TutorialDirector、ActionRecorder 的状态。
    /// </summary>
    public class PrototypeHud : MonoBehaviour
    {
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle objectiveStyle;
        private GUIStyle hintStyle;
        /// <summary>
        /// Unity 旧版即时 UI 绘制函数。这里用于显示原型 HUD 和调试文字。
        /// </summary>
        private void OnGUI()
        {
            EnsureStyles();

            EchoEscapeGameManager manager = EchoEscapeGameManager.Instance;
            if (manager == null)
            {
                // 没有 GameManager 的场景不显示 HUD，避免测试场景报空引用。
                return;
            }

            // 左上角区域显示调试用核心状态：教程、录制、死亡次数。
            GUILayout.BeginArea(new Rect(14f, 14f, 620f, 300f), panelStyle);
            GUILayout.Label("Echo Escape Training Room", titleStyle);
            GUILayout.Label("Move: A/D or Arrow Keys | Jump: Space/W | Attack: J | Record: Q | Replay Echo: E | Open Chest: F | Restart: R", hintStyle);
            GUILayout.Space(6f);

            TutorialDirector tutorial = manager.Tutorial;
            if (tutorial != null)
            {
                GUILayout.Label(tutorial.ProgressLabel, hintStyle);
                GUILayout.Label(tutorial.Title, objectiveStyle);
                GUILayout.Label(tutorial.Objective, bodyStyle);
                GUILayout.Label(tutorial.Hint, hintStyle);
            }
            else
            {
                GUILayout.Label(manager.StatusMessage, bodyStyle);
            }

            GUILayout.Space(6f);

            string recordingText = "Recording: idle";
            if (manager.recorder != null)
            {
                // 录制状态直接读取 ActionRecorder，方便测试 Q/E Echo 流程是否正常。
                recordingText = manager.recorder.IsRecording
                    ? $"Recording: {Mathf.RoundToInt(manager.recorder.RecordingProgress * 100f)}%"
                    : manager.recorder.HasRecording ? "Recording: saved" : "Recording: none";
            }

            GUILayout.Label(recordingText, bodyStyle);
            GUILayout.Label($"Deaths: {manager.DeathCount}", bodyStyle);
            GUILayout.EndArea();

            if (tutorial != null)
            {
                // 屏幕底部再重复显示当前目标，玩家不需要一直看左上角小字。
                float width = Mathf.Min(780f, Screen.width - 28f);
                GUILayout.BeginArea(new Rect(14f, Screen.height - 132f, width, 116f), panelStyle);
                GUILayout.Label(tutorial.ProgressLabel + "  CURRENT GOAL", hintStyle);
                GUILayout.Label(tutorial.Objective, objectiveStyle);
                GUILayout.Label(tutorial.Hint, bodyStyle);
                GUILayout.EndArea();
            }
        }
        /// <summary>
        /// 初始化 OnGUI 使用的文字样式。只创建一次，避免每帧重复分配 GUIStyle。
        /// </summary>
        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                // 已经创建过样式就直接复用。
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12),
                alignment = TextAnchor.UpperLeft
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            objectiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.88f, 0.35f) }
            };

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.86f, 0.95f) }
            };
        }

    }
}
