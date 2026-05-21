using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Provides helper functions for creating simple prototype objects and visuals.
    /// </summary>
    /// <remarks>
    /// Editor builders and runtime systems use this static class to create blocks,
    /// simple materials, and shared tint effects without duplicating setup code.
    /// </remarks>
    public static class PrototypeFactory
    {
        /// <summary>
        /// Creates a simple rectangular 2D gameplay block from a Unity cube primitive.
        /// </summary>
        /// <param name="name">Name assigned to the created GameObject.</param>
        /// <param name="position">World position of the block.</param>
        /// <param name="size">Width and height of the block.</param>
        /// <param name="color">Material color applied to the block.</param>
        /// <param name="solid">True for a blocking collider; false for a trigger collider.</param>
        /// <param name="parent">Optional transform parent for hierarchy organization.</param>
        /// <returns>The created GameObject with BoxCollider2D and MeshRenderer.</returns>
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
                // Unity cubes are born with a 3D collider. Remove it immediately
                // before adding the 2D collider used by the prototype gameplay.
                Object.DestroyImmediate(collider3D);
            }

            BoxCollider2D collider2D = block.AddComponent<BoxCollider2D>();
            collider2D.isTrigger = !solid;

            MeshRenderer renderer = block.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(color);

            return block;
        }

        /// <summary>
        /// Creates a material using the best available simple shader for the project.
        /// </summary>
        /// <param name="color">Color assigned to the material.</param>
        /// <returns>A new Material tinted with the requested color.</returns>
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
        /// Applies a color tint to mesh and sprite renderers on a target object.
        /// </summary>
        /// <param name="target">GameObject whose visible renderers should be tinted.</param>
        /// <param name="color">Color to apply to the target visuals.</param>
        public static void Tint(GameObject target, Color color)
        {
            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(color);
            }

            SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                spriteRenderer.color = color;
            }
        }

    }
}
