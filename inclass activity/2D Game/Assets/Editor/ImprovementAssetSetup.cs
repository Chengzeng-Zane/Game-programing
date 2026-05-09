using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the assignment improvement visible in Unity prefabs, not only in runtime code.
/// This runs in the editor after scripts compile and creates/updates the HUD text and pickup prefab.
/// </summary>
[InitializeOnLoad]
public static class ImprovementAssetSetup
{
    private const string MainHudPrefabPath = "Assets/Prefabs/UI/InGameUI.prefab";
    private const string PageHudPrefabPath = "Assets/Prefabs/UI/UIPages/InGameUI.prefab";
    private const string MainMenuPrefabPath = "Assets/Prefabs/UI/MainMenu.prefab";
    private const string PowerUpPrefabPath = "Assets/Resources/PowerUps/RapidFirePowerUp.prefab";
    private const string PowerUpFolder = "Assets/Resources/PowerUps";
    private const string GoldReticlePath = "Assets/Art/Reticles/Reticle_Gold.png";
    private const string InstructionsText = "INSTRUCTIONS\n\nOBJECTIVE\nDefeat 3 enemies to clear the level.\n\nCONTROLS\nWASD: move\nMouse: aim\nLeft Click: shoot\nEsc: pause\n\nHUD\nWatch Lives, Goal, and Power-Up in the top-left.\n\nPOWER-UP\nCollect the gold Rapid Fire pickup to shoot faster for a few seconds.";

    static ImprovementAssetSetup()
    {
        EditorApplication.delayCall += EnsureImprovementAssets;
    }

    private static void EnsureImprovementAssets()
    {
        EditorApplication.delayCall -= EnsureImprovementAssets;

        EnsureHudPrefab(MainHudPrefabPath);
        EnsureHudPrefab(PageHudPrefabPath);
        EnsureInstructionsPage();
        EnsurePowerUpPrefab();
        AssetDatabase.SaveAssets();
    }

    private static void EnsureHudPrefab(string prefabPath)
    {
        if (!File.Exists(prefabPath))
        {
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        bool changed = false;

        try
        {
            TextMeshProUGUI[] textElements = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI highScoreText = textElements.FirstOrDefault(text => text.name == "High Score");
            TextMeshProUGUI scoreText = textElements.FirstOrDefault(text => text.name == "Score text");
            TextMeshProUGUI templateText = highScoreText != null ? highScoreText : scoreText;

            if (templateText == null)
            {
                return;
            }

            RectTransform baseTransform = templateText.GetComponent<RectTransform>();
            Transform parent = templateText.transform.parent;
            Vector2 basePosition = baseTransform.anchoredPosition;
            const float lineHeight = 45f;

            changed |= CreateOrUpdateHudText(root, parent, templateText, baseTransform, "Lives Text", "Lives: 3", basePosition + new Vector2(0f, -lineHeight));
            changed |= CreateOrUpdateHudText(root, parent, templateText, baseTransform, "Objective Text", "Goal: 0/3 kills", basePosition + new Vector2(0f, -lineHeight * 2f));
            changed |= CreateOrUpdateHudText(root, parent, templateText, baseTransform, "Power-Up Text", "Power-Up: Waiting", basePosition + new Vector2(0f, -lineHeight * 3f), "Bonus Text");

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool CreateOrUpdateHudText(GameObject root, Transform parent, TextMeshProUGUI templateText, RectTransform templateTransform, string objectName, string defaultText, Vector2 anchoredPosition, string legacyObjectName = null)
    {
        Transform existingTransform = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(child => child.name == objectName || child.name == legacyObjectName);
        GameObject textObject = existingTransform != null ? existingTransform.gameObject : new GameObject(objectName, typeof(RectTransform));

        if (existingTransform == null)
        {
            textObject.layer = templateText.gameObject.layer;
            textObject.transform.SetParent(parent, false);
        }
        else
        {
            textObject.name = objectName;
        }

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = templateTransform.anchorMin;
        rectTransform.anchorMax = templateTransform.anchorMax;
        rectTransform.pivot = templateTransform.pivot;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(280f, templateTransform.sizeDelta.y);
        rectTransform.localScale = templateTransform.localScale;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

        text.font = templateText.font;
        text.fontSharedMaterial = templateText.fontSharedMaterial;
        text.fontSize = templateText.fontSize;
        text.fontStyle = templateText.fontStyle;
        text.color = templateText.color;
        text.alignment = templateText.alignment;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.text = defaultText;

        EditorUtility.SetDirty(textObject);
        return true;
    }

    private static void EnsureInstructionsPage()
    {
        if (!File.Exists(MainMenuPrefabPath))
        {
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(MainMenuPrefabPath);

        try
        {
            UIPage instructionsPage = root.GetComponentsInChildren<UIPage>(true)
                .FirstOrDefault(page => page.name == "Instructions");

            if (instructionsPage == null)
            {
                return;
            }

            Transform backdrop = instructionsPage.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "UIBackdrop");

            if (backdrop == null)
            {
                backdrop = instructionsPage.transform;
            }

            Text bodyText = backdrop.GetComponentsInChildren<Text>(true)
                .FirstOrDefault(text => text.name == "Instructions Body Text");

            if (bodyText == null)
            {
                GameObject textObject = new GameObject("Instructions Body Text", typeof(RectTransform));
                textObject.layer = backdrop.gameObject.layer;
                textObject.transform.SetParent(backdrop, false);
                bodyText = textObject.AddComponent<Text>();
            }

            RectTransform rectTransform = bodyText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 35f);
            rectTransform.sizeDelta = new Vector2(365f, 250f);
            rectTransform.localScale = Vector3.one;

            Text templateText = root.GetComponentsInChildren<Text>(true)
                .FirstOrDefault(text => text.text == "Instructions")
                ?? root.GetComponentsInChildren<Text>(true).FirstOrDefault();

            if (templateText != null)
            {
                bodyText.font = templateText.font;
                bodyText.color = templateText.color;
            }

            bodyText.fontSize = 14;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
            bodyText.supportRichText = true;
            bodyText.raycastTarget = false;
            bodyText.lineSpacing = 1f;
            bodyText.text = InstructionsText;

            EditorUtility.SetDirty(bodyText.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, MainMenuPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsurePowerUpPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder(PowerUpFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "PowerUps");
        }

        bool loadedExistingPrefab = File.Exists(PowerUpPrefabPath);
        GameObject root = loadedExistingPrefab
            ? PrefabUtility.LoadPrefabContents(PowerUpPrefabPath)
            : new GameObject("RapidFirePowerUp");

        try
        {
            root.name = "RapidFirePowerUp";
            root.transform.localScale = new Vector3(2.5f, 2.5f, 1f);

            SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = root.AddComponent<SpriteRenderer>();
            }
            spriteRenderer.sprite = LoadGoldReticleSprite();
            spriteRenderer.sortingOrder = 50;

            CircleCollider2D collider = root.GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = root.AddComponent<CircleCollider2D>();
            }
            collider.isTrigger = true;
            collider.radius = 0.55f;

            if (root.GetComponent<RapidFirePowerUpPickup>() == null)
            {
                root.AddComponent<RapidFirePowerUpPickup>();
            }

            PrefabUtility.SaveAsPrefabAsset(root, PowerUpPrefabPath);
        }
        finally
        {
            if (loadedExistingPrefab)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    private static Sprite LoadGoldReticleSprite()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(GoldReticlePath);
        return assets.OfType<Sprite>().FirstOrDefault(sprite => sprite.name == "Reticle_Gold_0")
            ?? assets.OfType<Sprite>().FirstOrDefault();
    }
}
