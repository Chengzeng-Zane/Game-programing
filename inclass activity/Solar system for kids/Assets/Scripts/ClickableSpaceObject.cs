using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClickableSpaceObject : MonoBehaviour
{
    public string displayName = "Earth";

    [TextArea(2, 4)]
    public string kidFact = "This is a space object!";

    public Transform cameraSpot;
    public AudioClip clickSound;

    public float growSize = 1.25f;
    public Color flashColor = Color.yellow;

    private Vector3 originalScale;
    private Renderer objectRenderer;
    private Material objectMaterial;
    private Color originalColor;
    private string colorProperty = "";
    private Coroutine visualRoutine;

    void Awake()
    {
        originalScale = transform.localScale;
        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
        {
            objectMaterial = objectRenderer.material;

            if (objectMaterial.HasProperty("_BaseColor"))
            {
                colorProperty = "_BaseColor";
                originalColor = objectMaterial.GetColor(colorProperty);
            }
            else if (objectMaterial.HasProperty("_Color"))
            {
                colorProperty = "_Color";
                originalColor = objectMaterial.GetColor(colorProperty);
            }
        }
    }

    void OnMouseDown()
    {
        if (SolarSystemManager.Instance != null)
        {
            SolarSystemManager.Instance.SelectObject(this);
        }
    }

    public void PlayVisualResponse()
    {
        if (visualRoutine != null)
        {
            StopCoroutine(visualRoutine);
        }

        visualRoutine = StartCoroutine(FlashAndGrow());
    }

    IEnumerator FlashAndGrow()
    {
        transform.localScale = originalScale * growSize;
        SetObjectColor(flashColor);

        yield return new WaitForSeconds(0.4f);

        transform.localScale = originalScale;
        SetObjectColor(originalColor);
    }

    void SetObjectColor(Color newColor)
    {
        if (objectMaterial != null && colorProperty != "")
        {
            objectMaterial.SetColor(colorProperty, newColor);
        }
    }
}