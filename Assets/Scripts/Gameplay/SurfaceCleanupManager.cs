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
    public Color murkyWaterColor = new Color(0.12f, 0.25f, 0.28f); // Warna air keruh awal
    public Color clearWaterColor = new Color(0.0f, 0.65f, 0.85f);  // Warna air jernih akhir
    public float murkyFogDensity = 0.05f;
    public float clearFogDensity = 0.015f;

    [Header("Daftar Sampah di Scene")]
    public List<InteractableItem> activeTrashList = new List<InteractableItem>();

    private int totalInitialTrash = 0;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // Aktifkan Fog di scene ini
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        // Cari semua sampah yang ada di dalam scene jika list belum diisi manual
        if (activeTrashList.Count == 0)
        {
            activeTrashList.AddRange(FindObjectsOfType<InteractableItem>());
        }

        totalInitialTrash = activeTrashList.Count;
        UpdateWaterVisuals();

        if (introPanel != null) introPanel.SetActive(true);
        if (transitionText != null) transitionText.gameObject.SetActive(false);
    }

    public void OnTrashCleaned(InteractableItem cleanedTrash)
    {
        if (activeTrashList.Contains(cleanedTrash))
        {
            activeTrashList.Remove(cleanedTrash);
        }

        UpdateWaterVisuals();

        // Cek apakah seluruh sampah sudah habis
        if (activeTrashList.Count == 0 && !isTransitioning)
        {
            StartCoroutine(FinishCleanupSequence());
        }
    }

    private void UpdateWaterVisuals()
    {
        if (totalInitialTrash == 0) return;

        // Hitung persentase kebersihan (0.0 = kotor total, 1.0 = bersih total)
        float cleanedCount = totalInitialTrash - activeTrashList.Count;
        float progress = Mathf.Clamp01(cleanedCount / totalInitialTrash);

        // Ubah warna dan kepadatan fog secara dinamis
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
        SceneManager.LoadScene("Scene02_DeepSeaDive");
    }
}