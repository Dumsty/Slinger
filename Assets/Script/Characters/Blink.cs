using System.Collections;
using UnityEngine;

/// <summary>
/// Plays both eye blink clips together at random intervals, with an
/// occasional double blink so it doesn't read as mechanical.
/// Purely cosmetic and client-local - no networking needed.
/// </summary>
public class BlinkController : MonoBehaviour
{
    [Tooltip("Animators to trigger together - one per eye, or a single shared one.")]
    public Animator[] eyeAnimators;

    [Tooltip("Name of the blink STATE in the Animator Controller (not the clip).")]
    public string blinkState = "Blink";

    [Header("Timing")]
    [Tooltip("Shortest wait between blinks, in seconds.")]
    public float minInterval = 2.5f;

    [Tooltip("Longest wait between blinks, in seconds.")]
    public float maxInterval = 6f;

    [Header("Double blink")]
    [Tooltip("Chance that a blink is immediately followed by a second one.")]
    [Range(0f, 1f)] public float doubleBlinkChance = 0.2f;

    [Tooltip("Gap between the two blinks of a double blink.")]
    public float doubleBlinkGap = 0.18f;

    void OnEnable()
    {
        StartCoroutine(BlinkLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator BlinkLoop()
    {
        // Random head start, so several NPCs in a scene don't blink in unison.
        yield return new WaitForSeconds(Random.Range(0f, maxInterval));

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            Blink();

            if (Random.value < doubleBlinkChance)
            {
                yield return new WaitForSeconds(doubleBlinkGap);
                Blink();
            }
        }
    }

    /// <summary>Fires both eyes on the same frame. Also callable from elsewhere.</summary>
    public void Blink()
    {
        if (eyeAnimators == null) return;
        Debug.Log("Blink fired");
        foreach (var anim in eyeAnimators)
        {
            if (anim == null) continue;

            // Layer 0, normalizedTime 0 restarts the state from the top, so
            // a new blink can interrupt one already playing.
            anim.Play(blinkState, 0, 0f);
        }
    }
}