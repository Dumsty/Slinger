using UnityEngine;
using TMPro;

public class WinnerDisplay : MonoBehaviour
{
    public TMP_Text label;

    void Update()
    {
        if (label == null || MatchManager.Singleton == null) return;

        bool showing = MatchManager.Singleton.phase.Value == MatchPhase.MatchOver;
        label.gameObject.SetActive(showing);

        if (showing)
        {
            int winner = MatchManager.Singleton.winner.Value;
            label.text = winner == 1 ? "Player 1 Wins!" : winner == 2 ? "Player 2 Wins!" : "";
        }
    }
}