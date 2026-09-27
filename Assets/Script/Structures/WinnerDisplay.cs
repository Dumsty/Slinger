using UnityEngine;
using TMPro;

/// <summary>
/// Shows "Player X Wins!" in the center of the screen during the
/// MatchOver pause before returning to the lobby. Important: must live on
/// a different, always-active object than label itself - if attached
/// directly to the object it hides, that would stop this script's own
/// Update() from ever running again.
/// </summary>
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
            int winner = MatchManager.Singleton.winner.Value; // 0 = none, 1 = A, 2 = B
            label.text = winner == 1 ? "Player 1 Wins!" : winner == 2 ? "Player 2 Wins!" : "";
        }
    }
}