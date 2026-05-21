using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Builds a simple line-rendered stick figure placeholder.
    /// </summary>
    /// <remarks>
    /// Attach this script to an early prototype player or Echo object when pixel art is not available.
    /// Current pixel-art visuals disable these LineRenderer children through PixelCharacterVisual.
    /// </remarks>
    public class StickFigureVisual : MonoBehaviour
    {
        /// <summary>
        /// Color applied to all stick figure line renderers.
        /// </summary>
        public Color color = Color.white;

        /// <summary>
        /// Width used by each stick figure line renderer.
        /// </summary>
        public float lineWidth = 0.07f;

        /// <summary>
        /// Unity event method called when this visual component is created.
        /// </summary>
        /// <remarks>
        /// Builds the stick figure once if no child line objects already exist.
        /// </remarks>
        private void Awake()
        {
            Build();
        }

        /// <summary>
        /// Changes the color of all existing stick figure line renderers.
        /// </summary>
        /// <param name="newColor">New color to apply to the stick figure.</param>
        public void SetColor(Color newColor)
        {
            color = newColor;
            LineRenderer[] lines = GetComponentsInChildren<LineRenderer>();
            foreach (LineRenderer line in lines)
            {
                line.startColor = color;
                line.endColor = color;
            }
        }

        /// <summary>
        /// Creates line-renderer children for body, arms, legs, and head.
        /// </summary>
        private void Build()
        {
            if (transform.childCount > 0)
            {
                return;
            }

            CreateLine("Body", new[] { new Vector3(0f, 0.25f, -0.1f), new Vector3(0f, -0.45f, -0.1f) });
            CreateLine("LeftArm", new[] { new Vector3(0f, 0.05f, -0.1f), new Vector3(-0.35f, -0.15f, -0.1f) });
            CreateLine("RightArm", new[] { new Vector3(0f, 0.05f, -0.1f), new Vector3(0.35f, -0.15f, -0.1f) });
            CreateLine("LeftLeg", new[] { new Vector3(0f, -0.45f, -0.1f), new Vector3(-0.25f, -0.95f, -0.1f) });
            CreateLine("RightLeg", new[] { new Vector3(0f, -0.45f, -0.1f), new Vector3(0.25f, -0.95f, -0.1f) });
            CreateHead();
        }

        /// <summary>
        /// Creates one named line-renderer child from local-space points.
        /// </summary>
        /// <param name="lineName">Name of the created line object.</param>
        /// <param name="points">Local-space points used by the LineRenderer.</param>
        private void CreateLine(string lineName, Vector3[] points)
        {
            GameObject child = new GameObject(lineName);
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = color;
            line.endColor = color;
            line.material = PrototypeFactory.CreateMaterial(color);
            line.numCapVertices = 4;
        }

        /// <summary>
        /// Creates a circular line-rendered head for the stick figure.
        /// </summary>
        private void CreateHead()
        {
            GameObject child = new GameObject("Head");
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 24;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = color;
            line.endColor = color;
            line.material = PrototypeFactory.CreateMaterial(color);
            line.numCapVertices = 4;

            Vector3[] points = new Vector3[line.positionCount];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = (Mathf.PI * 2f * i) / points.Length;
                points[i] = new Vector3(Mathf.Cos(angle) * 0.22f, 0.55f + Mathf.Sin(angle) * 0.22f, -0.1f);
            }

            line.SetPositions(points);
        }
    }
}
