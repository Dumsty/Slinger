using UnityEngine;
using TMPro;

/// <summary>
/// Shows the pre-match countdown number once both players are ready.
/// Important: must live on a different, always-active object than label
/// itself - if attached directly to the object it hides, that would stop
/// this script's own Update() from ever running again.
/// </summary>
public class CountdownDisplay : MonoBehaviour
{
    public TMP_Text label;

    void Update()
    {
        if (label == null || MatchManager.Singleton == null) return;

        bool showing = MatchManager.Singleton.phase.Value == MatchPhase.Countdown;
        label.gameObject.SetActive(showing);

        if (showing)
            label.text = Mathf.CeilToInt(MatchManager.Singleton.countdownRemaining.Value).ToString();
    }
}