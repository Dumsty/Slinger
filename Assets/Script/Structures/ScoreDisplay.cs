using UnityEngine;
using TMPro;

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