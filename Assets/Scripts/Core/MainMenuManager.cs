using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("Video Player")]
    public VideoPlayer videoPlayer;
    public VideoClip opening0Clip;
    public VideoClip opening1Clip;

    [Header("UI Panels")]
    public GameObject attractPanel;   // Teks "Angkat tanganmu untuk memulai"
    public CanvasGroup attractPanelCanvasGroup;
    public GameObject menuPanel;      // Panel berisi 2 tombol (Story & Direct)
    public CanvasGroup menuPanelCanvasGroup;

    [Header("Fade Transition")]
    public Image fadeOverlayImage;    // Image hitam untuk crossfade video -> scene 3D
    public float fadeDuration = 0.8f;

    [Header("Status")]
    public bool isMenuOpen = false;
    private bool isTransitioning = false;

    [Header("Tombol 3D & Objek")]
    public GameObject menuButtons3DGroup;
    public GameObject earthObject;

    private FloatingObject[] buttonFloaters;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnOpening0Finished;
            videoPlayer.loopPointReached -= OnOpening1Finished;
        }
    }

    private void Start()
    {
        SetupFadeOverlay();
        SetupButtonFloaters();
        SetupVideoPlayer();
        ShowAttractMode();
    }

    private void SetupFadeOverlay()
    {
        if (fadeOverlayImage == null)
        {
            // Buat Canvas Fade Overlay otomatis jika belum ada di Inspector
            GameObject canvasGo = new GameObject("FadeCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject imgGo = new GameObject("FadeImage");
            imgGo.transform.SetParent(canvasGo.transform, false);
            fadeOverlayImage = imgGo.AddComponent<Image>();
            fadeOverlayImage.color = new Color(0, 0, 0, 0);

            RectTransform rt = imgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            fadeOverlayImage.raycastTarget = false;
        }
    }

    private void SetupButtonFloaters()
    {
        if (menuButtons3DGroup != null)
        {
            buttonFloaters = menuButtons3DGroup.GetComponentsInChildren<FloatingObject>(true);
            if (buttonFloaters == null || buttonFloaters.Length == 0)
            {
                // Pasang otomatis ke child buttons jika belum terpasang
                for (int i = 0; i < menuButtons3DGroup.transform.childCount; i++)
                {
                    Transform child = menuButtons3DGroup.transform.GetChild(i);
                    FloatingObject fo = child.GetComponent<FloatingObject>();
                    if (fo == null) fo = child.gameObject.AddComponent<FloatingObject>();
                    fo.timeOffset = i * 0.5f;
                }
                buttonFloaters = menuButtons3DGroup.GetComponentsInChildren<FloatingObject>(true);
            }
        }

        if (menuPanel != null && menuPanelCanvasGroup == null)
        {
            menuPanelCanvasGroup = menuPanel.GetComponent<CanvasGroup>();
            if (menuPanelCanvasGroup == null)
            {
                menuPanelCanvasGroup = menuPanel.AddComponent<CanvasGroup>();
            }
        }

        if (attractPanel != null && attractPanelCanvasGroup == null)
        {
            attractPanelCanvasGroup = attractPanel.GetComponent<CanvasGroup>();
            if (attractPanelCanvasGroup == null)
            {
                attractPanelCanvasGroup = attractPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void SetupVideoPlayer()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }
        }

        if (videoPlayer != null && Camera.main != null)
        {
            videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
            videoPlayer.targetCamera = Camera.main;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.playOnAwake = false;
        }
    }

    private void Update()
    {
        // Deteksi angkat tangan (Spasi di mode mouse)
        if (!isMenuOpen && !isTransitioning && KinectInputManager.Instance != null)
        {
            if (KinectInputManager.Instance.IsHandRaisedAboveShoulder)
            {
                StartOpeningTransition();
            }
        }
    }

    public void ShowAttractMode()
    {
        isMenuOpen = false;
        isTransitioning = false;

        if (fadeOverlayImage != null)
        {
            fadeOverlayImage.color = new Color(0, 0, 0, 0);
        }

        if (attractPanel != null)
        {
            attractPanel.SetActive(false);
            if (attractPanelCanvasGroup != null) attractPanelCanvasGroup.alpha = 0f;
            attractPanel.transform.localScale = Vector3.zero;
        }

        if (menuPanel != null) menuPanel.SetActive(false);
        if (menuButtons3DGroup != null) menuButtons3DGroup.SetActive(false);
        if (earthObject != null) earthObject.SetActive(false);

        if (videoPlayer != null && opening0Clip != null)
        {
            videoPlayer.clip = opening0Clip;
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnOpening0Finished;
            videoPlayer.Play();
        }
        else
        {
            ShowAttractPanelPopUp();
        }
    }

    private void OnOpening0Finished(VideoPlayer source)
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnOpening0Finished;
        }

        ShowAttractPanelPopUp();
    }

    public void ShowAttractPanelPopUp()
    {
        if (attractPanel != null)
        {
            attractPanel.SetActive(true);
            StopCoroutine(nameof(AnimateAttractPanelPopUpRoutine));
            StartCoroutine(nameof(AnimateAttractPanelPopUpRoutine));
        }
    }

    private IEnumerator AnimateAttractPanelPopUpRoutine()
    {
        RectTransform rt = attractPanel.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = Vector2.zero; // posisi 0, 0

        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Back out overshoot untuk efek pop-up kenyal
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float easeBackOut = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

            attractPanel.transform.localScale = Vector3.one * Mathf.Max(0f, easeBackOut);
            if (attractPanelCanvasGroup != null)
            {
                attractPanelCanvasGroup.alpha = t;
            }

            yield return null;
        }

        attractPanel.transform.localScale = Vector3.one;
        if (attractPanelCanvasGroup != null) attractPanelCanvasGroup.alpha = 1f;
    }

    public void StartOpeningTransition()
    {
        if (isTransitioning || isMenuOpen) return;

        isTransitioning = true;

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnOpening0Finished;
        }

        StartCoroutine(StartOpeningTransitionRoutine());
    }

    private IEnumerator StartOpeningTransitionRoutine()
    {
        // Fade out attractPanel secara halus
        if (attractPanel != null && attractPanel.activeSelf)
        {
            float fadeOutDuration = 0.35f;
            float elapsed = 0f;
            Vector3 initialScale = attractPanel.transform.localScale;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);

                if (attractPanelCanvasGroup != null)
                {
                    attractPanelCanvasGroup.alpha = 1f - t;
                }
                attractPanel.transform.localScale = Vector3.Lerp(initialScale, initialScale * 0.8f, t);
                yield return null;
            }

            attractPanel.SetActive(false);
        }

        if (videoPlayer != null && opening1Clip != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = opening1Clip;
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnOpening1Finished;
            videoPlayer.Play();
        }
        else
        {
            StartCoroutine(TransitionToEarthAndMenuRoutine());
        }
    }

    private void OnOpening1Finished(VideoPlayer source)
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnOpening1Finished;
        }

        StartCoroutine(TransitionToEarthAndMenuRoutine());
    }

    private IEnumerator TransitionToEarthAndMenuRoutine()
    {
        // 1. Fade out ke hitam halus agar tidak terpotong kasar
        if (fadeOverlayImage != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / (fadeDuration * 0.5f));
                fadeOverlayImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeOverlayImage.color = new Color(0, 0, 0, 1);
        }

        // Hentikan video player
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        // Aktifkan Earth
        if (earthObject != null)
        {
            earthObject.SetActive(true);
        }

        // 2. Fade in dari hitam ke Earth 3D
        if (fadeOverlayImage != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - (elapsed / (fadeDuration * 0.5f)));
                fadeOverlayImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeOverlayImage.color = new Color(0, 0, 0, 0);
        }

        // 3. Munculkan tombol 3D dari bawah dengan animasi mengambang
        if (menuButtons3DGroup != null)
        {
            menuButtons3DGroup.SetActive(true);
            if (buttonFloaters != null)
            {
                for (int i = 0; i < buttonFloaters.Length; i++)
                {
                    if (buttonFloaters[i] != null)
                    {
                        buttonFloaters[i].PlayRiseAndFloat(i * 0.15f);
                    }
                }
            }
        }

        // 4. Setelah tombol muncul, teks instruksi meluncur dari bawah & fade in
        yield return new WaitForSeconds(0.4f);
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            StartCoroutine(AnimateMenuPanelTextRoutine());
        }

        isTransitioning = false;
        isMenuOpen = true;
    }

    private IEnumerator AnimateMenuPanelTextRoutine()
    {
        RectTransform rt = menuPanel.GetComponent<RectTransform>();
        Vector2 targetPos = rt != null ? rt.anchoredPosition : Vector2.zero;
        Vector2 startPos = targetPos - new Vector2(0, 80f); // dari bawah

        if (menuPanelCanvasGroup != null)
        {
            menuPanelCanvasGroup.alpha = 0f;
        }

        if (rt != null)
        {
            rt.anchoredPosition = startPos;
        }

        float duration = 0.8f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);

            if (rt != null)
            {
                rt.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, easeOut);
            }

            if (menuPanelCanvasGroup != null)
            {
                menuPanelCanvasGroup.alpha = easeOut;
            }

            yield return null;
        }

        if (rt != null) rt.anchoredPosition = targetPos;
        if (menuPanelCanvasGroup != null) menuPanelCanvasGroup.alpha = 1f;
    }

    public void OpenMenu()
    {
        StartCoroutine(TransitionToEarthAndMenuRoutine());
    }

    public void SelectStoryMode()
    {
        Debug.Log("[MainMenu] Memilih Story Mode -> Pindah ke Scene01_SurfaceCleanup");
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartStoryMode();
        }
        else
        {
            SceneManager.LoadScene("Scene01_SurfaceCleanup");
        }
    }

    public void SelectDirectDiveMode()
    {
        Debug.Log("[MainMenu] Memilih Langsung Selam -> Pindah ke Scene02_DeepSeaDive");
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartDirectDive();
        }
        else
        {
            SceneManager.LoadScene("Scene02_DeepSeaDive");
        }
    }

}