using System.Collections;
using UnityEngine;
using TMPro;

public class DiveCameraController : MonoBehaviour
{
    public static DiveCameraController Instance { get; private set; }

    [Header("Kecepatan & Kedalaman")]
    [Tooltip("Kecepatan normal turun per detik (dalam unit Unity)")]
    public float normalSpeed = 6f; // ~60m/detik, tembus 11.000m dalam ~3 menit
    public float currentSpeed;

    [Tooltip("Target kedalaman terbawah (11.000m = -1100 unit Y)")]
    public float maxDepthUnits = -1100f;

    [Header("UI Kedalaman & Meter")]
    public TextMeshProUGUI depthMeterText; // Teks penunjuk meter (misal: "850 m")
    public TextMeshProUGUI zoneNameText;   // Teks penunjuk nama zona

    [Header("Senter Tangan (Spot Light)")]
    public Light handFlashlight;           // Senter yang mengikuti kursor
    public float flashlightActiveDepth = -100f; // Aktif di >= 1.000m (-100 unit)

    [Header("Pengaturan Fog / Kegelapan")]
    public Gradient waterDepthGradient;    // Biru cerah -> Biru redup -> Hitam pekat

    private bool isPaused = false;
    private bool isSlowedDown = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        currentSpeed = normalSpeed;
    }

    private void Start()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.03f;

        if (handFlashlight != null)
        {
            handFlashlight.enabled = false;
        }
    }

    private void Update()
    {
        // 1. Gerakkan kamera turun jika tidak sedang pause membaca info
        if (!isPaused && transform.position.y > maxDepthUnits)
        {
            float targetSpeed = isSlowedDown ? (normalSpeed * 0.2f) : normalSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

            transform.position += Vector3.down * currentSpeed * Time.deltaTime;
        }

        // 2. Perbarui tampilan meteran kedalaman
        float currentDepthMeters = Mathf.Abs(transform.position.y) * 10f;
        if (depthMeterText != null)
        {
            depthMeterText.text = $"{Mathf.FloorToInt(currentDepthMeters):N0} m";
        }

        // 3. Perbarui nama zona & warna fog lingkungan
        UpdateZoneAndVisuals(transform.position.y, currentDepthMeters);

        // 4. Update posisi senter tangan (mengikuti kursor mouse di kedalaman >= 1.000m)
        UpdateHandFlashlight();
    }

    private void UpdateZoneAndVisuals(float currentY, float depthMeters)
    {
        string currentZone = "Zona Sunlight";
        if (depthMeters >= 6000f) currentZone = "Zona Hadal (Palung)";
        else if (depthMeters >= 4000f) currentZone = "Zona Abyssal";
        else if (depthMeters >= 1000f) currentZone = "Zona Midnight";
        else if (depthMeters >= 200f) currentZone = "Zona Twilight";

        if (zoneNameText != null) zoneNameText.text = currentZone;

        // Hitung warna fog berdasarkan kedalaman (0 sampai -1100)
        float depthPercent = Mathf.Clamp01(Mathf.Abs(currentY) / Mathf.Abs(maxDepthUnits));
        Color targetColor = waterDepthGradient.Evaluate(depthPercent);

        RenderSettings.fogColor = targetColor;
        if (Camera.main != null) Camera.main.backgroundColor = targetColor;
    }

    private void UpdateHandFlashlight()
    {
        if (handFlashlight == null) return;

        // Senter hanya menyala jika kedalaman >= 1.000m (-100 unit)
        bool shouldLightBeOn = transform.position.y <= flashlightActiveDepth;
        handFlashlight.enabled = shouldLightBeOn;

        if (shouldLightBeOn && KinectInputManager.Instance != null)
        {
            // Posisikan senter searah kursor di depan kamera
            Ray ray = Camera.main.ScreenPointToRay(KinectInputManager.Instance.HandScreenPosition);
            handFlashlight.transform.position = ray.origin + ray.direction * 2f;
            handFlashlight.transform.rotation = Quaternion.LookRotation(ray.direction);
        }
    }

    // Fungsi yang dipanggil saat kursor sedang menyorot objek (melambat 20%)
    public void SetHoverSlowdown(bool slow)
    {
        isSlowedDown = slow;
    }

    // Fungsi pause saat user berhasil dwell dan kartu info terbuka
    public void PauseForReading(float duration = 3.5f)
    {
        StartCoroutine(PauseRoutine(duration));
    }

    private IEnumerator PauseRoutine(float duration)
    {
        isPaused = true;
        yield return new WaitForSeconds(duration);
        isPaused = false;
    }
}