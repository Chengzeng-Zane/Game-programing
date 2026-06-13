using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：项目的资源加载工具。它把 Resources 目录里的音效、音乐、角色帧、平台图块等素材集中在一个地方读取，避免其他脚本到处硬写路径。
    /// 玩法逻辑：玩家、Echo、敌人动画都需要从素材表里切帧；关卡背景、平台视觉和音频也需要统一加载。这个脚本不控制玩法，只负责把资源变成可用的 AudioClip 或 Sprite。
    /// 协作关系：PlayerAnimationController、EchoAnimationController、EnemyAnimationController、PrototypeVisualSkinner、PrototypeAudio、BackgroundMusic 都会调用它。
    /// </summary>
    public static class PixelArtLibrary
    {
        private const float PixelsPerUnit = 16f;
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite[]> FrameCache = new Dictionary<string, Sprite[]>();
        public static Sprite KnightIdle => KnightIdleFrames[0];
        public static Sprite KnightRun => KnightRunFrames[0];
        public static Sprite[] KnightIdleFrames => FramesFromTopLeft("Sprites/knight", "knight-idle", 0, 0, 4, 32, 32, 32);
        public static Sprite[] KnightRunFrames => FramesFromTopLeft("Sprites/knight", "knight-run", 0, 64, 8, 32, 32, 32);
        public static Sprite GroundTile => FromTopLeft("Sprites/world_tileset", "ground-tile", 16, 0, 16, 16);
        public static Sprite StoneTile => FromTopLeft("Sprites/world_tileset", "stone-tile", 80, 0, 16, 16);
        public static Sprite DoorTile => FromTopLeft("Sprites/world_tileset", "door-tile", 112, 48, 16, 32);
        public static Sprite PlateTile => FromTopLeft("Sprites/coin", "plate-tile", 0, 0, 16, 16);
        public static Sprite ChestTile => FromTopLeft("Sprites/world_tileset", "chest-tile", 96, 48, 16, 16);
        public static Sprite CoinTile => FromTopLeft("Sprites/coin", "coin-tile", 0, 0, 16, 16);
        public static Sprite HazardSlime => FromTopLeft("Sprites/slime_green", "slime-green", 0, 0, 24, 24);
        public static Sprite ExitGem => FromTopLeft("Sprites/fruit", "exit-gem", 0, 0, 16, 16);
        /// <summary>
        /// 从 Resources 或传入数据中加载需要的资源，并转换成脚本可直接使用的对象。
        /// </summary>
        /// <param name="fileName">资源文件名，不包含路径前缀。</param>
        /// <returns>返回加载到的音频资源；资源不存在时可能返回 null。</returns>
        public static AudioClip LoadSound(string fileName)
        {
            return Resources.Load<AudioClip>($"BrackeysPlatformer/Audio/Sounds/{fileName}");
        }
        /// <summary>
        /// 从 Resources 或传入数据中加载需要的资源，并转换成脚本可直接使用的对象。
        /// </summary>
        /// <param name="fileName">资源文件名，不包含路径前缀。</param>
        /// <returns>返回加载到的音频资源；资源不存在时可能返回 null。</returns>
        public static AudioClip LoadMusic(string fileName)
        {
            return Resources.Load<AudioClip>($"BrackeysPlatformer/Audio/Music/{fileName}");
        }
        /// <summary>
        /// 从一张大图里连续切出多帧 Sprite，例如角色跑步帧。切出的结果会缓存，避免重复创建 Sprite。
        /// </summary>
        /// <param name="texturePath">Resources 中纹理大图的路径。</param>
        /// <param name="key">缓存 Sprite 时使用的唯一 key，避免重复切图。</param>
        /// <param name="x">在纹理图上的横向像素坐标。</param>
        /// <param name="y">在纹理图上的纵向像素坐标。</param>
        /// <param name="count">需要连续读取的帧数量。</param>
        /// <param name="width">切图或 UI 元素的宽度。</param>
        /// <param name="height">切图或 UI 元素的高度。</param>
        /// <param name="xStep">连续切帧时每一帧在横向移动的像素距离。</param>
        /// <returns>返回一组 Sprite 动画帧；资源不存在时可能是空数组。</returns>
        private static Sprite[] FramesFromTopLeft(string texturePath, string key, int x, int y, int count, int width, int height, int xStep)
        {
            string cacheKey = texturePath + ":" + key + ":frames";
            if (FrameCache.TryGetValue(cacheKey, out Sprite[] frames))
            {
                // 同一组动画帧已经切过时直接复用缓存。
                return frames;
            }

            frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                // 每一帧在 x 方向移动 xStep 个像素，适合横向排列的 spritesheet。
                frames[i] = FromTopLeft(texturePath, key + "-" + i, x + (i * xStep), y, width, height);
            }

            FrameCache[cacheKey] = frames;
            return frames;
        }
        /// <summary>
        /// 从纹理左上角坐标切出一个 Sprite。Unity 的 Rect 坐标从左下角算，所以这里会转换 y 坐标。
        /// </summary>
        /// <param name="texturePath">Resources 中纹理大图的路径。</param>
        /// <param name="key">缓存 Sprite 时使用的唯一 key，避免重复切图。</param>
        /// <param name="x">在纹理图上的横向像素坐标。</param>
        /// <param name="y">在纹理图上的纵向像素坐标。</param>
        /// <param name="width">切图或 UI 元素的宽度。</param>
        /// <param name="height">切图或 UI 元素的高度。</param>
        /// <returns>返回加载或生成的 Sprite；资源不存在时可能返回 null。</returns>
        private static Sprite FromTopLeft(string texturePath, string key, int x, int y, int width, int height)
        {
            string cacheKey = texturePath + ":" + key;
            if (SpriteCache.TryGetValue(cacheKey, out Sprite sprite))
            {
                // 单张 Sprite 已经创建过时直接复用，减少运行时内存和重复切图。
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>("BrackeysPlatformer/" + texturePath);
            if (texture == null)
            {
                Debug.LogWarning("Missing pixel art texture: " + texturePath);
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            // 传入的 y 是美术图常用的左上角坐标，Sprite.Create 需要左下角坐标。
            Rect rect = new Rect(x, texture.height - y - height, width, height);
            sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = key;
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }
    }
}
