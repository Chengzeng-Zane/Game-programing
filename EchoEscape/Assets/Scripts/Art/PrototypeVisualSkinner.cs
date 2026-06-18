using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Prototype Level Visual Replacer. Early level objects are usually simple color blocks. This script will add pixel style to the ground, platform, players and other objects. SpriteRenderer, making gray box levels look like official levels.
/// Gameplay logic: It only replaces or hides the visual, not changes BoxCollider2D、Trigger and Rigidbody2D, so the gameplay such as platform standing, death zone, chest interaction, etc. will not change due to changing the picture.
/// Collaboration: by EchoEscapeGameManager Called at the start of the level; depends on PixelArtLibrary Load platform and character materials.
    /// </summary>
    public class PrototypeVisualSkinner : MonoBehaviour
    {
        private const string PixelVisualName = "Pixel Art Visual";
        /// <summary>
/// Unity Called before the first frame. Here the scene object is usually connected to start the initial UI, tutorial or level process.
        /// </summary>
        private void Start()
        {
            SkinAll();
        }
        /// <summary>
/// Uniformly apply pixel style visuals to the characters and level color blocks in the scene. It only changes the display, not the Collider。
        /// </summary>
        public void SkinAll()
        {
            SkinCharacters();
            SkinLevelBlocks();
        }
        /// <summary>
/// Adds alternate pixel vision to players; if there is already an official PlayerAnimationController, turn off backup vision.
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
// formal Ruby When the animation exists, it cannot be displayed again. fallback Pixel characters, otherwise double shadows will overlap.
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
// When there is no official animation in the old gray box scene, a backup pixel character is automatically added to ensure that the player is not a block.
                PixelCharacterVisual fallbackVisual = player.gameObject.AddComponent<PixelCharacterVisual>();
                fallbackVisual.SetStyle(false, Color.white);
            }
        }
        /// <summary>
/// Hide correspondence UI Or visual state, usually called when the prompt ends, the pop-up window is closed, or the process is cleaned up.
        /// </summary>
/// <param name="playerTransform">player root object Transform。</param>
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
/// Traverse the gray boxes in the scene MeshRenderer, replace it with the corresponding pixel according to the object name Sprite。
        /// </summary>
        private void SkinLevelBlocks()
        {
            MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                GameObject target = renderer.gameObject;
                if (target.GetComponent<BoxCollider2D>() == null)
                {
// Only process color blocks related to card collision to avoid accidentally changing pure decoration Mesh。
                    continue;
                }

                string lowerName = target.name.ToLowerInvariant();
// This relies on object naming: Ground/Platform/Door/Chest It will be mapped to different Sprite。
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
/// Hide original gray box MeshRenderer, and create or update sub-objects Pixel Art Visual to display pixels Sprite。
        /// </summary>
/// <param name="target">Target Transform or GameObject, the function reads its position, component, or state. </param>
/// <param name="sprite">to be displayed Sprite picture. </param>
/// <param name="tiled">tiled Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
        private void ReplaceWithSprite(GameObject target, Sprite sprite, bool tiled, Color color)
        {
            if (sprite == null)
            {
// When the material is missing, the original object is not changed to prevent the level from becoming invisible.
                return;
            }

            MeshRenderer meshRenderer = target.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
// Only hides the visual, does not delete the object, Collider and the script remains on the original object.
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
// The child object carries the pixel map, and the root object continues to retain collision and gameplay scripts.
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
// platform/The ground needs to be tiled and mapped, and the size is directly related to BoxCollider2D Alignment.
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
// Click on non-platform objects such as doors, treasure chests, and exits Collider Scale one size Sprite。
                FitSpriteToCollider(target, visualTransform, spriteRenderer);
            }
        }
        /// <summary>
/// Determined based on object name SpriteRenderer Sort the levels so that treasure chests, buttons, and doors appear in front of the platform.
        /// </summary>
/// <param name="objectName">to create or find GameObject name. </param>
/// <returns>Returns an integer result, usually representing the quantity, index, or quantity of this settlement. </returns>
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
/// will not tile Sprite zoom to BoxCollider2D The size aligns the visual and interactive scope.
        /// </summary>
/// <param name="target">Target Transform or GameObject, the function reads its position, component, or state. </param>
/// <param name="visualTransform">visualTransform Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="spriteRenderer">spriteRenderer Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
        private void FitSpriteToCollider(GameObject target, Transform visualTransform, SpriteRenderer spriteRenderer)
        {
            BoxCollider2D box = target.GetComponent<BoxCollider2D>();
            if (box == null || spriteRenderer.sprite == null)
            {
// No Collider or Sprite The scaling factor cannot be calculated.
                return;
            }

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
// Prevent division by 0。
                return;
            }

            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            visualTransform.localPosition = new Vector3(box.offset.x, box.offset.y, 0f);
            visualTransform.localScale = new Vector3(box.size.x / spriteSize.x, box.size.y / spriteSize.y, 1f);
        }
    }
}
