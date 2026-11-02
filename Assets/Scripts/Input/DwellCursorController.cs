using UnityEngine;
using UnityEngine.UI;

public class DwellCursorController : MonoBehaviour
{
    public static DwellCursorController Instance { get; private set; }

    [Header("Komponen UI Kursor")]
    public RectTransform cursorRectTransform;
    public Image radialProgressFill;

    [Header("Layer Mask Objek Interaktif")]
    public LayerMask interactableLayer = ~0;

    private InteractableItem currentHoveredItem;
    private float currentDwellTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        Cursor.visible = true; // Biarkan kursor OS tetap tampak saat debug
    }

    private void Update()
    {
        if (KinectInputManager.Instance == null) return;

        // 1. Pindahkan posisi UI Kursor mengikuti posisi tangan/mouse
        Vector2 screenPos = KinectInputManager.Instance.HandScreenPosition;
        if (cursorRectTransform != null)
        {
            cursorRectTransform.position = screenPos;
        }

        // 2. Tembakkan Raycast dari kamera ke arah kursor
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableLayer))
        {
            InteractableItem item = hit.collider.GetComponent<InteractableItem>();
            if (item != null && item.isInteractable)
            {
                ProcessHover(item);
                return;
            }
        }

        // 3. Reset jika kursor keluar dari objek
        ResetHover();
    }

    private void ProcessHover(InteractableItem item)
    {
        if (currentHoveredItem != item)
        {
            currentHoveredItem = item;
            currentDwellTimer = 0f;

            // Beri tahu kamera untuk melambat (20%) saat objek disorot
            if (DiveCameraController.Instance != null)
            {
                DiveCameraController.Instance.SetHoverSlowdown(true);
            }
        }

        currentDwellTimer += Time.deltaTime;
        float targetDuration = item.dwellDuration;
        float progress = Mathf.Clamp01(currentDwellTimer / targetDuration);

        if (radialProgressFill != null) radialProgressFill.fillAmount = progress;

        if (currentDwellTimer >= targetDuration)
        {
            item.TriggerInteraction();

            // Pause kamera sejenak agar user sempat membaca
            if (DiveCameraController.Instance != null)
            {
                DiveCameraController.Instance.PauseForReading(3.5f);
            }

            ResetHover();
        }
    }

    private void ResetHover()
    {
        currentHoveredItem = null;
        currentDwellTimer = 0f;
        if (radialProgressFill != null) radialProgressFill.fillAmount = 0f;

        // Kembalikan kamera ke kecepatan normal
        if (DiveCameraController.Instance != null)
        {
            DiveCameraController.Instance.SetHoverSlowdown(false);
        }
    }

}