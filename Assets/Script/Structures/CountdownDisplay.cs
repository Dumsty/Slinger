using UnityEngine;
using TMPro;

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