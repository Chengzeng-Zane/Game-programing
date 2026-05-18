using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    public class ActionRecorder : MonoBehaviour
    {
        public float maxRecordSeconds = 5f;
        public Color echoColor = new Color(0.1f, 0.95f, 1f);

        public bool IsRecording { get; private set; }
        public bool HasRecording => frames.Count > 1;
        public float RecordingProgress => IsRecording ? Mathf.Clamp01(recordTimer / maxRecordSeconds) : 0f;
        public EchoReplayController ActiveEcho => activeEcho;

        private readonly List<RecordingFrame> frames = new List<RecordingFrame>();
        private float recordTimer;
        private EchoReplayController activeEcho;

        private void FixedUpdate()
        {
            if (!IsRecording)
            {
                return;
            }

            frames.Add(new RecordingFrame(transform.position));
            recordTimer += Time.fixedDeltaTime;

            if (recordTimer >= maxRecordSeconds)
            {
                StopRecording();
            }
        }

        public void ToggleRecording()
        {
            if (IsRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        public void PlayEcho()
        {
            if (!HasRecording)
            {
                EchoEscapeGameManager.Instance?.UpdateStatus("Record a movement first with Q, then press E to replay it.");
                return;
            }

            DestroyActiveEcho();

            GameObject echoObject = new GameObject("Echo Replay");
            echoObject.transform.position = frames[0].position;

            Rigidbody2D body = echoObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D collider = echoObject.AddComponent<CapsuleCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.65f, 1.8f);
            collider.offset = new Vector2(0f, -0.15f);

            PixelCharacterVisual visual = echoObject.AddComponent<PixelCharacterVisual>();
            visual.SetStyle(true, new Color(echoColor.r, echoColor.g, echoColor.b, 0.72f));

            activeEcho = echoObject.AddComponent<EchoReplayController>();
            activeEcho.Load(frames);

            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus("Echo replay started. Use it to hold switches or time hazards.");
        }

        public void DestroyActiveEcho()
        {
            if (activeEcho == null)
            {
                return;
            }

            Destroy(activeEcho.gameObject);
            activeEcho = null;
        }

        private void StartRecording()
        {
            frames.Clear();
            recordTimer = 0f;
            IsRecording = true;
            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus("Recording. Move, jump, or stand on a plate, then press Q to stop.");
        }

        private void StopRecording()
        {
            IsRecording = false;
            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus($"Recording saved: {frames.Count} frame(s). Press E to replay your echo.");
        }
    }
}
