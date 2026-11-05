using System.Collections;
using UnityEngine;

public class DwellDissolveEffect : MonoBehaviour
{
    [Header("Animasi Durasi & Skala")]
    [Tooltip("Total durasi efek dari awal sampai destroy")]
    public float totalDuration = 1.0f;

    [Tooltip("Pengali skala saat membesar")]
    public float maxScaleMultiplier = 1.5f;

    [Header("Visual")]
    public Color flashColor = Color.white;

    private SpriteRenderer spriteRenderer;
    private Renderer meshRenderer;
    private Material runtimeMaterial;
    private Color originalColor = Color.white;
    private Vector3 originalScale;
    private bool isPlaying = false;

    private void Awake()
    {
        originalScale = transform.localScale;

        // Auto detect 2D Sprite vs 3D Mesh
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            meshRenderer = GetComponent<Renderer>();
            if (meshRenderer != null)
            {
                // Instantiate material runtime agar tidak ubah asset material asli di project
                runtimeMaterial = meshRenderer.material;
                if (runtimeMaterial.HasProperty("_BaseColor"))
                {
                    originalColor = runtimeMaterial.GetColor("_BaseColor");
                }
                else if (runtimeMaterial.HasProperty("_Color"))
                {
                    originalColor = runtimeMaterial.color;
                }
            }
        }
    }

    public void Play()
    {
        if (isPlaying) return;
        StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        isPlaying = true;

        // Matikan collider agar raycast kursor tidak lagi mengenai objek saat animasi
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null) col2D.enabled = false;

        float phase1Duration = totalDuration * 0.35f; // Fase membesar + mulai memutih
        float phase2Duration = totalDuration * 0.65f; // Fase memutih maksimal -> memudar hilang
        float elapsed = 0f;

        Vector3 targetBigScale = originalScale * maxScaleMultiplier;

        // Fase 1: Membesar & bergeser ke warna putih
        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / phase1Duration);

            // Lerp scale ke 1.5x
            transform.localScale = Vector3.Lerp(originalScale, targetBigScale, Mathf.SmoothStep(0f, 1f, t));

            // Lerp warna ke putih
            Color currentColor = Color.Lerp(originalColor, flashColor, t);
            ApplyColor(currentColor);

            yield return null;
        }

        // Fase 2: Warna putih murni memudar (alpha -> 0) & scale mengecil sedikit
        elapsed = 0f;
        Color fullWhite = flashColor;

        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / phase2Duration);

            // Fade alpha ke 0
            Color fadeColor = fullWhite;
            fadeColor.a = Mathf.Lerp(1f, 0f, t);
            ApplyColor(fadeColor);

            // Sedikit menyusut saat lenyap
            transform.localScale = Vector3.Lerp(targetBigScale, Vector3.zero, t * t);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void ApplyColor(Color col)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = col;
        }
        else if (runtimeMaterial != null)
        {
            if (runtimeMaterial.HasProperty("_BaseColor"))
            {
                runtimeMaterial.SetColor("_BaseColor", col);
            }
            else if (runtimeMaterial.HasProperty("_Color"))
            {
                runtimeMaterial.color = col;
            }
        }
    }

    private void OnDestroy()
    {
        // Bersihkan instance runtime material dari memori
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
