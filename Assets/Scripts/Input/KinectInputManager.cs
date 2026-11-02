using UnityEngine;

public class KinectInputManager : MonoBehaviour
{
    public static KinectInputManager Instance { get; private set; }

    [Header("Mode Input")]
    public bool useMouseForDebug = true;

    [Header("Idle Timeout")]
    public float idleTimeoutSeconds = 15f;

    public Vector2 HandScreenPosition { get; private set; }
    public bool IsUserDetected { get; private set; }
    public bool IsHandRaisedAboveShoulder { get; private set; }

    private float idleTimer = 0f;
    private Vector2 lastMousePosition;

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
        }
    }

    private void Update()
    {
        if (useMouseForDebug)
        {
            HandScreenPosition = Input.mousePosition;
            IsUserDetected = true;

            // Spasi atau Klik Kanan untuk simulasi 'Angkat Tangan di atas Bahu'
            IsHandRaisedAboveShoulder = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(1);
        }

        // Deteksi Idle Timeout jika mouse tidak bergerak sama sekali
        if (Vector2.Distance(HandScreenPosition, lastMousePosition) < 0.5f && !IsHandRaisedAboveShoulder)
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

        lastMousePosition = HandScreenPosition;
    }

    private void OnIdleTimeout()
    {
        Debug.Log("[KinectInputManager] User idle lebih dari 15 detik. Trigger reset.");
        // Tempat memanggil GameStateMachine.Instance.ResetToLobby() nanti
    }
}