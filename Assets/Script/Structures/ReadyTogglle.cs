using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ReadyToggle : MonoBehaviour
{
    public TMP_Text label;
    public TMP_Text messageText;
    private float messageTimer;

    private static ReadyToggle activeInstance;

    void OnEnable() { activeInstance = this; }

    void Update()
    {
        if (messageText != null && messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0) messageText.text = "";
        }

        if (label == null || MatchManager.Singleton == null || NetworkManager.Singleton == null) return;

        MatchManager mm = MatchManager.Singleton;
        bool showing = mm.phase.Value == MatchPhase.Lobby
                    || mm.phase.Value == MatchPhase.Countdown
                    || (mm.phase.Value == MatchPhase.Solo && !mm.soloPracticing.Value);

        label.gameObject.SetActive(showing);
        if (!showing) return;

        bool isReady = mm.IsClientReady(NetworkManager.Singleton.LocalClientId);
        label.text = isReady ? "Ready" : "Unready";

        if (PauseMenu.paused || DeckStation.editingDeck) return;

        if (Input.GetKeyDown(KeyCode.R))
            Toggle(!isReady);
    }

    public void Toggle(bool ready)
    {
        if (MatchManager.Singleton == null || NetworkManager.Singleton == null) return;
        MatchManager.Singleton.SetReady(NetworkManager.Singleton.LocalClientId, ready);
    }

    public static void ShowRejectionMessage(string message)
    {
        if (activeInstance == null || activeInstance.messageText == null) return;
        activeInstance.messageText.text = message;
        activeInstance.messageTimer = 2.5f;
    }
}