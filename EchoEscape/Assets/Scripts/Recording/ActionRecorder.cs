using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：Echo 录制系统。玩家按 Q 录制自己的移动，再按 E 生成 Echo，让 Echo 复现刚才的行动路线。
    /// 玩法逻辑：录制期间每个 FixedUpdate 保存 RecordingFrame，包括位置、时间、朝向、是否反重力；播放时创建一个 EchoReplay GameObject，给它 Rigidbody2D、Trigger Collider、EchoAnimationController 和 EchoReplayController。Echo 回放结束会停在最后位置，常用于压住压力板。
    /// 协作关系：读取 PlayerController2D 和 GravityFlipController；生成 EchoReplayController；Echo 可以触发 PressurePlate；PrototypeAudio 播放录制反馈。
    /// </summary>
    public class ActionRecorder : MonoBehaviour
    {
        public float maxRecordSeconds = 5f;
        public Color echoColor = new Color(0.3f, 0.9f, 1f, 0.55f);
        public bool IsRecording { get; private set; }
        public bool HasRecording => frames.Count > 1;
        public float RecordingProgress => IsRecording ? Mathf.Clamp01(recordTimer / maxRecordSeconds) : 0f;
        public EchoReplayController ActiveEcho => activeEcho;

        private readonly List<RecordingFrame> frames = new List<RecordingFrame>();
        private float recordTimer;
        private EchoReplayController activeEcho;
        private PlayerController2D player;
        private GravityFlipController gravityFlip;
        /// <summary>
        /// 缓存玩家控制器和重力翻转控制器。录制帧需要知道玩家朝向和是否反重力，所以这里先准备引用。
        /// </summary>
        private void Awake()
        {
            player = GetComponent<PlayerController2D>();
            gravityFlip = GetComponent<GravityFlipController>();
        }
        /// <summary>
        /// 录制进行中时，每个物理帧保存玩家当前位置、录制时间、面朝方向和 Gravity Flip 状态。用 FixedUpdate 是为了让 Echo 回放和物理移动节奏一致。
        /// </summary>
        private void FixedUpdate()
        {
            if (!IsRecording)
            {
                return;
            }

            // Echo 不只是记录位置，还要记录朝向和重力状态；否则回放时视觉和倒挂状态会对不上。
            bool facingRight = player == null || player.FacingRight;
            bool isGravityFlipped = gravityFlip != null && gravityFlip.IsFlipped;
            frames.Add(new RecordingFrame(transform.position, recordTimer, facingRight, isGravityFlipped));
            recordTimer += Time.fixedDeltaTime;

            if (recordTimer >= maxRecordSeconds)
            {
                // 达到最大录制时长自动停止，防止列表无限增长，也让谜题时间窗口可控。
                StopRecording();
            }
        }
        /// <summary>
        /// Q 键调用的录制开关。如果当前正在录制，就停止并保留帧；如果没有录制，就清空旧数据并开始新录制。
        /// </summary>
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
        /// <summary>
        /// E 键调用的 Echo 回放入口。它会销毁旧 Echo，创建新的 EchoReplay 对象，添加 Rigidbody2D、Trigger Collider、EchoAnimationController 和 EchoReplayController，然后把录制帧交给它回放。
        /// </summary>
        public void PlayEcho()
        {
            if (!HasRecording)
            {
                // 至少需要两帧才能形成可见路线；没有录制时只提示玩家，不生成空 Echo。
                EchoEscapeGameManager.Instance?.UpdateStatus("Record a movement first with Q, then press E to replay it.");
                Debug.Log("No recording available.");
                return;
            }

            // 每次播放前销毁旧 Echo，让场景里只有一个可控的回放体，避免多个 Echo 同时压机关。
            DestroyActiveEcho();

            GameObject echoObject = new GameObject("EchoReplay");
            echoObject.transform.position = frames[0].position;
            TrySetEchoTag(echoObject);

            Rigidbody2D body = echoObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Echo 用单独的视觉子物体，这样逻辑碰撞体和角色显示可以分别调整大小/位置。
            GameObject visualObject = new GameObject("EchoVisual");
            visualObject.transform.SetParent(echoObject.transform, false);
            visualObject.transform.localPosition = new Vector3(0f, -0.58f, -0.03f);
            visualObject.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.color = echoColor;
            renderer.sortingOrder = 5;

            Animator animator = visualObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("RubyPlayer");
            visualObject.AddComponent<EchoAnimationController>();

            BoxCollider2D collider = echoObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.75f, 1.5f);
            collider.offset = new Vector2(0f, -0.33f);

            // 真正的路径回放交给 EchoReplayController，它会按 RecordingFrame 一帧帧移动。
            activeEcho = echoObject.AddComponent<EchoReplayController>();
            activeEcho.Load(frames);

            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus("Echo replay started. Use it to hold switches or time hazards.");
            Debug.Log("Echo replaying.");
        }
        /// <summary>
        /// 销毁当前已经存在的 Echo。这样玩家每次播放新 Echo 时，场景里只保留一个回放体，避免多个 Echo 同时压机关导致关卡逻辑混乱。
        /// </summary>
        public void DestroyActiveEcho()
        {
            if (activeEcho == null)
            {
                return;
            }

            Destroy(activeEcho.gameObject);
            activeEcho = null;
        }
        /// <summary>
        /// 开始录制一段新 Echo。它会清空旧帧、重置计时器、设置 IsRecording，并播放提示音/状态文字告诉玩家已经开始录制。
        /// </summary>
        private void StartRecording()
        {
            // 新录制会覆盖旧录制，保证玩家按 E 播放的是最近一次尝试的路线。
            frames.Clear();
            recordTimer = 0f;
            IsRecording = true;
            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus("Recording. Move, jump, or stand on a plate, then press Q to stop.");
            Debug.Log("Recording...");
        }
        /// <summary>
        /// 结束录制。它停止继续写入 RecordingFrame，并更新 UI 状态，之后玩家可以按 E 生成 Echo 回放。
        /// </summary>
        private void StopRecording()
        {
            IsRecording = false;
            EchoEscapeGameManager.Instance?.AudioService?.PlayTap();
            EchoEscapeGameManager.Instance?.UpdateStatus($"Recording saved: {frames.Count} frame(s). Press E to replay your echo.");
            Debug.Log("Recording stopped. Press E to replay Echo.");
        }
        /// <summary>
        /// 尝试把 Echo 设置为 Echo 标签。这样敌人、危险区和其他逻辑可以识别它不是玩家，避免 Echo 触发玩家死亡。
        /// </summary>
        /// <param name="echoObject">echoObject 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private void TrySetEchoTag(GameObject echoObject)
        {
            try
            {
                // Echo 标签用于敌人、死亡区等逻辑过滤，避免 Echo 被当成真正玩家。
                echoObject.tag = "Echo";
            }
            catch (UnityException)
            {
                // 如果项目 Tag 列表里没有 Echo，组件检测仍然能工作，所以这里只警告不终止游戏。
                Debug.LogWarning("Echo tag is missing. The Echo can still press plates by component detection, but add an Echo tag in Project Settings if you want tag-based filtering.");
            }
        }

    }
}
