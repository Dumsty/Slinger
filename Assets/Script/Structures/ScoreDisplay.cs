using UnityEngine;
using TMPro;

/// <summary>
/// Shows "A - B" score at the top of the screen during a match and while
/// the final score is briefly displayed after it ends. Important: must
/// live on a different, always-active object than label itself - if
/// attached directly to the object it hides, that would stop this
/// script's own Update() from ever running again.
/// </summary>
public class ScoreDisplay : MonoBehaviour
{
    public TMP_Text label;

    void Update()
    {
        if (label == null || MatchManager.Singleton == null) return;

        bool showing = MatchManager.Singleton.phase.Value == MatchPhase.InProgress
                    || MatchManager.Singleton.phase.Value == MatchPhase.MatchOver;

        label.gameObject.SetActive(showing);

        if (showing)
            label.text = MatchManager.Singleton.scoreA.Value + " - " + MatchManager.Singleton.scoreB.Value;
    }
}