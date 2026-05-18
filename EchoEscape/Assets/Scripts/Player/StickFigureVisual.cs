using UnityEngine;

namespace EchoEscape
{
    public class StickFigureVisual : MonoBehaviour
    {
        public Color color = Color.white;
        public float lineWidth = 0.07f;

        private void Awake()
        {
            Build();
        }

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
