using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    public class ActionRecorder : MonoBehaviour
    {
        public float maxRecordSeconds = 5f;
        public Color echoColor = new Color(0.8f, 0.95f, 1f, 0.8f);

        public bool IsRecording { get; private set; }
        public bool HasRecording => frames.Count > 1;
        public float RecordingProgress => IsRecording ? Mathf.Clamp01(recordTimer / maxRecordSeconds) : 0f;
        public EchoReplayController ActiveEcho => activeEcho;

        private readonly List<RecordingFrame> frames = new List<RecordingFrame>();
        private float recordTimer;
        private EchoReplayController activeEcho;
        private PlayerController2D player;
        private static Sprite echoSquareSprite;

        private void Awake()
        {
            player = GetComponent<PlayerController2D>();
        }

        private void FixedUpdate()
        {
            if (!IsRecording)
            {
                return;
            }

            frames.Add(new RecordingFrame(transform.position, recordTimer, player == null || player.FacingRight));
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
                Debug.Log("No recording available.");
                return;
            }

            DestroyActiveEcho();

            GameObject echoObject = new GameObject("EchoReplay");
            echoObject.transform.position = frames[0].position;
            TrySetEchoTag(echoObject);

            Rigidbody2D body = echoObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            SpriteRenderer renderer = echoObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetEchoSquareSprite();
            renderer.color = echoColor;
            renderer.sortingOrder = 6;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(0.65f, 1.5f);

            BoxCollider2D collider = echoObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.75f, 1.9f);
            collider.offset = new Vector2(0f, -0.25f);

            activeEcho = echoObject.AddComponent<EchoReplayController>();
            activeEcho.Load(frames);

            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus("Echo replay started. Use it to hold switches or time hazards.");
            Debug.Log("Echo replaying.");
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
            Debug.Log("Recording...");
        }

        private void StopRecording()
        {
            IsRecording = false;
            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus($"Recording saved: {frames.Count} frame(s). Press E to replay your echo.");
            Debug.Log("Recording stopped. Press E to replay Echo.");
        }

        private void TrySetEchoTag(GameObject echoObject)
        {
            try
            {
                echoObject.tag = "Echo";
            }
            catch (UnityException)
            {
                Debug.LogWarning("Echo tag is missing. The Echo can still press plates by component detection, but add an Echo tag in Project Settings if you want tag-based filtering.");
            }
        }

        private static Sprite GetEchoSquareSprite()
        {
            if (echoSquareSprite != null)
            {
                return echoSquareSprite;
            }

            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "EchoSquareTexture"
            };

            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            echoSquareSprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
            echoSquareSprite.name = "EchoSquareSprite";
            return echoSquareSprite;
        }
    }
}
