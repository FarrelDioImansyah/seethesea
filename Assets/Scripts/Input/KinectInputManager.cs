using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class KinectInputManager : MonoBehaviour
{
    private static KinectInputManager _instance;
    public static KinectInputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<KinectInputManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("KinectInputManager");
                    _instance = go.AddComponent<KinectInputManager>();
                }
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    public enum InputMode
    {
        MouseDebug,
        WebcamMediaPipeUDP,
        KinectHardware // Placeholder untuk driver SDK Kinect v2 nanti
    }

    [Header("Mode Input")]
    public InputMode currentInputMode = InputMode.WebcamMediaPipeUDP;
    [Tooltip("Jika dicentang, Mouse & Spasi tetap bisa dipakai sebagai override saat debug")]
    public bool allowMouseOverride = true;

    [Header("UDP Configuration (Webcam MediaPipe)")]
    public int udpPort = 5065;
    [Range(1f, 30f)]
    public float smoothingSpeed = 15f;

    [Header("Idle Timeout")]
    public bool enableIdleTimeout = false;
    public float idleTimeoutSeconds = 15f;

    public Vector2 HandScreenPosition { get; private set; }
    public bool IsUserDetected { get; private set; }
    public bool IsHandRaisedAboveShoulder { get; private set; }

    [System.Serializable]
    private struct HandPayload
    {
        public float x;
        public float y;
        public bool is_raised;
        public bool detected;
    }

    private UdpClient udpClient;
    private Thread udpThread;
    private bool isRunningThread = false;

    // Data dari background thread
    private readonly object lockObject = new object();
    private HandPayload latestPayload;
    private bool hasNewPayload = false;

    private Vector2 targetScreenPosition;
    private float idleTimer = 0f;
    private Vector2 lastHandPosition;
    private Vector3 lastMousePosition;
    private bool hasReceivedFirstUdp = false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Posisi awal kursor di tengah layar
        HandScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        targetScreenPosition = HandScreenPosition;
        lastMousePosition = Input.mousePosition;

        // Auto-pasang KinectVideoReceiver jika belum ada
        if (GetComponent<KinectVideoReceiver>() == null)
        {
            gameObject.AddComponent<KinectVideoReceiver>();
        }
    }

    private void Start()
    {
        StartUdpListener();
    }

    private void StartUdpListener()
    {
        try
        {
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, udpPort));

            isRunningThread = true;
            udpThread = new Thread(ListenForUdpPackets)
            {
                IsBackground = true
            };
            udpThread.Start();
            Debug.Log($"[KinectInputManager] UDP Listener aktif di port {udpPort}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[KinectInputManager] Gagal membuka UDP port {udpPort}: {ex.Message}");
        }
    }

    private void ListenForUdpPackets()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isRunningThread && udpClient != null)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                if (data != null && data.Length > 0)
                {
                    string json = Encoding.UTF8.GetString(data);
                    HandPayload payload = JsonUtility.FromJson<HandPayload>(json);

                    lock (lockObject)
                    {
                        latestPayload = payload;
                        hasNewPayload = true;
                    }
                }
            }
            catch (SocketException)
            {
                // Socket ditutup atau timeout, keluar loop
                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[KinectInputManager] Error parsing packet: {ex.Message}");
            }
        }
    }

    private void Update()
    {
        switch (currentInputMode)
        {
            case InputMode.MouseDebug:
                ProcessMouseInput();
                break;

            case InputMode.WebcamMediaPipeUDP:
                ProcessUdpInput();
                break;

            case InputMode.KinectHardware:
                // Placeholder Kinect v2 SDK
                ProcessMouseInput();
                break;
        }

        // Mouse override jika sedang mode Webcam/Kinect untuk kemudahan tester
        if (allowMouseOverride && currentInputMode != InputMode.MouseDebug)
        {
            bool mouseMoved = (Input.mousePosition - lastMousePosition).sqrMagnitude > 0.05f;
            lastMousePosition = Input.mousePosition;

            bool mouseClickedOrSpaced = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetKey(KeyCode.Space);

            // Jika ada pergerakan/klik mouse, atau belum ada paket UDP webcam yang masuk sama sekali
            if (mouseMoved || mouseClickedOrSpaced || !hasReceivedFirstUdp)
            {
                HandScreenPosition = Input.mousePosition;
                IsUserDetected = true;
                IsHandRaisedAboveShoulder = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(1);
            }
        }

        CheckIdleTimeout();
    }

    private void ProcessMouseInput()
    {
        HandScreenPosition = Input.mousePosition;
        IsUserDetected = true;
        IsHandRaisedAboveShoulder = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(1);
    }

    private void ProcessUdpInput()
    {
        HandPayload payload;
        bool updated = false;

        lock (lockObject)
        {
            payload = latestPayload;
            updated = hasNewPayload;
        }

        if (updated)
        {
            hasReceivedFirstUdp = true;
            IsUserDetected = payload.detected;

            if (payload.detected)
            {
                // Konversi normalisasi (0..1) ke koordinat piksel layar
                float targetX = Mathf.Clamp01(payload.x) * Screen.width;
                float targetY = Mathf.Clamp01(payload.y) * Screen.height;
                targetScreenPosition = new Vector2(targetX, targetY);

                IsHandRaisedAboveShoulder = payload.is_raised;
            }
            else
            {
                IsHandRaisedAboveShoulder = false;
            }
        }

        // Smoothing dengan Lerp untuk meredam jitter mikro tangan
        HandScreenPosition = Vector2.Lerp(HandScreenPosition, targetScreenPosition, Time.deltaTime * smoothingSpeed);
    }

    private void CheckIdleTimeout()
    {
        if (!enableIdleTimeout) return;

        if (Vector2.Distance(HandScreenPosition, lastHandPosition) < 0.5f && !IsHandRaisedAboveShoulder)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTimeoutSeconds)
            {
                OnIdleTimeout();
                idleTimer = 0f;
            }
        }
        else
        {
            idleTimer = 0f;
        }

        lastHandPosition = HandScreenPosition;
    }

    private void OnIdleTimeout()
    {
        Debug.Log("[KinectInputManager] User idle. Trigger reset.");
    }

    private void CloseUdpListener()
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
        CloseUdpListener();
    }

    private void OnApplicationQuit()
    {
        CloseUdpListener();
    }
}
