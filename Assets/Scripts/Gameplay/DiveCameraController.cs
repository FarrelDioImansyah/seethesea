using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DiveCameraController : MonoBehaviour
{
    public static DiveCameraController Instance { get; private set; }

    [Header("Kecepatan & Kedalaman")]
    [Tooltip("Kecepatan normal turun per detik (dalam unit Unity)")]
    public float normalSpeed = 2f; // ~20m/detik di dunia nyata (skala 1 unit = 10m)
    public float currentSpeed;

    [Tooltip("Target kedalaman terbawah (1.000m = -100 unit Y)")]
    public float maxDepthUnits = -100f;

    [Header("UI Kedalaman & Meter")]
    public TextMeshProUGUI depthMeterText; // Teks penunjuk meter (misal: "850 m")
    public TextMeshProUGUI zoneNameText;   // Teks penunjuk nama zona

    [Header("UI Ending")]
    public GameObject endingPanel;         // Panel ringkasan / penutup di 1.000m
    public float returnToMenuDelay = 6f;   // Waktu tunggu sebelum load Scene00

    [Header("Pengaturan Fog / Kegelapan")]
    public Gradient waterDepthGradient;    // Biru cerah -> Biru gelap pekat

    private bool isPaused = false;
    private bool isSlowedDown = false;
    private bool isEndingTriggered = false;

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

        if (endingPanel != null)
        {
            endingPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // 1. Gerakkan kamera turun jika belum sampai kedalaman maksimal
        if (!isPaused && transform.position.y > maxDepthUnits)
        {
            float targetSpeed = isSlowedDown ? (normalSpeed * 0.2f) : normalSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

            transform.position += Vector3.down * currentSpeed * Time.deltaTime;
        }
        else if (transform.position.y <= maxDepthUnits && !isEndingTriggered)
        {
            TriggerEndingSequence();
        }

        // 2. Perbarui tampilan meteran kedalaman
        float currentDepthMeters = Mathf.Abs(transform.position.y) * 10f;
        if (depthMeterText != null)
        {
            depthMeterText.text = $"{Mathf.FloorToInt(currentDepthMeters):N0} m";
        }

        // 3. Perbarui nama zona & warna fog lingkungan
        UpdateZoneAndVisuals(transform.position.y, currentDepthMeters);
    }

    private void TriggerEndingSequence()
    {
        isEndingTriggered = true;
        isPaused = true;
        Debug.Log("[DiveCamera] Mencapai kedalaman 1.000m! Menampilkan ending...");

        if (endingPanel != null)
        {
            endingPanel.SetActive(true);
        }

        StartCoroutine(ReturnToMenuRoutine());
    }

    private IEnumerator ReturnToMenuRoutine()
    {
        yield return new WaitForSeconds(returnToMenuDelay);
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ReturnToMainMenu();
        }
        else
        {
            SceneManager.LoadScene("Scene00_MainMenu");
        }
    }

    private void UpdateZoneAndVisuals(float currentY, float depthMeters)
    {
        // Zona dinonaktifkan sementara, fokus pada meteran kedalaman
        if (zoneNameText != null && zoneNameText.gameObject.activeSelf)
        {
            zoneNameText.gameObject.SetActive(false);
        }

        // Hitung warna fog berdasarkan kedalaman (0 sampai -100)
        float depthPercent = Mathf.Clamp01(Mathf.Abs(currentY) / Mathf.Abs(maxDepthUnits));
        Color targetColor = waterDepthGradient != null ? waterDepthGradient.Evaluate(depthPercent) : Color.black;

        RenderSettings.fogColor = targetColor;
        if (Camera.main != null) Camera.main.backgroundColor = targetColor;
    }

    // Fungsi yang dipanggil saat kursor sedang menyorot objek (melambat 20%)
    public void SetHoverSlowdown(bool slow)
    {
        isSlowedDown = slow;
    }

    // Kontrol jeda kamera dari luar (misal saat membaca panel info biota)
    public void SetPaused(bool pause)
    {
        if (isEndingTriggered) return;
        isPaused = pause;
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
        // Jangan unpause jika panel info biota masih aktif membaca
        if (BiotaInfoPanel.Instance != null && BiotaInfoPanel.Instance.panelRoot != null && BiotaInfoPanel.Instance.panelRoot.activeSelf)
        {
            yield break;
        }
        isPaused = false;
    }
}