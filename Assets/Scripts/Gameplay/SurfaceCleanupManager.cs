using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SurfaceCleanupManager : MonoBehaviour
{
    public static SurfaceCleanupManager Instance { get; private set; }

    [Header("UI & Panduan")]
    public GameObject introPanel;              // Teks petunjuk bersihkan sampah
    public TextMeshProUGUI transitionText;     // Teks "Sekarang, lihat apa yang kamu jaga..."

    [Header("Pengaturan Lingkungan Air & Fog")]
    public Color murkyWaterColor = new Color(0.12f, 0.25f, 0.28f); // Warna fog keruh awal
    public Color clearWaterColor = new Color(0.0f, 0.65f, 0.85f);  // Warna fog jernih akhir
    public float murkyFogDensity = 0.05f;
    public float clearFogDensity = 0.015f;

    [Header("Material Shader Air (Plane Water)")]
    public Material waterMaterial;
    public Renderer waterPlaneRenderer;
    [Tooltip("Warna dasar air keruh awal pada Shader (_BaseColor)")]
    public Color murkyShaderBaseColor = new Color(0.20f, 0.26f, 0.18f, 1f); // Keruh lumut kehijauan/coklat
    [Tooltip("Warna dasar air jernih akhir pada Shader (_BaseColor)")]
    public Color clearShaderBaseColor = new Color(0.0f, 0.55f, 0.85f, 1f);  // Biru laut segar
    [Tooltip("Warna riak air saat kotor (_RippleColor)")]
    public Color murkyRippleColor = new Color(0.35f, 0.38f, 0.30f, 1f);
    [Tooltip("Warna riak air saat jernih (_RippleColor)")]
    public Color clearRippleColor = new Color(0.1f, 0.8f, 1.0f, 1f);

    [Header("Daftar Sampah di Scene")]
    public List<InteractableItem> activeTrashList = new List<InteractableItem>();

    private int totalInitialTrash = 0;
    private bool isTransitioning = false;
    private float targetProgress = 0f;
    private float currentProgress = 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (waterMaterial != null)
        {
            Destroy(waterMaterial);
        }
    }

    private void Start()
    {
        SetupWaterMaterial();

        // Aktifkan Fog di scene ini
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        // Cari semua sampah yang ada di dalam scene jika list belum diisi manual
        if (activeTrashList.Count == 0)
        {
            activeTrashList.AddRange(FindObjectsOfType<InteractableItem>());
        }

        totalInitialTrash = activeTrashList.Count;
        targetProgress = 0f;
        currentProgress = 0f;
        ApplyWaterVisuals(0f);

        if (introPanel != null) introPanel.SetActive(true);
        if (transitionText != null) transitionText.gameObject.SetActive(false);
    }

    private void SetupWaterMaterial()
    {
        if (waterPlaneRenderer == null)
        {
            GameObject plane = GameObject.Find("Plane");
            if (plane != null)
            {
                waterPlaneRenderer = plane.GetComponent<Renderer>();
            }
        }

        if (waterPlaneRenderer != null)
        {
            // Buat runtime instance material agar tidak merusak aset .mat di project
            waterMaterial = waterPlaneRenderer.material;

            // Sinkronkan juga ke WaterRipple jika ada
            WaterRipple ripple = waterPlaneRenderer.GetComponent<WaterRipple>();
            if (ripple != null)
            {
                ripple.waterMaterial = waterMaterial;
            }
        }
        else if (waterMaterial == null)
        {
            WaterRipple ripple = FindObjectOfType<WaterRipple>();
            if (ripple != null && ripple.waterMaterial != null)
            {
                waterMaterial = ripple.waterMaterial;
            }
        }
    }

    private void Update()
    {
        // Lerp transisi warna air secara halus
        if (Mathf.Abs(currentProgress - targetProgress) > 0.001f)
        {
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * 2.5f);
            ApplyWaterVisuals(currentProgress);
        }
    }

    public void OnTrashCleaned(InteractableItem cleanedTrash)
    {
        if (activeTrashList.Contains(cleanedTrash))
        {
            activeTrashList.Remove(cleanedTrash);
        }

        UpdateWaterProgress();

        // Cek apakah seluruh sampah sudah habis
        if (activeTrashList.Count == 0 && !isTransitioning)
        {
            StartCoroutine(FinishCleanupSequence());
        }
    }

    private void UpdateWaterProgress()
    {
        if (totalInitialTrash == 0) return;

        // Hitung persentase kebersihan (0.0 = kotor total, 1.0 = bersih total)
        float cleanedCount = totalInitialTrash - activeTrashList.Count;
        targetProgress = Mathf.Clamp01(cleanedCount / totalInitialTrash);
    }

    private void ApplyWaterVisuals(float progress)
    {
        // 1. Ubah warna material Shader Graph air (Plane)
        if (waterMaterial != null)
        {
            Color currentShaderColor = Color.Lerp(murkyShaderBaseColor, clearShaderBaseColor, progress);
            if (waterMaterial.HasProperty("_BaseColor"))
            {
                waterMaterial.SetColor("_BaseColor", currentShaderColor);
            }
            else if (waterMaterial.HasProperty("_Color"))
            {
                waterMaterial.SetColor("_Color", currentShaderColor);
            }

            if (waterMaterial.HasProperty("_RippleColor"))
            {
                Color currentRipple = Color.Lerp(murkyRippleColor, clearRippleColor, progress);
                waterMaterial.SetColor("_RippleColor", currentRipple);
            }
        }

        // 2. Ubah warna dan kepadatan fog lingkungan
        Color currentColor = Color.Lerp(murkyWaterColor, clearWaterColor, progress);
        float currentDensity = Mathf.Lerp(murkyFogDensity, clearFogDensity, progress);

        RenderSettings.fogColor = currentColor;
        RenderSettings.fogDensity = currentDensity;
        if (Camera.main != null) Camera.main.backgroundColor = currentColor;
    }

    private IEnumerator FinishCleanupSequence()
    {
        isTransitioning = true;
        Debug.Log("[SurfaceCleanup] Semua sampah bersih! Memulai transisi...");

        if (introPanel != null) introPanel.SetActive(false);

        // Tampilkan teks transisi emosional
        if (transitionText != null)
        {
            transitionText.gameObject.SetActive(true);
            transitionText.text = "Sekarang, lihat apa yang kamu jaga...";
        }

        // Tunggu 3.5 detik agar user menikmati air yang sudah jernih
        yield return new WaitForSeconds(3.5f);

        // Pindah ke gameplay menyelam
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.GoToDeepSeaDive();
        }
        else
        {
            SceneManager.LoadScene("Scene02_DeepSeaDive");
        }
    }
}