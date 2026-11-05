using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DynamicVignette : MonoBehaviour
{
    public Camera mainCamera;
    public Volume globalVolume;

    [Header("Camera Y (Kedalaman 0 sampai -100 unit = 1000m)")]
    public float surfaceCameraY = 0f;      // Y di permukaan (0m)
    public float deepCameraY = -100f;      // Y di dasar target (1000m)

    [Header("Vignette Intensity")]
    public float minIntensity = 0.15f;     // Intensitas di permukaan (terang)
    public float maxIntensity = 0.85f;     // Intensitas di kedalaman (gelap menyempit)

    [Header("Vignette Smoothness (Makin dalam makin sempit)")]
    public float surfaceSmoothness = 0.2f;
    public float deepSmoothness = 0.7f;

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

        if (KinectInputManager.Instance == null)
            return;

        // =========================
        // CAMERA Y → INTENSITY & SMOOTHNESS
        // =========================

        float cameraY = mainCamera.transform.position.y;

        // Progress kedalaman dari 0f (permukaan) ke 1f (1000m / deepCameraY)
        float depthProgress = Mathf.InverseLerp(surfaceCameraY, deepCameraY, cameraY);

        float intensity = Mathf.Lerp(minIntensity, maxIntensity, depthProgress);
        vignette.intensity.value = intensity;

        // Vignette makin menyempit/tajam seiring makin dalam
        float smoothness = Mathf.Lerp(surfaceSmoothness, deepSmoothness, depthProgress);
        vignette.smoothness.value = smoothness;


        // =========================
        // MOUSE / KINECT → CENTER
        // =========================

        Vector2 handPos = KinectInputManager.Instance.HandScreenPosition;
        Vector2 normalizedPos = new Vector2(
            handPos.x / Screen.width,
            handPos.y / Screen.height
        );

        currentCenter = Vector2.Lerp(
            currentCenter,
            normalizedPos,
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