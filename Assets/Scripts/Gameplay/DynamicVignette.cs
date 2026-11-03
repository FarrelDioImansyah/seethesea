using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DynamicVignette : MonoBehaviour
{
    public Camera mainCamera;
    public Volume globalVolume;

    [Header("Camera Y")]
    public float minCameraY = 0f;
    public float maxCameraY = 10f;

    [Header("Vignette")]
    public float minIntensity = 0.1f;
    public float maxIntensity = 0.8f;

    [Header("Mouse")]
    public float mouseSmoothSpeed = 5f;

    private Vignette vignette;
    private Vector2 currentCenter;

    void Start()
    {
        if (globalVolume == null)
        {
            Debug.LogError("Global Volume belum dimasukkan!");
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera belum dimasukkan!");
            return;
        }

        if (globalVolume.profile.TryGet(out vignette))
        {
            Debug.Log("Vignette berhasil ditemukan!");
            currentCenter = vignette.center.value;
        }
        else
        {
            Debug.LogError("Vignette tidak ditemukan di Volume Profile!");
        }
    }

    void Update()
    {
        if (vignette == null)
            return;

        // =========================
        // CAMERA Y → INTENSITY
        // =========================

        float cameraY = mainCamera.transform.position.y;

        float normalizedY = Mathf.InverseLerp(
            minCameraY,
            maxCameraY,
            cameraY
        );

        float intensity = Mathf.Lerp(
            minIntensity,
            maxIntensity,
            normalizedY
        );

        vignette.intensity.value = intensity;


        // =========================
        // MOUSE → CENTER
        // =========================

        Vector2 mousePosition = new Vector2(
            Input.mousePosition.x / Screen.width,
            Input.mousePosition.y / Screen.height
        );

        currentCenter = Vector2.Lerp(
            currentCenter,
            mousePosition,
            Time.deltaTime * mouseSmoothSpeed
        );

        vignette.center.value = currentCenter;


        // DEBUG
        Debug.Log(
            "Camera Y: " + cameraY +
            " | Intensity: " + intensity
        );
    }
}