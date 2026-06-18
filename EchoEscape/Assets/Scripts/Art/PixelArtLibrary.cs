using System.Collections.Generic;
using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: The project’s resource loading tool. it puts Resources The sound effects, music, character frames, platform tiles and other materials in the directory are all read in one place, preventing other scripts from hard-writing paths everywhere.
/// Gameplay logic: players, Echo, enemy animations all need to be cut from the material table; the level background, platform visuals and audio also need to be loaded uniformly. This script does not control gameplay, it is only responsible for making resources available. AudioClip or Sprite。
/// Collaborates with: PlayerAnimationController、EchoAnimationController、EnemyAnimationController、PrototypeVisualSkinner、PrototypeAudio、BackgroundMusic will call it.
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
/// from Resources Or load the required resources from the incoming data and convert it into an object that can be used directly by the script.
        /// </summary>
/// <param name="fileName">Resource file name, without path prefix. </param>
/// <returns>Returns the loaded audio resource; may be returned if the resource does not exist null。</returns>
        public static AudioClip LoadSound(string fileName)
        {
            return Resources.Load<AudioClip>($"BrackeysPlatformer/Audio/Sounds/{fileName}");
        }
        /// <summary>
/// from Resources Or load the required resources from the incoming data and convert it into an object that can be used directly by the script.
        /// </summary>
/// <param name="fileName">Resource file name, without path prefix. </param>
/// <returns>Returns the loaded audio resource; may be returned if the resource does not exist null。</returns>
        public static AudioClip LoadMusic(string fileName)
        {
            return Resources.Load<AudioClip>($"BrackeysPlatformer/Audio/Music/{fileName}");
        }
        /// <summary>
/// Cut multiple frames continuously from a large image Sprite, such as character running frame. The cut results will be cached to avoid repeated creation. Sprite。
        /// </summary>
/// <param name="texturePath">Resources The path of the medium texture image. </param>
/// <param name="key">cache Sprite the only one used when key, to avoid repeated cutting. </param>
/// <param name="x">Horizontal pixel coordinates on the texture map. </param>
/// <param name="y">Vertical pixel coordinates on the texture map. </param>
/// <param name="count">The number of frames that need to be read continuously. </param>
/// <param name="width">cut picture or UI The width of the element. </param>
/// <param name="height">cut picture or UI The height of the element. </param>
/// <param name="xStep">The pixel distance that each frame moves horizontally when cutting continuously. </param>
/// <returns>Return a set Sprite Animation frames; may be an empty array if the resource does not exist. </returns>
        private static Sprite[] FramesFromTopLeft(string texturePath, string key, int x, int y, int count, int width, int height, int xStep)
        {
            string cacheKey = texturePath + ":" + key + ":frames";
            if (FrameCache.TryGetValue(cacheKey, out Sprite[] frames))
            {
// When the same set of animation frames has been cut, the cache is directly reused.
                return frames;
            }

            frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
// every frame in x direction movement xStep pixels, suitable for horizontal arrangement spritesheet。
                frames[i] = FromTopLeft(texturePath, key + "-" + i, x + (i * xStep), y, width, height);
            }

            FrameCache[cacheKey] = frames;
            return frames;
        }
        /// <summary>
/// Cut one from the coordinates of the upper left corner of the texture Sprite。Unity of Rect The coordinates are calculated from the lower left corner, so there will be a conversion here y coordinate.
        /// </summary>
/// <param name="texturePath">Resources The path of the medium texture image. </param>
/// <param name="key">cache Sprite the only one used when key, to avoid repeated cutting. </param>
/// <param name="x">Horizontal pixel coordinates on the texture map. </param>
/// <param name="y">Vertical pixel coordinates on the texture map. </param>
/// <param name="width">cut picture or UI The width of the element. </param>
/// <param name="height">cut picture or UI The height of the element. </param>
/// <returns>Returns the loaded or generated Sprite; May be returned when the resource does not exist null。</returns>
        private static Sprite FromTopLeft(string texturePath, string key, int x, int y, int width, int height)
        {
            string cacheKey = texturePath + ":" + key;
            if (SpriteCache.TryGetValue(cacheKey, out Sprite sprite))
            {
// leaflet Sprite Directly reuse the created files to reduce runtime memory and repeated drawing cuts.
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

// incoming y It is the coordinate of the upper left corner commonly used in art drawings. Sprite. Create The coordinates of the lower left corner are required.
            Rect rect = new Rect(x, texture.height - y - height, width, height);
            sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = key;
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }
    }
}
