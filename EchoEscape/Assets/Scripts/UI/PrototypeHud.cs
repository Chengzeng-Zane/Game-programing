using System.Text;
using UnityEngine;

namespace EchoEscape
{
    public class PrototypeHud : MonoBehaviour
    {
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle objectiveStyle;
        private GUIStyle hintStyle;

        private void OnGUI()
        {
            EnsureStyles();

            EchoEscapeGameManager manager = EchoEscapeGameManager.Instance;
            if (manager == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(14f, 14f, 620f, 300f), panelStyle);
            GUILayout.Label("Echo Escape Training Room", titleStyle);
            GUILayout.Label("Move: A/D or Arrow Keys | Jump: Space/W | Record: Q | Replay Echo: E | Open Chest: F | Restart: R", hintStyle);
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
                recordingText = manager.recorder.IsRecording
                    ? $"Recording: {Mathf.RoundToInt(manager.recorder.RecordingProgress * 100f)}%"
                    : manager.recorder.HasRecording ? "Recording: saved" : "Recording: none";
            }

            GUILayout.Label(recordingText, bodyStyle);
            GUILayout.Label($"Deaths: {manager.DeathCount}", bodyStyle);
            GUILayout.Label($"Pending loot: {FormatLoot(manager.PendingLoot)}", bodyStyle);
            GUILayout.Label($"Secured loot: {FormatLoot(manager.SecuredLoot)}", bodyStyle);
            GUILayout.EndArea();

            if (tutorial != null)
            {
                float width = Mathf.Min(780f, Screen.width - 28f);
                GUILayout.BeginArea(new Rect(14f, Screen.height - 132f, width, 116f), panelStyle);
                GUILayout.Label(tutorial.ProgressLabel + "  CURRENT GOAL", hintStyle);
                GUILayout.Label(tutorial.Objective, objectiveStyle);
                GUILayout.Label(tutorial.Hint, bodyStyle);
                GUILayout.EndArea();
            }
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
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

        private string FormatLoot(System.Collections.Generic.IReadOnlyList<LootDefinition> loot)
        {
            if (loot == null || loot.Count == 0)
            {
                return "none";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < loot.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(loot[i].itemName);
            }

            return builder.ToString();
        }
    }
}
