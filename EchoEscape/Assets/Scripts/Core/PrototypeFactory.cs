using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：运行时原型对象工厂，用代码快速创建平台、墙、触发器视觉等基础 GameObject。
    /// 玩法逻辑：早期关卡或自动搭建流程需要快速生成方块对象；这个工具会创建 Sprite/Mesh 外观、材质和 2D 碰撞体，并处理 Unity 默认 3D Collider 的清理。
    /// 协作关系：主要服务灰盒关卡搭建和原型视觉，不参与玩家移动、loot 或死亡判定规则。
    /// </summary>
    public static class PrototypeFactory
    {
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="name">对象名称或资源名称。</param>
        /// <param name="position">目标世界坐标，常用于重生、生成对象或记录 Echo 帧。</param>
        /// <param name="size">size 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <param name="solid">solid 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="parent">新创建对象要挂到的父节点。</param>
        /// <returns>返回创建或找到的 GameObject，方便调用方继续添加组件或设置位置。</returns>
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
                // Unity 默认方块会自带 3D 碰撞体，这里先删掉。
                // 本项目是 2D 平台游戏，后面会重新添加 2D 碰撞体。
                Object.DestroyImmediate(collider3D);
            }

            BoxCollider2D collider2D = block.AddComponent<BoxCollider2D>();
            collider2D.isTrigger = !solid;

            MeshRenderer renderer = block.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(color);

            return block;
        }
        /// <summary>
        /// 运行时创建对象、UI 元素或视觉组件，并设置它在当前游戏界面或场景中的基础属性。
        /// </summary>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        /// <returns>返回创建好的材质，可用于 Sprite 或 Mesh 显示。</returns>
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
        /// 给目标对象和它的 Sprite 子物体统一染色。门、宝箱 fallback 和原型色块反馈会用到。
        /// </summary>
        /// <param name="target">目标 Transform 或 GameObject，函数会读取它的位置、组件或状态。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        public static void Tint(GameObject target, Color color)
        {
            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                // 灰盒 Mesh 使用 Material 颜色。
                renderer.sharedMaterial = CreateMaterial(color);
            }

            SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                // 像素视觉子物体使用 SpriteRenderer.color。
                spriteRenderer.color = color;
            }
        }

    }
}
