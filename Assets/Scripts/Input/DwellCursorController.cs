using UnityEngine;
using UnityEngine.UI;

public class DwellCursorController : MonoBehaviour
{
    public static DwellCursorController Instance { get; private set; }

    [Header("Komponen UI Kursor")]
    public RectTransform cursorRectTransform;
    public Image radialProgressFill;
    public CanvasGroup cursorCanvasGroup;

    [Header("Layer Mask Objek Interaktif")]
    public LayerMask interactableLayer = ~0;

    [Header("Pengaturan Tampilan")]
    public float cursorSize = 60f;
    public Color normalColor = new Color(0.2f, 0.8f, 1f, 0.75f);
    public Color dwellColor = new Color(0.4f, 1f, 0.5f, 0.95f);

    private InteractableItem currentHoveredItem;
    private float currentDwellTimer = 0f;
    private Image baseCursorImage;

    // Sprite prosedural lingkaran & ring agar tidak bergantung pada asset eksternal/builtin
    private static Sprite s_RingSprite;
    private static Sprite s_CircleSprite;

    private void Awake()
    {
        Instance = this;
        Cursor.visible = true; // Biarkan kursor OS tetap tampak saat debug

        SetupCursorVisuals();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static Sprite GetRingSprite()
    {
        if (s_RingSprite != null) return s_RingSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float outerRadius = size * 0.5f - 2f;
        float innerRadius = outerRadius * 0.65f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float outerAlpha = Mathf.Clamp01(outerRadius - dist);
                float innerAlpha = Mathf.Clamp01(dist - innerRadius);
                float alpha = Mathf.Min(outerAlpha, innerAlpha);

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        s_RingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return s_RingSprite;
    }

    public static Sprite GetCircleSprite()
    {
        if (s_CircleSprite != null) return s_CircleSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        s_CircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return s_CircleSprite;
    }

    private void SetupCursorVisuals()
    {
        // Jika script ini terpasang di Canvas (seperti pada Scene 1 & 2),
        // buat atau temukan child CursorContainer agar tidak menggeser seluruh Canvas
        Canvas canvas = GetComponent<Canvas>();
        Transform targetParent = transform;

        if (canvas != null)
        {
            // Pulihkan CanvasGroup pada root canvas jika sebelumnya sempat terpasang
            CanvasGroup rootCg = GetComponent<CanvasGroup>();
            if (rootCg != null)
            {
                rootCg.alpha = 1f;
                rootCg.blocksRaycasts = true;
                rootCg.interactable = true;
            }

            Transform existingContainer = transform.Find("CursorContainer");
            if (existingContainer == null)
            {
                GameObject containerGo = new GameObject("CursorContainer", typeof(RectTransform));
                containerGo.transform.SetParent(transform, false);
                cursorRectTransform = containerGo.GetComponent<RectTransform>();
            }
            else
            {
                cursorRectTransform = existingContainer.GetComponent<RectTransform>();
            }
            targetParent = cursorRectTransform;
        }
        else
        {
            if (cursorRectTransform == null)
            {
                cursorRectTransform = GetComponent<RectTransform>();
            }
            targetParent = cursorRectTransform;
        }

        if (cursorRectTransform != null)
        {
            cursorRectTransform.sizeDelta = new Vector2(cursorSize, cursorSize);
        }

        // Pasang CanvasGroup pada container kursor (bukan pada root Canvas)
        cursorCanvasGroup = targetParent.GetComponent<CanvasGroup>();
        if (cursorCanvasGroup == null)
        {
            cursorCanvasGroup = targetParent.gameObject.AddComponent<CanvasGroup>();
        }
        cursorCanvasGroup.blocksRaycasts = false;
        cursorCanvasGroup.interactable = false;

        Sprite ringSprite = GetRingSprite();
        Sprite dotSprite = GetCircleSprite();

        // 1. Cek atau buat BaseRing Outer di dalam targetParent
        Transform bgChild = targetParent.Find("BaseRing");
        if (bgChild != null)
        {
            baseCursorImage = bgChild.GetComponent<Image>();
        }
        if (baseCursorImage == null)
        {
            GameObject bgRingGo = new GameObject("BaseRing");
            bgRingGo.transform.SetParent(targetParent, false);
            baseCursorImage = bgRingGo.AddComponent<Image>();
            baseCursorImage.raycastTarget = false;
            RectTransform bgRt = bgRingGo.GetComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(cursorSize, cursorSize);
        }
        baseCursorImage.sprite = ringSprite;
        baseCursorImage.color = new Color(0.1f, 0.2f, 0.4f, 0.6f);

        // 2. Cek atau buat Radial Progress Fill Ring di dalam targetParent
        if (radialProgressFill == null)
        {
            Transform progressChild = targetParent.Find("RadialProgress");
            if (progressChild != null)
            {
                radialProgressFill = progressChild.GetComponent<Image>();
            }
        }
        if (radialProgressFill == null)
        {
            GameObject fillRingGo = new GameObject("RadialProgress");
            fillRingGo.transform.SetParent(targetParent, false);
            radialProgressFill = fillRingGo.AddComponent<Image>();
            radialProgressFill.raycastTarget = false;
            RectTransform fillRt = fillRingGo.GetComponent<RectTransform>();
            fillRt.sizeDelta = new Vector2(cursorSize * 1.15f, cursorSize * 1.15f);
        }
        radialProgressFill.sprite = ringSprite;
        radialProgressFill.type = Image.Type.Filled;
        radialProgressFill.fillMethod = Image.FillMethod.Radial360;
        radialProgressFill.fillOrigin = (int)Image.Origin360.Top;
        radialProgressFill.fillClockwise = true;
        radialProgressFill.fillAmount = 0f;
        radialProgressFill.color = normalColor;

        // 3. Cek atau buat Center Dot di dalam targetParent
        Transform dotChild = targetParent.Find("CenterDot");
        Image centerDot = dotChild != null ? dotChild.GetComponent<Image>() : null;
        if (centerDot == null)
        {
            GameObject centerDotGo = new GameObject("CenterDot");
            centerDotGo.transform.SetParent(targetParent, false);
            centerDot = centerDotGo.AddComponent<Image>();
            centerDot.raycastTarget = false;
            RectTransform dotRt = centerDotGo.GetComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(cursorSize * 0.25f, cursorSize * 0.25f);
        }
        centerDot.sprite = dotSprite;
        centerDot.color = Color.white;
    }

    private void Update()
    {
        if (KinectInputManager.Instance == null) return;

        // Auto-hide kursor jika tangan tidak terdeteksi
        bool isDetected = KinectInputManager.Instance.IsUserDetected;
        if (cursorCanvasGroup != null)
        {
            cursorCanvasGroup.alpha = Mathf.Lerp(cursorCanvasGroup.alpha, isDetected ? 1f : 0f, Time.deltaTime * 10f);
        }

        if (!isDetected && cursorCanvasGroup != null && cursorCanvasGroup.alpha < 0.05f)
        {
            ResetHover();
            return;
        }

        // 1. Pindahkan posisi UI Kursor mengikuti posisi tangan/mouse
        Vector2 screenPos = KinectInputManager.Instance.HandScreenPosition;
        if (cursorRectTransform != null)
        {
            cursorRectTransform.position = screenPos;
        }

        // 2. Tembakkan Raycast dari kamera ke arah kursor (3D & 2D)
        Camera cam = Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, interactableLayer))
            {
                InteractableItem item = hit.collider.GetComponent<InteractableItem>();
                if (item != null && item.isInteractable)
                {
                    ProcessHover(item);
                    return;
                }
            }

            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, 500f, interactableLayer);
            if (hit2D.collider != null)
            {
                InteractableItem item2D = hit2D.collider.GetComponent<InteractableItem>();
                if (item2D != null && item2D.isInteractable)
                {
                    ProcessHover(item2D);
                    return;
                }
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

            if (DiveCameraController.Instance != null)
            {
                DiveCameraController.Instance.SetHoverSlowdown(true);
            }
        }

        currentDwellTimer += Time.deltaTime;
        float targetDuration = item.dwellDuration;
        float progress = Mathf.Clamp01(currentDwellTimer / targetDuration);

        if (radialProgressFill != null)
        {
            radialProgressFill.fillAmount = progress;
            radialProgressFill.color = Color.Lerp(normalColor, dwellColor, progress);
        }

        if (currentDwellTimer >= targetDuration)
        {
            item.TriggerInteraction();

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
        if (radialProgressFill != null)
        {
            radialProgressFill.fillAmount = 0f;
            radialProgressFill.color = normalColor;
        }

        if (DiveCameraController.Instance != null)
        {
            DiveCameraController.Instance.SetHoverSlowdown(false);
        }
    }
}
