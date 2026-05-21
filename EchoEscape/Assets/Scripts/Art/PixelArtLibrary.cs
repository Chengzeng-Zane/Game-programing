using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Loads and slices pixel-art sprites and audio from the Resources folder.
    /// </summary>
    /// <remarks>
    /// This static helper is used by visual scripts, menu scripts, and audio services.
    /// It caches generated sprites so repeated calls do not recreate the same sprite rectangles.
    /// </remarks>
    public static class PixelArtLibrary
    {
        private const float PixelsPerUnit = 16f;
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite[]> FrameCache = new Dictionary<string, Sprite[]>();

        /// <summary>
        /// First frame of the knight idle animation.
        /// </summary>
        public static Sprite KnightIdle => KnightIdleFrames[0];

        /// <summary>
        /// First frame of the knight run animation.
        /// </summary>
        public static Sprite KnightRun => KnightRunFrames[0];

        /// <summary>
        /// Frames sliced from the knight idle sprite sheet row.
        /// </summary>
        public static Sprite[] KnightIdleFrames => FramesFromTopLeft("Sprites/knight", "knight-idle", 0, 0, 4, 32, 32, 32);

        /// <summary>
        /// Frames sliced from the knight run sprite sheet row.
        /// </summary>
        public static Sprite[] KnightRunFrames => FramesFromTopLeft("Sprites/knight", "knight-run", 0, 64, 8, 32, 32, 32);

        /// <summary>
        /// Ground tile sprite used for prototype platforms.
        /// </summary>
        public static Sprite GroundTile => FromTopLeft("Sprites/world_tileset", "ground-tile", 16, 0, 16, 16);

        /// <summary>
        /// Stone tile sprite used for background or menu surfaces.
        /// </summary>
        public static Sprite StoneTile => FromTopLeft("Sprites/world_tileset", "stone-tile", 80, 0, 16, 16);

        /// <summary>
        /// Door tile sprite used for door visuals.
        /// </summary>
        public static Sprite DoorTile => FromTopLeft("Sprites/world_tileset", "door-tile", 112, 48, 16, 32);

        /// <summary>
        /// Coin sprite reused as a simple pressure plate icon.
        /// </summary>
        public static Sprite PlateTile => FromTopLeft("Sprites/coin", "plate-tile", 0, 0, 16, 16);

        /// <summary>
        /// Chest tile sprite used for loot chest visuals.
        /// </summary>
        public static Sprite ChestTile => FromTopLeft("Sprites/world_tileset", "chest-tile", 96, 48, 16, 16);

        /// <summary>
        /// Coin sprite used in the main menu decoration.
        /// </summary>
        public static Sprite CoinTile => FromTopLeft("Sprites/coin", "coin-tile", 0, 0, 16, 16);

        /// <summary>
        /// Slime sprite used as a readable hazard placeholder.
        /// </summary>
        public static Sprite HazardSlime => FromTopLeft("Sprites/slime_green", "slime-green", 0, 0, 24, 24);

        /// <summary>
        /// Fruit sprite reused as the exit marker gem.
        /// </summary>
        public static Sprite ExitGem => FromTopLeft("Sprites/fruit", "exit-gem", 0, 0, 16, 16);

        /// <summary>
        /// Loads a sound effect from the BrackeysPlatformer Resources folder.
        /// </summary>
        /// <param name="fileName">Audio file name without extension.</param>
        /// <returns>The loaded AudioClip, or null if the clip is missing.</returns>
        public static AudioClip LoadSound(string fileName)
        {
            return Resources.Load<AudioClip>($"BrackeysPlatformer/Audio/Sounds/{fileName}");
        }

        /// <summary>
        /// Loads a music clip from the BrackeysPlatformer Resources folder.
        /// </summary>
        /// <param name="fileName">Audio file name without extension.</param>
        /// <returns>The loaded AudioClip, or null if the clip is missing.</returns>
        public static AudioClip LoadMusic(string fileName)
        {
            return Resources.Load<AudioClip>($"BrackeysPlatformer/Audio/Music/{fileName}");
        }

        /// <summary>
        /// Slices multiple animation frames from a texture using top-left pixel coordinates.
        /// </summary>
        /// <param name="texturePath">Path inside the BrackeysPlatformer Resources folder.</param>
        /// <param name="key">Cache key prefix for the generated frames.</param>
        /// <param name="x">Left pixel coordinate of the first frame.</param>
        /// <param name="y">Top pixel coordinate of the first frame.</param>
        /// <param name="count">Number of frames to create.</param>
        /// <param name="width">Frame width in pixels.</param>
        /// <param name="height">Frame height in pixels.</param>
        /// <param name="xStep">Horizontal pixel distance between frames.</param>
        /// <returns>An array of generated Sprite frames.</returns>
        private static Sprite[] FramesFromTopLeft(string texturePath, string key, int x, int y, int count, int width, int height, int xStep)
        {
            string cacheKey = texturePath + ":" + key + ":frames";
            if (FrameCache.TryGetValue(cacheKey, out Sprite[] frames))
            {
                return frames;
            }

            frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = FromTopLeft(texturePath, key + "-" + i, x + (i * xStep), y, width, height);
            }

            FrameCache[cacheKey] = frames;
            return frames;
        }

        /// <summary>
        /// Slices one sprite from a texture using top-left pixel coordinates.
        /// </summary>
        /// <param name="texturePath">Path inside the BrackeysPlatformer Resources folder.</param>
        /// <param name="key">Cache key and sprite name.</param>
        /// <param name="x">Left pixel coordinate.</param>
        /// <param name="y">Top pixel coordinate.</param>
        /// <param name="width">Sprite width in pixels.</param>
        /// <param name="height">Sprite height in pixels.</param>
        /// <returns>The generated Sprite, or null if the texture is missing.</returns>
        private static Sprite FromTopLeft(string texturePath, string key, int x, int y, int width, int height)
        {
            string cacheKey = texturePath + ":" + key;
            if (SpriteCache.TryGetValue(cacheKey, out Sprite sprite))
            {
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

            Rect rect = new Rect(x, texture.height - y - height, width, height);
            sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = key;
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }
    }
}
