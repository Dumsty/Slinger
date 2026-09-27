using UnityEngine;

/// <summary>
/// Gently pulses an object's scale up and down over time - used on the
/// main menu title text for a subtle "breathing" effect.
/// </summary>
public class TitlePulse : MonoBehaviour
{
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.05f;
    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * scale;
    }
}