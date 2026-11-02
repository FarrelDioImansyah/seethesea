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

    public void TriggerInteraction()
    {
        if (!isInteractable) return;
        Debug.Log($"[DWELL SELESAI] {itemName}");

        // Jalankan event tombol (jika ada)
        onDwellTriggered?.Invoke();

        // Jika objek ini adalah sampah di Scene 1
        if (SurfaceCleanupManager.Instance != null)
        {
            SurfaceCleanupManager.Instance.OnTrashCleaned(this);
        }

        // Hilangkan objek sampah
        gameObject.SetActive(false);
    }
}