using UnityEngine;

namespace EchoVault
{
    public class PrototypeVisualSkinner : MonoBehaviour
    {
        private const string PixelVisualName = "Pixel Art Visual";

        private void Start()
        {
            SkinAll();
        }

        public void SkinAll()
        {
            SkinCharacters();
            SkinLevelBlocks();
        }

        private void SkinCharacters()
        {
            PlayerController2D player = FindObjectOfType<PlayerController2D>();
            if (player != null && player.GetComponent<PixelCharacterVisual>() == null)
            {
                PixelCharacterVisual visual = player.gameObject.AddComponent<PixelCharacterVisual>();
                visual.SetStyle(false, Color.white);
            }
        }

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
