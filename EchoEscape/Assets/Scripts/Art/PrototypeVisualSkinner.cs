using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Applies pixel-art visuals to prototype objects that were built from simple blocks.
    /// </summary>
    /// <remarks>
    /// Attach this script to the Game Manager or a scene service object.
    /// It finds player and level objects, then adds SpriteRenderer visuals from PixelArtLibrary
    /// without changing the underlying gameplay colliders.
    /// </remarks>
    public class PrototypeVisualSkinner : MonoBehaviour
    {
        private const string PixelVisualName = "Pixel Art Visual";

        /// <summary>
        /// Unity event method called before the first frame update.
        /// </summary>
        /// <remarks>
        /// Applies pixel-art skinning once the scene has created its prototype objects.
        /// </remarks>
        private void Start()
        {
            SkinAll();
        }

        /// <summary>
        /// Applies character and level-block visual skinning.
        /// </summary>
        /// <remarks>
        /// Can be called by EchoEscapeGameManager or editor-built scenes after objects are created.
        /// </remarks>
        public void SkinAll()
        {
            SkinCharacters();
            SkinLevelBlocks();
        }

        /// <summary>
        /// Adds PixelCharacterVisual to the player when it is missing.
        /// </summary>
        private void SkinCharacters()
        {
            PlayerController2D player = FindObjectOfType<PlayerController2D>();
            if (player == null)
            {
                return;
            }

            if (player.GetComponentInChildren<PlayerAnimationController>(true) != null)
            {
                PixelCharacterVisual existingFallbackVisual = player.GetComponent<PixelCharacterVisual>();
                if (existingFallbackVisual != null)
                {
                    existingFallbackVisual.enabled = false;
                }

                HideFallbackCharacterSprite(player.transform);
                return;
            }

            if (player.GetComponent<PixelCharacterVisual>() == null)
            {
                PixelCharacterVisual fallbackVisual = player.gameObject.AddComponent<PixelCharacterVisual>();
                fallbackVisual.SetStyle(false, Color.white);
            }
        }

        /// <summary>
        /// Hides the older runtime fallback sprite when the Ruby animation controller is present.
        /// </summary>
        /// <param name="playerTransform">Root transform of the player object.</param>
        private void HideFallbackCharacterSprite(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return;
            }

            Transform fallbackSprite = playerTransform.Find("Player Pixel Sprite");
            if (fallbackSprite == null)
            {
                return;
            }

            SpriteRenderer fallbackRenderer = fallbackSprite.GetComponent<SpriteRenderer>();
            if (fallbackRenderer != null)
            {
                fallbackRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Finds simple block objects and replaces their visible mesh with matching sprites.
        /// </summary>
        private void SkinLevelBlocks()
        {
            MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                GameObject target = renderer.gameObject;
                if (target.GetComponent<BoxCollider2D>() == null)
                {
                    continue;
                }

                string lowerName = target.name.ToLowerInvariant();
                if (lowerName.Contains("ground") || lowerName.Contains("ledge") || lowerName.Contains("platform"))
                {
                    ReplaceWithSprite(target, PixelArtLibrary.GroundTile, true, new Color(1f, 1f, 1f));
                }
                else if (lowerName.Contains("door"))
                {
                    ReplaceWithSprite(target, PixelArtLibrary.DoorTile, false, new Color(1f, 0.65f, 0.65f));
                }
                else if (lowerName.Contains("pressure plate"))
                {
                    ReplaceWithSprite(target, PixelArtLibrary.PlateTile, false, new Color(1f, 0.9f, 0.25f));
                }
                else if (lowerName.Contains("hazard"))
                {
                    ReplaceWithSprite(target, PixelArtLibrary.HazardSlime, false, new Color(1f, 1f, 1f));
                }
                else if (lowerName.Contains("chest"))
                {
                    ReplaceWithSprite(target, PixelArtLibrary.ChestTile, false, new Color(1f, 1f, 1f));
                }
                else if (lowerName.Contains("exit"))
                {
                    ReplaceWithSprite(target, PixelArtLibrary.ExitGem, false, new Color(1f, 1f, 1f));
                }
            }
        }

        /// <summary>
        /// Adds or updates a child SpriteRenderer to visually replace a prototype block.
        /// </summary>
        /// <param name="target">Prototype object being skinned.</param>
        /// <param name="sprite">Sprite used for the new visual.</param>
        /// <param name="tiled">True when the sprite should tile to match the collider size.</param>
        /// <param name="color">Color tint applied to the sprite.</param>
        private void ReplaceWithSprite(GameObject target, Sprite sprite, bool tiled, Color color)
        {
            if (sprite == null)
            {
                return;
            }

            MeshRenderer meshRenderer = target.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }

            SpriteRenderer rootSprite = target.GetComponent<SpriteRenderer>();
            if (rootSprite != null)
            {
                rootSprite.enabled = false;
            }

            Transform visualTransform = target.transform.Find(PixelVisualName);
            if (visualTransform == null)
            {
                GameObject visualObject = new GameObject(PixelVisualName);
                visualTransform = visualObject.transform;
                visualTransform.SetParent(target.transform, false);
            }

            visualTransform.localPosition = Vector3.zero;
            visualTransform.localRotation = Quaternion.identity;

            SpriteRenderer spriteRenderer = visualTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = visualTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = SortingOrderFor(target.name);

            if (tiled)
            {
                spriteRenderer.drawMode = SpriteDrawMode.Tiled;
                spriteRenderer.tileMode = SpriteTileMode.Continuous;

                BoxCollider2D box = target.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    visualTransform.localPosition = new Vector3(box.offset.x, box.offset.y, 0f);
                    visualTransform.localScale = Vector3.one;
                    spriteRenderer.size = box.size;
                }
            }
            else
            {
                FitSpriteToCollider(target, visualTransform, spriteRenderer);
            }
        }

        /// <summary>
        /// Chooses sprite sorting order from the object's name.
        /// </summary>
        /// <param name="objectName">Name of the object being skinned.</param>
        /// <returns>Sorting order used by the replacement SpriteRenderer.</returns>
        private int SortingOrderFor(string objectName)
        {
            string lowerName = objectName.ToLowerInvariant();
            if (lowerName.Contains("chest") || lowerName.Contains("plate") || lowerName.Contains("hazard") || lowerName.Contains("exit"))
            {
                return 4;
            }

            if (lowerName.Contains("door"))
            {
                return 3;
            }

            return 0;
        }

        /// <summary>
        /// Scales a non-tiled sprite to match a BoxCollider2D.
        /// </summary>
        /// <param name="target">Object that owns the collider.</param>
        /// <param name="visualTransform">Child transform used for the sprite visual.</param>
        /// <param name="spriteRenderer">SpriteRenderer being fitted to the collider.</param>
        private void FitSpriteToCollider(GameObject target, Transform visualTransform, SpriteRenderer spriteRenderer)
        {
            BoxCollider2D box = target.GetComponent<BoxCollider2D>();
            if (box == null || spriteRenderer.sprite == null)
            {
                return;
            }

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                return;
            }

            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            visualTransform.localPosition = new Vector3(box.offset.x, box.offset.y, 0f);
            visualTransform.localScale = new Vector3(box.size.x / spriteSize.x, box.size.y / spriteSize.y, 1f);
        }
    }
}
