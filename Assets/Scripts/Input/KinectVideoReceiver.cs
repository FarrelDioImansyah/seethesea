using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class KinectVideoReceiver : MonoBehaviour
{
    public static KinectVideoReceiver Instance { get; private set; }

    [Header("Pengaturan Jaringan UDP")]
    [Tooltip("Port khusus untuk streaming video/frame kamera (berbeda dari port 5065 koordinat tangan)")]
    public int videoPort = 5066;

    [Header("Tampilan UI (Picture-in-Picture)")]
    public RawImage displayImage;
    public RectTransform previewContainer;
    public CanvasGroup previewCanvasGroup;

    [Header("Pengaturan Tampilan")]
    [Tooltip("Tampilkan atau sembunyikan kotak preview kamera")]
    public bool showPreview = true;
    [Tooltip("Sembunyikan otomatis jika tidak ada frame kamera yang masuk")]
    public bool autoHideWhenNoSignal = false;
    [Tooltip("Ukuran lebar kotak preview kamera di layar (tinggi otomatis 16:9)")]
    public float previewWidth = 240f;
    [Tooltip("Tombol keyboard untuk memunculkan/menutup kotak kamera saat pameran")]
    public KeyCode toggleKey = KeyCode.C;

    private UdpClient udpClient;
    private Thread udpThread;
    private bool isRunningThread = false;

    private readonly object lockObject = new object();
    private byte[] latestFrameBytes;
    private bool hasNewFrame = false;

    private Texture2D videoTexture;
    private float lastFrameTime = 0f;
    private bool hasReceivedFirstFrame = false;
    private Text placeholderText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Texture2D reusable untuk me-render JPEG
        videoTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);

        SetupPreviewUI();
    }

    private void Start()
    {
        StartUdpReceiver();
    }

    private void SetupPreviewUI()
    {
        if (displayImage != null) return;

        // Auto-create Canvas & RawImage untuk PiP preview jika belum dipasang di Inspector
        GameObject canvasGo = new GameObject("KinectCameraCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998; // Tepat di bawah FadeCanvas (999) dan kursor (999)
        canvasGo.AddComponent<GraphicRaycaster>();

        // Container Kotak di Pojok Kanan Atas
        GameObject containerGo = new GameObject("CameraPreviewBox");
        containerGo.transform.SetParent(canvasGo.transform, false);
        previewContainer = containerGo.AddComponent<RectTransform>();
        previewCanvasGroup = containerGo.AddComponent<CanvasGroup>();
        previewCanvasGroup.blocksRaycasts = false;
        previewCanvasGroup.interactable = false;

        float previewHeight = previewWidth * (9f / 16f); // Rasio 16:9
        previewContainer.anchorMin = new Vector2(1f, 1f); // Pojok Kanan Atas
        previewContainer.anchorMax = new Vector2(1f, 1f);
        previewContainer.pivot = new Vector2(1f, 1f);
        previewContainer.anchoredPosition = new Vector2(-25f, -25f); // Padding 25px dari tepi
        previewContainer.sizeDelta = new Vector2(previewWidth, previewHeight);

        // Frame / Background gelap berbingkai
        Image bgFrame = containerGo.AddComponent<Image>();
        bgFrame.color = new Color(0.05f, 0.12f, 0.18f, 0.85f);

        // RawImage untuk me-render video di dalam container
        GameObject imgGo = new GameObject("VideoDisplay");
        imgGo.transform.SetParent(containerGo.transform, false);
        RectTransform imgRt = imgGo.AddComponent<RectTransform>();
        imgRt.anchorMin = Vector2.zero;
        imgRt.anchorMax = Vector2.one;
        imgRt.offsetMin = new Vector2(4f, 4f); // Padding border 4px
        imgRt.offsetMax = new Vector2(-4f, -4f);

        displayImage = imgGo.AddComponent<RawImage>();
        displayImage.raycastTarget = false;
        displayImage.color = new Color(0.1f, 0.15f, 0.22f, 1f); // Warna gelap saat menunggu kamera
        displayImage.texture = videoTexture;

        // Teks Placeholder "Menunggu Kamera..."
        GameObject textGo = new GameObject("PlaceholderText");
        textGo.transform.SetParent(imgGo.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        placeholderText = textGo.AddComponent<Text>();
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (placeholderText.font == null) placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        placeholderText.text = "Menunggu Kamera\n(Port 5066)...";
        placeholderText.alignment = TextAnchor.MiddleCenter;
        placeholderText.fontSize = 13;
        placeholderText.color = new Color(0.6f, 0.8f, 1f, 0.8f);
        placeholderText.raycastTarget = false;

        // Atur alpha awal
        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.alpha = autoHideWhenNoSignal ? 0f : 1f;
        }
    }

    private void StartUdpReceiver()
    {
        try
        {
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            // Alokasikan buffer 2MB agar penerimaan frame video lancar tanpa drop
            udpClient.Client.ReceiveBufferSize = 2 * 1024 * 1024;
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, videoPort));

            isRunningThread = true;
            udpThread = new Thread(ListenForVideoFrames)
            {
                IsBackground = true
            };
            udpThread.Start();
            Debug.Log($"[KinectVideoReceiver] Video Stream Listener aktif di port {videoPort}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[KinectVideoReceiver] Gagal membuka Video UDP port {videoPort}: {ex.Message}");
        }
    }

    private void ListenForVideoFrames()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isRunningThread && udpClient != null)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                if (data != null && data.Length > 0)
                {
                    lock (lockObject)
                    {
                        latestFrameBytes = data;
                        hasNewFrame = true;
                    }
                }
            }
            catch (SocketException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[KinectVideoReceiver] Error menerima frame video: {ex.Message}");
            }
        }
    }

    private void Update()
    {
        // Toggle preview dengan tombol shortcut (default 'C')
        if (Input.GetKeyDown(toggleKey))
        {
            showPreview = !showPreview;
            Debug.Log($"[KinectVideoReceiver] Camera Preview toggled: {(showPreview ? "ON" : "OFF")}");
        }

        byte[] frameToRender = null;
        bool isNew = false;

        lock (lockObject)
        {
            if (hasNewFrame)
            {
                frameToRender = latestFrameBytes;
                hasNewFrame = false;
                isNew = true;
            }
        }

        if (isNew && frameToRender != null)
        {
            // Muat byte JPEG ke Texture2D secara efisien
            videoTexture.LoadImage(frameToRender);
            if (displayImage != null)
            {
                displayImage.texture = videoTexture;
                displayImage.color = Color.white;
            }

            if (!hasReceivedFirstFrame)
            {
                hasReceivedFirstFrame = true;
                Debug.Log("[KinectVideoReceiver] Live video stream terhubung dari Python!");
                if (placeholderText != null)
                {
                    placeholderText.gameObject.SetActive(false);
                }
            }

            lastFrameTime = Time.time;
        }

        // Kontrol fade-in/fade-out kotak preview
        if (previewCanvasGroup != null)
        {
            bool hasSignal = (Time.time - lastFrameTime) < 2.0f; // Sinyal aktif jika frame masuk dalam 2 detik terakhir
            if (!hasSignal && placeholderText != null)
            {
                placeholderText.gameObject.SetActive(true);
            }

            float targetAlpha = (showPreview && (!autoHideWhenNoSignal || hasSignal)) ? 1f : 0f;
            previewCanvasGroup.alpha = Mathf.Lerp(previewCanvasGroup.alpha, targetAlpha, Time.deltaTime * 8f);
        }
    }

    private void StopUdpReceiver()
    {
        isRunningThread = false;

        if (udpClient != null)
        {
            try
            {
                udpClient.Close();
                udpClient = null;
            }
            catch { }
        }

        if (udpThread != null && udpThread.IsAlive)
        {
            try
            {
                udpThread.Abort();
            }
            catch { }
            udpThread = null;
        }
    }

    private void OnDestroy()
    {
        StopUdpReceiver();
        if (videoTexture != null)
        {
            Destroy(videoTexture);
        }
    }

    private void OnApplicationQuit()
    {
        StopUdpReceiver();
    }
}

