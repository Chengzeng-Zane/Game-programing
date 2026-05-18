using UnityEngine;

namespace EchoEscape
{
    public static class PrototypeFactory
    {
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
