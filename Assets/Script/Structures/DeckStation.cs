using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Trigger zone for the deck builder: opens the shop UI on entry and closes
/// it on exit. UI activation is deferred to Update() because disabling a
/// TMP_InputField inside a physics callback (OnTriggerExit) throws.
/// </summary>
public class DeckStation : MonoBehaviour
{
    public GameObject deckBuilderUI;
    public GameObject hud;
    public static bool editingDeck;

    // What the player's position says the shop should be (applied in Update)
    private bool wantOpen;

    void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        wantOpen = true;
        editingDeck = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        wantOpen = false;
        editingDeck = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Applies the open/closed state outside the physics callback.
    void Update()
    {
        if (deckBuilderUI != null && deckBuilderUI.activeSelf != wantOpen)
            deckBuilderUI.SetActive(wantOpen);

        if (hud != null && hud.activeSelf == wantOpen)
            hud.SetActive(!wantOpen);
    }

    // Safety: if the station is disabled/destroyed while the player is inside
    // (scene change, match reset), don't leave the static flag stuck on.
    void OnDisable()
    {
        if (wantOpen) editingDeck = false;
        wantOpen = false;
    }

    private bool IsLocalPlayer(Collider other)
    {
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return false;
        return other.GetComponent<PlayerHand>() != null || other.CompareTag("Player");
    }

    void StripHand(Hand h)
    {
        if (h.playerHand == null) return;
        for (int i = 0; i < h.playerHand.hand.Length; i++)
            if (h.playerHand.hand[i] != null)
                h.playerHand.RemoveCard(i, h.deck);
    }
}