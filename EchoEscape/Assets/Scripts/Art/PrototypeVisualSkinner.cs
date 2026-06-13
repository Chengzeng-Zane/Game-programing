using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// 脚本总览：原型关卡视觉替换器。早期关卡对象通常是简单色块，这个脚本会给地面、平台、玩家等对象添加像素风 SpriteRenderer，让灰盒关卡看起来像正式关卡。
    /// 玩法逻辑：它只替换或隐藏视觉，不改 BoxCollider2D、Trigger 和 Rigidbody2D，所以平台站立、死亡区、宝箱交互等玩法不会因为换图而改变。
    /// 协作关系：由 EchoEscapeGameManager 在关卡开始时调用；依赖 PixelArtLibrary 读取平台和角色素材。
    /// </summary>
    public class PrototypeVisualSkinner : MonoBehaviour
    {
        private const string PixelVisualName = "Pixel Art Visual";
        /// <summary>
        /// Unity 在第一帧前调用。这里通常连接场景对象，启动初始 UI、教程或关卡流程。
        /// </summary>
        private void Start()
        {
            SkinAll();
        }
        /// <summary>
        /// 对场景里的角色和关卡色块统一应用像素风视觉。它只改显示，不改 Collider。
        /// </summary>
        public void SkinAll()
        {
            SkinCharacters();
            SkinLevelBlocks();
        }
        /// <summary>
        /// 给玩家补充备用像素视觉；如果已经有正式 PlayerAnimationController，就关闭备用视觉。
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
                // 正式 Ruby 动画存在时，不能再显示 fallback 像素角色，否则会双影重叠。
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
                // 旧灰盒场景没有正式动画时，自动加一个备用像素角色，保证玩家不是方块。
                PixelCharacterVisual fallbackVisual = player.gameObject.AddComponent<PixelCharacterVisual>();
                fallbackVisual.SetStyle(false, Color.white);
            }
        }
        /// <summary>
        /// 隐藏对应 UI 或视觉状态，通常在提示结束、关闭弹窗或清理流程时调用。
        /// </summary>
        /// <param name="playerTransform">玩家根对象 Transform。</param>
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
        /// 遍历场景里的灰盒 MeshRenderer，根据对象名字替换成对应像素 Sprite。
        /// </summary>
        private void SkinLevelBlocks()
        {
            MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                GameObject target = renderer.gameObject;
                if (target.GetComponent<BoxCollider2D>() == null)
                {
                    // 只处理有关卡碰撞的色块，避免误改纯装饰 Mesh。
                    continue;
                }

                string lowerName = target.name.ToLowerInvariant();
                // 这里依赖对象命名：Ground/Platform/Door/Chest 等会映射到不同 Sprite。
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
        /// 隐藏原本的灰盒 MeshRenderer，并创建或更新子物体 Pixel Art Visual 来显示像素 Sprite。
        /// </summary>
        /// <param name="target">目标 Transform 或 GameObject，函数会读取它的位置、组件或状态。</param>
        /// <param name="sprite">要显示的 Sprite 图片。</param>
        /// <param name="tiled">tiled 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="color">颜色值，用于材质、文字、图片或 SpriteRenderer。</param>
        private void ReplaceWithSprite(GameObject target, Sprite sprite, bool tiled, Color color)
        {
            if (sprite == null)
            {
                // 素材缺失时不改原对象，避免关卡变成不可见。
                return;
            }

            MeshRenderer meshRenderer = target.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                // 只隐藏视觉，不删除对象，Collider 和脚本仍留在原物体上。
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
                // 子物体承载像素图，根物体继续保留碰撞和玩法脚本。
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
                // 平台/地面需要平铺贴图，尺寸直接跟 BoxCollider2D 对齐。
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
                // 门、宝箱、出口等非平台对象按 Collider 尺寸缩放一张 Sprite。
                FitSpriteToCollider(target, visualTransform, spriteRenderer);
            }
        }
        /// <summary>
        /// 根据对象名字决定 SpriteRenderer 排序层级，让宝箱、按钮、门显示在平台前面。
        /// </summary>
        /// <param name="objectName">要创建或查找的 GameObject 名称。</param>
        /// <returns>返回整数结果，通常表示数量、索引或本次结算数量。</returns>
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
        /// 将非平铺 Sprite 缩放到 BoxCollider2D 的大小，让视觉和交互范围对齐。
        /// </summary>
        /// <param name="target">目标 Transform 或 GameObject，函数会读取它的位置、组件或状态。</param>
        /// <param name="visualTransform">visualTransform 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        /// <param name="spriteRenderer">spriteRenderer 参数由调用方传入，用来参与本函数的判断、计算或设置。</param>
        private void FitSpriteToCollider(GameObject target, Transform visualTransform, SpriteRenderer spriteRenderer)
        {
            BoxCollider2D box = target.GetComponent<BoxCollider2D>();
            if (box == null || spriteRenderer.sprite == null)
            {
                // 没有 Collider 或 Sprite 时无法计算缩放比例。
                return;
            }

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                // 防止除以 0。
                return;
            }

            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            visualTransform.localPosition = new Vector3(box.offset.x, box.offset.y, 0f);
            visualTransform.localScale = new Vector3(box.size.x / spriteSize.x, box.size.y / spriteSize.y, 1f);
        }
    }
}
