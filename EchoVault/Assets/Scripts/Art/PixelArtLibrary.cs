using System.Collections.Generic;
using UnityEngine;

namespace EchoVault
{
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

        public static AudioClip LoadSound(string fileName)
        {
            return Resources.Load<AudioClip>($"BrackeysPlatformer/Audio/Sounds/{fileName}");
        }

        public static AudioClip LoadMusic(string fileName)
        {
            return Resources.Load<AudioClip>($"BrackeysPlatformer/Audio/Music/{fileName}");
        }

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
