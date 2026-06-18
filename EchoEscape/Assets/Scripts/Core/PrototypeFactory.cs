using UnityEngine;

namespace EchoEscape
{
    /// <summary>
/// Script overview: Runtime prototype object factory, quickly create platforms, walls, trigger vision and other basics using code GameObject。
/// Gameplay logic: Early levels or automatic construction processes need to quickly generate block objects; this tool will create Sprite/Mesh Appearance, material and 2D Collider and handle Unity default 3D Collider of cleaning.
/// Collaborates with: Mainly serving gray box level construction and prototype vision, and does not participate in player movement, loot or death determination rules.
    /// </summary>
    public static class PrototypeFactory
    {
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="name">Object name or resource name. </param>
/// <param name="position">Target world coordinates, often used for respawn, spawning objects or recording Echo frame. </param>
/// <param name="size">size Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <param name="solid">solid Parameters are passed in by the caller and used to participate in the judgment, calculation or setting of this function. </param>
/// <param name="parent">The parent node to which the newly created object will be hung. </param>
/// <returns>Returns a created or found GameObjectto facilitate the caller to continue adding components or setting locations. </returns>
        public static GameObject CreateBlock(string name, Vector2 position, Vector2 size, Color color, bool solid, Transform parent = null)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = new Vector3(position.x, position.y, 0f);
            block.transform.localScale = new Vector3(size.x, size.y, 0.25f);

            Collider collider3D = block.GetComponent<Collider>();
            if (collider3D != null)
            {
// Unity The default block will come with 3D The collision body is deleted here first.
// This project is 2D Platform game, will be added later 2D Collider.
                Object.DestroyImmediate(collider3D);
            }

            BoxCollider2D collider2D = block.AddComponent<BoxCollider2D>();
            collider2D.isTrigger = !solid;

            MeshRenderer renderer = block.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(color);

            return block;
        }
        /// <summary>
/// Create objects at runtime, UI Element or visual component and set its basic properties in the current game interface or scene.
        /// </summary>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
/// <returns>Returns the created material, which can be used for Sprite or Mesh show. </returns>
        public static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.color = color;
            return material;
        }
        /// <summary>
/// Give the target object and its Sprite Sub-objects are uniformly colored. door, treasure chest fallback and prototype color block feedback will be used.
        /// </summary>
/// <param name="target">Target Transform or GameObject, the function reads its position, component, or state. </param>
/// <param name="color">Color value, used for materials, text, images, or SpriteRenderer。</param>
        public static void Tint(GameObject target, Color color)
        {
            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
// gray box Mesh use Material color.
                renderer.sharedMaterial = CreateMaterial(color);
            }

            SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
// Pixel vision sub-object usage SpriteRenderer. color。
                spriteRenderer.color = color;
            }
        }

    }
}
