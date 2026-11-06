using System.Collections;
using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("Floating Parameters")]
    public float floatAmplitude = 0.25f;
    public float floatFrequency = 1.2f;
    public float timeOffset = 0f;

    [Header("Rise Animation")]
    public float riseDistance = 4.0f;
    public float riseDuration = 1.0f;

    private Vector3 baseLocalPosition;
    private bool isAnimating = false;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
    }

    public void PlayRiseAndFloat(float delay = 0f)
    {
        StopAllCoroutines();
        StartCoroutine(RiseAndFloatRoutine(delay));
    }

    private IEnumerator RiseAndFloatRoutine(float delay)
    {
        isAnimating = false;

        // Mulai dari posisi bawah
        Vector3 startPos = baseLocalPosition - new Vector3(0, riseDistance, 0);
        transform.localPosition = startPos;

        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            // Smooth ease out
            float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localPosition = Vector3.LerpUnclamped(startPos, baseLocalPosition, easeOut);
            yield return null;
        }

        transform.localPosition = baseLocalPosition;
        isAnimating = true;
    }

    private void Update()
    {
        if (!isAnimating) return;

        float yOffset = Mathf.Sin((Time.time + timeOffset) * floatFrequency) * floatAmplitude;
        transform.localPosition = baseLocalPosition + new Vector3(0, yOffset, 0);
    }
}
