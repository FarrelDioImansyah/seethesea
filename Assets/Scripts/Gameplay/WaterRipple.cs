using UnityEngine;

public class WaterRipple : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Material waterMaterial;

    [Header("Ripple Settings")]
    public float rippleDuration = 1f;
    public float fadeDuration = 0.5f;
    public float rippleSpeed = 1f;

    private float rippleTime;
    private bool rippleActive;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        waterMaterial.SetFloat("_RippleOpacity", 0f);
        waterMaterial.SetFloat("_RippleTime", 0f);
    }

    void Update()
    {
        // Klik air
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    Vector2 uv = hit.textureCoord;

                    waterMaterial.SetVector(
                        "_RippleCenter",
                        new Vector4(uv.x, uv.y, 0, 0)
                    );

                    // RESET
                    rippleTime = 0f;
                    rippleActive = true;

                    waterMaterial.SetFloat("_RippleTime", 0f);
                    waterMaterial.SetFloat("_RippleOpacity", 1f);
                }
            }
        }

        if (!rippleActive)
            return;

        rippleTime += Time.deltaTime;

        // Gerakan ripple
        waterMaterial.SetFloat(
            "_RippleTime",
            rippleTime * rippleSpeed
        );

        // =========================
        // FADE OUT
        // =========================

        if (rippleTime > rippleDuration)
        {
            float fadeProgress =
                (rippleTime - rippleDuration) / fadeDuration;

            fadeProgress = Mathf.Clamp01(fadeProgress);

            // 1 → 0
            float opacity = 1f - fadeProgress;

            waterMaterial.SetFloat(
                "_RippleOpacity",
                opacity
            );

            // Selesai
            if (fadeProgress >= 1f)
            {
                waterMaterial.SetFloat(
                    "_RippleOpacity",
                    0f
                );

                rippleActive = false;
            }
        }
    }
}