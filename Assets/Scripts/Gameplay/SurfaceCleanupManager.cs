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

    [Header("Spawn Sampah Random (Opsional)")]
    [Tooltip("Prefab sampah yang boleh muncul secara random.")]
    public InteractableItem[] trashPrefabs;
    [Tooltip("Area berbentuk BoxCollider sebagai batas kemunculan sampah.")]
    public BoxCollider trashSpawnArea;
    [Min(0)]
    public int randomTrashCount = 10;
    [Min(0f)]
    public float minTrashSpawnDistance = 1.5f;
    public bool spawnRandomTrashOnStart = true;

    [Header("Cutscene Awal Scene 1")]
    public bool playOpeningCutscene = true;
    public AudioSource openingVoiceSource;
    public AudioClip openingVoice;
    [Tooltip("Waktu mulai spawn sampah sejak voice dimulai.")]
    [Min(0f)]
    public float trashSpawnStartTime = 6f;
    [Min(0f)]
    public float trashSpawnDelay = 0.6f;
    [Min(0f)]
    public float trashRiseDuration = 1.2f;
    [Min(0f)]
    public float trashStartBelowArea = 3f;
    [Min(0f)]
    public float openingCutsceneDuration = 8f;

    [Header("Transisi ke Scene 2")]
    public AudioSource transitionVoiceSource;
    public AudioClip transitionVoice;
    [Min(0f)]
    public float transitionCameraDistance = 5f;
    [Min(0f)]
    public float transitionCameraDuration = 4f;

    private int totalInitialTrash = 0;
    private bool isTransitioning = false;
    private bool isOpeningCutscene = false;
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

        if (trashSpawnArea != null && !trashSpawnArea.gameObject.activeInHierarchy)
        {
            Debug.LogError("[SurfaceCleanup] Trash Spawn Area tidak aktif. Aktifkan GameObject-nya agar posisi spawn tidak menumpuk di tengah.");
        }

        // Aktifkan Fog di scene ini
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        if (playOpeningCutscene)
        {
            StartCoroutine(PlayOpeningCutscene());
        }
        else
        {
            PrepareTrashList();
        }

        if (!playOpeningCutscene)
        {
            totalInitialTrash = activeTrashList.Count;
        }
        targetProgress = playOpeningCutscene ? 1f : 0f;
        currentProgress = playOpeningCutscene ? 1f : 0f;
        ApplyWaterVisuals(currentProgress);

        if (introPanel != null) introPanel.SetActive(!playOpeningCutscene);
        if (transitionText != null) transitionText.gameObject.SetActive(false);
    }

    private void PrepareTrashList()
    {
        if (spawnRandomTrashOnStart && trashPrefabs != null && trashPrefabs.Length > 0)
        {
            SpawnRandomTrash();
        }

        if (activeTrashList.Count == 0)
        {
            activeTrashList.AddRange(FindObjectsOfType<InteractableItem>());
        }
    }

    private void SpawnRandomTrash()
    {
        if (trashSpawnArea == null)
        {
            Debug.LogError("[SurfaceCleanup] Trash Spawn Area belum diisi.");
            return;
        }

        Bounds bounds = trashSpawnArea.bounds;
        List<InteractableItem> availablePrefabs = new List<InteractableItem>();
        List<Vector3> spawnPositions = new List<Vector3>();

        foreach (InteractableItem prefab in trashPrefabs)
        {
            if (prefab != null && !availablePrefabs.Contains(prefab))
            {
                availablePrefabs.Add(prefab);
            }
        }

        int spawnCount = Mathf.Min(randomTrashCount, availablePrefabs.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            int prefabIndex = Random.Range(0, availablePrefabs.Count);
            InteractableItem prefab = availablePrefabs[prefabIndex];
            availablePrefabs.RemoveAt(prefabIndex);

            Vector3 spawnPosition = GetNonOverlappingPosition(bounds, spawnPositions);
            spawnPositions.Add(spawnPosition);
            Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                * prefab.transform.rotation;

            InteractableItem spawnedTrash = Instantiate(prefab, spawnPosition, spawnRotation);
            activeTrashList.Add(spawnedTrash);
        }
    }

    private IEnumerator PlayOpeningCutscene()
    {
        isOpeningCutscene = true;

        if (openingVoiceSource == null)
        {
            openingVoiceSource = gameObject.GetComponent<AudioSource>();
            if (openingVoiceSource == null)
            {
                openingVoiceSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (openingVoice != null)
        {
            openingVoiceSource.clip = openingVoice;
            openingVoiceSource.Play();
        }
        else
        {
            Debug.LogError("[SurfaceCleanup] Opening Voice belum diisi.");
        }

        float cutsceneDuration = openingVoice != null
            ? openingVoice.length
            : openingCutsceneDuration;
        cutsceneDuration = Mathf.Max(cutsceneDuration, trashRiseDuration);

        Coroutine trashSpawnRoutine = null;
        float elapsed = 0f;
        if (trashPrefabs == null || trashPrefabs.Length == 0 || trashSpawnArea == null)
        {
            Debug.LogError("[SurfaceCleanup] Cutscene membutuhkan Trash Prefabs dan Trash Spawn Area.");
        }
        else
        {
            float spawnStartDelay = Mathf.Clamp(trashSpawnStartTime, 0f, cutsceneDuration);
            while (elapsed < spawnStartDelay)
            {
                elapsed += Time.deltaTime;
                ApplyWaterVisuals(1f - Mathf.Clamp01(elapsed / cutsceneDuration));
                yield return null;
            }
            trashSpawnRoutine = StartCoroutine(SpawnTrashDuringCutscene());
        }

        while (elapsed < cutsceneDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / cutsceneDuration);
            ApplyWaterVisuals(1f - progress);
            yield return null;
        }

        if (trashSpawnRoutine != null)
        {
            yield return trashSpawnRoutine;
        }

        ApplyWaterVisuals(0f);
        totalInitialTrash = activeTrashList.Count;
        targetProgress = 0f;
        currentProgress = 0f;
        isOpeningCutscene = false;

        if (introPanel != null) introPanel.SetActive(true);
    }

    private IEnumerator SpawnTrashDuringCutscene()
    {
        Bounds bounds = trashSpawnArea.bounds;
        List<InteractableItem> availablePrefabs = new List<InteractableItem>();

        foreach (InteractableItem prefab in trashPrefabs)
        {
            if (prefab != null && !availablePrefabs.Contains(prefab))
            {
                availablePrefabs.Add(prefab);
            }
        }

        int spawnCount = Mathf.Min(randomTrashCount, availablePrefabs.Count);
        List<Vector3> spawnPositions = new List<Vector3>();
        for (int i = 0; i < spawnCount; i++)
        {
            int prefabIndex = Random.Range(0, availablePrefabs.Count);
            InteractableItem prefab = availablePrefabs[prefabIndex];
            availablePrefabs.RemoveAt(prefabIndex);

            Vector3 targetPosition = GetNonOverlappingPosition(bounds, spawnPositions);
            spawnPositions.Add(targetPosition);
            Vector3 startPosition = targetPosition + Vector3.down * trashStartBelowArea;
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                * prefab.transform.rotation;

            InteractableItem spawnedTrash = Instantiate(prefab, startPosition, rotation);
            spawnedTrash.isInteractable = false;
            activeTrashList.Add(spawnedTrash);
            StartCoroutine(RiseTrash(spawnedTrash.transform, startPosition, targetPosition));

            yield return new WaitForSeconds(trashSpawnDelay);
        }

        yield return new WaitForSeconds(trashRiseDuration);
    }

    private IEnumerator RiseTrash(Transform trash, Vector3 startPosition, Vector3 targetPosition)
    {
        float elapsed = 0f;
        while (trash != null && elapsed < trashRiseDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / trashRiseDuration);
            trash.position = Vector3.Lerp(startPosition, targetPosition, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        if (trash != null)
        {
            trash.position = targetPosition;
            InteractableItem item = trash.GetComponent<InteractableItem>();
            if (item != null) item.isInteractable = true;
        }
    }

    private Vector3 GetNonOverlappingPosition(Bounds bounds, List<Vector3> existingPositions)
    {
        Vector3 position = bounds.center;
        int maxAttempts = 30;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            position = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));

            bool isFarEnough = true;
            foreach (Vector3 existingPosition in existingPositions)
            {
                if (Vector3.Distance(position, existingPosition) < minTrashSpawnDistance)
                {
                    isFarEnough = false;
                    break;
                }
            }

            if (isFarEnough) return position;
        }

        return position;
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
        if (isOpeningCutscene) return;

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

        if (transitionVoiceSource == null)
        {
            transitionVoiceSource = gameObject.GetComponent<AudioSource>();
            if (transitionVoiceSource == null)
            {
                transitionVoiceSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (transitionVoice != null)
        {
            transitionVoiceSource.clip = transitionVoice;
            transitionVoiceSource.Play();
        }
        else
        {
            Debug.LogError("[SurfaceCleanup] Transition Voice belum diisi.");
        }

        Camera mainCamera = Camera.main;
        Vector3 cameraStartPosition = mainCamera != null
            ? mainCamera.transform.position
            : Vector3.zero;
        float transitionDuration = transitionVoice != null
            ? Mathf.Max(transitionCameraDuration, transitionVoice.length)
            : transitionCameraDuration;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / transitionDuration);

            if (mainCamera != null)
            {
                Vector3 targetPosition = cameraStartPosition + Vector3.down * transitionCameraDistance;
                mainCamera.transform.position = Vector3.Lerp(
                    cameraStartPosition,
                    targetPosition,
                    Mathf.SmoothStep(0f, 1f, progress));
            }

            yield return null;
        }

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