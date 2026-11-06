using UnityEngine;

public class UIElementPulse : MonoBehaviour
{
    [Header("Scale Pulse")]
    public bool pulseScale = true;
    public float scaleAmplitude = 0.04f;
    public float frequency = 2.0f;

    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        if (pulseScale)
        {
            float wave = (Mathf.Sin(Time.time * frequency) + 1.0f) * 0.5f; // 0..1
            float scaleMultiplier = 1.0f + (wave * scaleAmplitude);
            transform.localScale = initialScale * scaleMultiplier;
        }
    }
}
