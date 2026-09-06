using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ReadyToggle : MonoBehaviour
{
    public TMP_Text label;
    private bool isReady;

    void Update()
    {
        if (label == null || MatchManager.Singleton == null) return;

        MatchPhase phase = MatchManager.Singleton.phase.Value;
        bool showing = phase == MatchPhase.Lobby || phase == MatchPhase.Countdown;

        label.gameObject.SetActive(showing);

        if (!showing)
        {
            isReady = false;
            return;
        }

        label.text = isReady ? "Ready" : "Unready";

        if (PauseMenu.paused || DeckStation.editingDeck) return;

        if (Input.GetKeyDown(KeyCode.R))
            Toggle();
    }

    public void Toggle()
    {
        if (MatchManager.Singleton == null) return;
        if (NetworkManager.Singleton == null) return;

        isReady = !isReady;
        MatchManager.Singleton.SetReady(NetworkManager.Singleton.LocalClientId, isReady);
    }
}