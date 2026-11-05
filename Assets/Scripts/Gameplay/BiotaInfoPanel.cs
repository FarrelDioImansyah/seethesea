using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BiotaInfoPanel : MonoBehaviour
{
    public static BiotaInfoPanel Instance { get; private set; }

    [Header("UI Elemen")]
    public GameObject panelRoot;
    public TextMeshProUGUI commonNameText;
    public TextMeshProUGUI scientificNameText;
    public TextMeshProUGUI depthText;
    public TextMeshProUGUI funFactText;
    public Image fishImage;

    [Header("Pengaturan Durasi")]
    [Tooltip("Waktu tampil panel sebelum tertutup otomatis (detik)")]
    public float displayDuration = 5f;

    [Header("Animasi Transisi Pop-Up")]
    public float animInDuration = 0.3f;
    public float animOutDuration = 0.2f;

    [Header("Pengaturan Posisi di Samping Ikan")]
    [Tooltip("Jarak horizontal (px) panel dari ikan di layar")]
    public float horizontalOffset = 320f;
    [Tooltip("Padding tepi layar (px) agar panel tidak terpotong")]
    public float screenPadding = 40f;

    [Header("Audio")]
    public AudioSource audioSource;

    private Coroutine autoCloseCoroutine;
    private Coroutine transitionCoroutine;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale = Vector3.one;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        originalScale = panelRoot.transform.localScale;
        if (originalScale == Vector3.zero) originalScale = Vector3.one;

        // Siapkan CanvasGroup untuk fade transparan
        canvasGroup = panelRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panelRoot.AddComponent<CanvasGroup>();
        }

        // Auto-detect fishImage jika belum di-assign di Inspector
        if (fishImage == null && panelRoot != null)
        {
            Image[] images = panelRoot.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject != panelRoot)
                {
                    fishImage = img;
                    break;
                }
            }
        }

        // Sembunyikan di awal
        panelRoot.SetActive(false);
    }

    public void ShowBiotaInfo(FishData data)
    {
        ShowBiotaInfo(data, Vector3.zero, false);
    }

    public void ShowBiotaInfo(FishData data, Vector3 worldPosition)
    {
        ShowBiotaInfo(data, worldPosition, true);
    }

    private void ShowBiotaInfo(FishData data, Vector3 worldPosition, bool useWorldPosition)
    {
        if (data == null) return;

        // Isi data teks
        if (commonNameText != null) commonNameText.text = data.commonName;
        if (scientificNameText != null) scientificNameText.text = $"<i>{data.scientificName}</i>";
        if (depthText != null) depthText.text = $"Kedalaman: {data.depthRange}";
        if (funFactText != null) funFactText.text = data.funFact;

        // Atur gambar jika tersedia
        if (fishImage != null)
        {
            if (data.illustration != null)
            {
                fishImage.sprite = data.illustration;
                fishImage.gameObject.SetActive(true);
            }
            else
            {
                fishImage.gameObject.SetActive(false);
            }
        }

        // Putar SFX discovery jika ada
        if (data.discoverySfx != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(data.discoverySfx);
            }
            else
            {
                AudioSource.PlayClipAtPoint(data.discoverySfx, Camera.main != null ? Camera.main.transform.position : transform.position);
            }
        }

        // Atur posisi panel di sebelah ikan sebelum ditampilkan
        if (useWorldPosition)
        {
            PositionPanelNearTarget(worldPosition);
        }

        // Tampilkan panel dengan animasi pop-in
        panelRoot.SetActive(true);
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(AnimatePopInRoutine());

        // Hentikan pergerakan kamera selam sementara user membaca
        if (DiveCameraController.Instance != null)
        {
            DiveCameraController.Instance.SetPaused(true);
        }

        // Reset dan mulai hitung mundur 5 detik
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }
        autoCloseCoroutine = StartCoroutine(AutoCloseRoutine(displayDuration));
    }

    private IEnumerator AnimatePopInRoutine()
    {
        float elapsed = 0f;
        Vector3 startScale = originalScale * 0.5f;

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        panelRoot.transform.localScale = startScale;

        while (elapsed < animInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animInDuration);

            // Ease-out back (sedikit membal/bounce saat pop-up)
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float easeBack = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

            panelRoot.transform.localScale = Vector3.LerpUnclamped(startScale, originalScale, easeBack);

            // Fade in alpha
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            }

            yield return null;
        }

        panelRoot.transform.localScale = originalScale;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        transitionCoroutine = null;
    }

    private void PositionPanelNearTarget(Vector3 worldPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        // Jika ikan di belakang kamera, abaikan
        if (screenPos.z < 0) return;

        float panelHalfWidth = panelRect.rect.width * 0.5f;
        float panelHalfHeight = panelRect.rect.height * 0.5f;

        // Tentukan apakah muncul di kanan atau di kiri ikan
        // Jika ikan di paruh kanan layar -> taruh di kiri ikan, sebaliknya taruh di kanan
        float targetX;
        if (screenPos.x > Screen.width * 0.5f)
        {
            targetX = screenPos.x - horizontalOffset;
        }
        else
        {
            targetX = screenPos.x + horizontalOffset;
        }

        float targetY = screenPos.y;

        // Clamp agar panel tidak tembus batas layar
        float minX = panelHalfWidth + screenPadding;
        float maxX = Screen.width - panelHalfWidth - screenPadding;
        float minY = panelHalfHeight + screenPadding;
        float maxY = Screen.height - panelHalfHeight - screenPadding;

        targetX = Mathf.Clamp(targetX, minX, maxX);
        targetY = Mathf.Clamp(targetY, minY, maxY);

        panelRect.position = new Vector3(targetX, targetY, 0f);
    }

    public void ClosePanel()
    {
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        if (panelRoot != null && panelRoot.activeSelf)
        {
            transitionCoroutine = StartCoroutine(AnimatePopOutRoutine());
        }
    }

    private IEnumerator AnimatePopOutRoutine()
    {
        float elapsed = 0f;
        Vector3 targetScale = originalScale * 0.7f;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        while (elapsed < animOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animOutDuration);

            panelRoot.transform.localScale = Vector3.Lerp(originalScale, targetScale, t * t);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            }

            yield return null;
        }

        panelRoot.SetActive(false);
        panelRoot.transform.localScale = originalScale;
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        // Lanjutkan pergerakan kamera selam
        if (DiveCameraController.Instance != null)
        {
            DiveCameraController.Instance.SetPaused(false);
        }

        transitionCoroutine = null;
    }

    private IEnumerator AutoCloseRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        ClosePanel();
    }
}
