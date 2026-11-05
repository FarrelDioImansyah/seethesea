using UnityEngine;
using UnityEngine.Events;

public class InteractableItem : MonoBehaviour
{
    [Header("Konfigurasi Objek")]
    public string itemName = "Tombol Menu";
    public bool isInteractable = true;

    [Tooltip("Waktu tahan (detik). Tombol menu: 1.5s, Sampah kecil: 0.5s, Sampah besar: 2.0s")]
    public float dwellDuration = 1.5f;

    [Header("Aksi Saat Dwell Selesai")]
    public UnityEvent onDwellTriggered;

    [Header("Data Biota (Opsional untuk Fauna / Ikan)")]
    public FishData fishData;
    public AudioClip defaultDiscoverySfx;

    public void TriggerInteraction()
    {
        if (!isInteractable) return;
        Debug.Log($"[DWELL SELESAI] {itemName}");

        // Nonaktifkan interaksi agar tidak di-trigger ganda
        isInteractable = false;

        // Jalankan event tombol (jika ada)
        onDwellTriggered?.Invoke();

        // Jika objek ini adalah biota/ikan yang memiliki FishData
        if (fishData != null)
        {
            if (BiotaInfoPanel.Instance != null)
            {
                BiotaInfoPanel.Instance.ShowBiotaInfo(fishData, transform.position);
            }
            else
            {
                PlayDiscoveryAudio();
            }
        }

        // Jika objek ini adalah sampah di Scene 1
        if (SurfaceCleanupManager.Instance != null)
        {
            SurfaceCleanupManager.Instance.OnTrashCleaned(this);
        }

        // Jalankan efek animasi membesar -> putih -> hilang -> destroy
        DwellDissolveEffect dissolve = GetComponent<DwellDissolveEffect>();
        if (dissolve != null)
        {
            dissolve.Play();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void PlayDiscoveryAudio()
    {
        AudioClip clipToPlay = fishData.discoverySfx != null ? fishData.discoverySfx : defaultDiscoverySfx;
        if (clipToPlay != null)
        {
            AudioSource.PlayClipAtPoint(clipToPlay, transform.position);
        }
    }
}