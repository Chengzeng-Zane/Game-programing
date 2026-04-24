using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SolarSystemManager : MonoBehaviour
{
    public static SolarSystemManager Instance { get; private set; }

    public Camera mainCamera;
    public Transform mainViewSpot;

    public TMP_Text titleText;
    public TMP_Text factText;
    public Button returnButton;

    public AudioSource audioSource;
    public float cameraMoveSeconds = 1.2f;

    private Transform currentTarget;
    private Coroutine cameraRoutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        HideUI();

        if (returnButton != null)
        {
            returnButton.onClick.AddListener(ReturnToMainView);
        }
    }

    void LateUpdate()
    {
        if (currentTarget != null && mainCamera != null)
        {
            mainCamera.transform.LookAt(currentTarget.position);
        }
    }

    public void SelectObject(ClickableSpaceObject selectedObject)
    {
        currentTarget = selectedObject.transform;

        ShowUI();

        if (titleText != null)
        {
            titleText.text = selectedObject.displayName;
        }

        if (factText != null)
        {
            factText.text = selectedObject.kidFact;
        }

        if (audioSource != null && selectedObject.clickSound != null)
        {
            audioSource.PlayOneShot(selectedObject.clickSound);
        }

        selectedObject.PlayVisualResponse();

        if (selectedObject.cameraSpot != null)
        {
            MoveCameraTo(selectedObject.cameraSpot.position, selectedObject.transform.position);
        }
    }

    public void ReturnToMainView()
    {
        currentTarget = null;

        HideUI();

        if (mainViewSpot != null)
        {
            MoveCameraTo(mainViewSpot.position, Vector3.zero);
        }
    }

    void ShowUI()
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
        }

        if (factText != null)
        {
            factText.gameObject.SetActive(true);
        }

        if (returnButton != null)
        {
            returnButton.gameObject.SetActive(true);
        }
    }

    void HideUI()
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
        }

        if (factText != null)
        {
            factText.gameObject.SetActive(false);
        }

        if (returnButton != null)
        {
            returnButton.gameObject.SetActive(false);
        }
    }

    void MoveCameraTo(Vector3 targetPosition, Vector3 lookAtPoint)
    {
        if (cameraRoutine != null)
        {
            StopCoroutine(cameraRoutine);
        }

        cameraRoutine = StartCoroutine(MoveCamera(targetPosition, lookAtPoint));
    }

    IEnumerator MoveCamera(Vector3 targetPosition, Vector3 lookAtPoint)
    {
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - targetPosition);

        float timer = 0f;

        while (timer < cameraMoveSeconds)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / cameraMoveSeconds);

            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;
    }
}