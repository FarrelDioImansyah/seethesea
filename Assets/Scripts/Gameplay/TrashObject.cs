using System.Collections;
using UnityEngine;
public class TrashObject : MonoBehaviour
{
    public enum DestroyMode
    {
        Tap,
        Hold
    }

    [Header("Destroy Mode")]
    public DestroyMode destroyMode = DestroyMode.Tap;

    [Header("References")]
    public Camera cam;

    [Tooltip("Renderer dari objek sampah")]
    public Renderer objectRenderer;

    [Header("Tap Settings")]
    [Tooltip("Durasi animasi sebelum objek hilang")]
    public float tapDuration = 0.4f;

    [Tooltip("Seberapa besar objek membesar")]
    public float scaleMultiplier = 1.15f;

    [Header("Hold Settings")]
    [Tooltip("Berapa detik harus menahan klik")]
    public float holdDuration = 1.5f;

    [Tooltip("Seberapa besar objek membesar saat berhasil")]
    public float holdScaleMultiplier = 1.15f;

    [Header("Disappear Settings")]
    [Tooltip("Durasi perubahan warna menjadi putih")]
    public float whiteFadeDuration = 0.3f;

    [Tooltip("Durasi scale up sebelum hilang")]
    public float scaleUpDuration = 0.3f;

    [Header("Floating Idle")]
    [Tooltip("Kecepatan gerakan terapung")]
    public float floatSpeed = 1f;

    [Tooltip("Tinggi gerakan naik turun")]
    public float floatHeight = 0.08f;

    [Tooltip("Gerakan kiri kanan")]
    public float floatSideAmount = 0.03f;

    [Tooltip("Kecepatan rotasi")]
    public float rotationSpeed = 15f;

    // ============================
    // PRIVATE
    // ============================

    private Material objectMaterial;

    private Color originalColor;
    private Vector3 originalScale;
    private Vector3 originalPosition;

    private float holdTimer = 0f;

    private bool isHolding = false;
    private bool isDestroying = false;

    private float randomOffset;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        if (objectRenderer == null)
            objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
        {
            // Membuat material instance khusus untuk objek ini
            objectMaterial = objectRenderer.material;

            // Simpan warna awal
            if (objectMaterial.HasProperty("_BaseColor"))
            {
                originalColor =
                    objectMaterial.GetColor("_BaseColor");
            }
            else if (objectMaterial.HasProperty("_Color"))
            {
                originalColor =
                    objectMaterial.GetColor("_Color");
            }
        }

        originalScale = transform.localScale;
        originalPosition = transform.localPosition;

        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (isDestroying)
            return;

        // =========================================
        // IDLE FLOATING
        // =========================================

        if (!isHolding)
        {
            FloatingMovement();
        }

        // =========================================
        // INPUT
        // =========================================

        if (destroyMode == DestroyMode.Tap)
        {
            HandleTap();
        }
        else
        {
            HandleHold();
        }
    }

    // =========================================================
    // FLOATING
    // =========================================================

    void FloatingMovement()
    {
        float time = Time.time * floatSpeed + randomOffset;

        float vertical =
            Mathf.Sin(time) * floatHeight;

        float horizontal =
            Mathf.Sin(time * 0.7f) * floatSideAmount;

        Vector3 targetPosition = originalPosition;

        targetPosition.y += vertical;
        targetPosition.x += horizontal;

        transform.localPosition =
            Vector3.Lerp(
                transform.localPosition,
                targetPosition,
                Time.deltaTime * 3f
            );

        // Sedikit rotasi seperti terapung
        float rotX =
            Mathf.Sin(time * 0.8f) * 3f;

        float rotZ =
            Mathf.Cos(time * 0.6f) * 3f;

        Quaternion targetRotation =
            Quaternion.Euler(
                rotX,
                0f,
                rotZ
            );

        transform.localRotation =
            Quaternion.Lerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * 2f
            );
    }

    // =========================================================
    // TAP MODE
    // =========================================================

    void HandleTap()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsClicked())
            {
                StartCoroutine(TapDestroy());
            }
        }
    }

    IEnumerator TapDestroy()
    {
        isDestroying = true;

        float timer = 0f;

        Vector3 startScale = transform.localScale;

        Vector3 targetScale =
            originalScale * scaleMultiplier;

        Color startColor = originalColor;

        while (timer < tapDuration)
        {
            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / tapDuration
                );

            // Scale up
            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    progress
                );

            // Putih
            SetColor(
                Color.Lerp(
                    startColor,
                    Color.white,
                    progress
                )
            );

            yield return null;
        }

        Destroy(gameObject);
    }

    // =========================================================
    // HOLD MODE
    // =========================================================

    void HandleHold()
    {
        // Mulai menahan
        if (Input.GetMouseButtonDown(0))
        {
            if (IsClicked())
            {
                isHolding = true;
                holdTimer = 0f;
            }
        }

        // Sedang menahan
        if (isHolding &&
            Input.GetMouseButton(0))
        {
            holdTimer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    holdTimer / holdDuration
                );

            // =====================================
            // INDIKATOR PUTIH
            // =====================================

            SetColor(
                Color.Lerp(
                    originalColor,
                    Color.white,
                    progress
                )
            );

            // Sedikit membesar selama ditahan
            float scaleProgress =
                progress * 0.05f;

            transform.localScale =
                originalScale *
                (1f + scaleProgress);

            // =====================================
            // SELESAI HOLD
            // =====================================

            if (progress >= 1f)
            {
                isHolding = false;

                StartCoroutine(
                    HoldDestroy()
                );
            }
        }

        // =====================================
        // MOUSE DILEPAS
        // =====================================

        if (isHolding &&
            Input.GetMouseButtonUp(0))
        {
            isHolding = false;

            // RESET TIMER
            holdTimer = 0f;

            // RESET WARNA
            SetColor(originalColor);

            // RESET SCALE
            transform.localScale =
                originalScale;
        }
    }

    // =========================================================
    // HOLD DESTROY
    // =========================================================

    IEnumerator HoldDestroy()
    {
        isDestroying = true;

        float timer = 0f;

        Vector3 startScale =
            transform.localScale;

        Vector3 targetScale =
            originalScale *
            holdScaleMultiplier;

        Color startColor =
            objectMaterial != null
                ? GetCurrentColor()
                : originalColor;

        while (timer < scaleUpDuration)
        {
            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / scaleUpDuration
                );

            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    progress
                );

            SetColor(
                Color.Lerp(
                    startColor,
                    Color.white,
                    progress
                )
            );

            yield return null;
        }

        // =====================================
        // FADE / PUTIH
        // =====================================

        timer = 0f;

        while (timer < whiteFadeDuration)
        {
            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / whiteFadeDuration
                );

            SetColor(
                Color.Lerp(
                    startColor,
                    Color.white,
                    progress
                )
            );

            yield return null;
        }

        // Hilangkan objek
        Destroy(gameObject);
    }

    // =========================================================
    // RAYCAST
    // =========================================================

    bool IsClicked()
    {
        if (cam == null)
            return false;

        Ray ray =
            cam.ScreenPointToRay(
                Input.mousePosition
            );

        if (Physics.Raycast(
            ray,
            out RaycastHit hit))
        {
            return hit.transform == transform ||
                   hit.transform.IsChildOf(transform);
        }

        return false;
    }

    // =========================================================
    // MATERIAL COLOR
    // =========================================================

    void SetColor(Color color)
    {
        if (objectMaterial == null)
            return;

        if (objectMaterial.HasProperty("_BaseColor"))
        {
            objectMaterial.SetColor(
                "_BaseColor",
                color
            );
        }
        else if (objectMaterial.HasProperty("_Color"))
        {
            objectMaterial.SetColor(
                "_Color",
                color
            );
        }
    }

    Color GetCurrentColor()
    {
        if (objectMaterial.HasProperty("_BaseColor"))
        {
            return objectMaterial.GetColor(
                "_BaseColor"
            );
        }

        if (objectMaterial.HasProperty("_Color"))
        {
            return objectMaterial.GetColor(
                "_Color"
            );
        }

        return originalColor;
    }
}
