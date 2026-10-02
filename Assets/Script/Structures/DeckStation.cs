using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Trigger zone for the deck builder: opens the shop UI on entry, closes
/// it and wipes the player's hand on exit if the deck was changed while
/// inside - forcing a fresh hand drawn from the newly edited deck.
/// </summary>
public class DeckStation : MonoBehaviour
{
    public GameObject deckBuilderUI;
    public GameObject hud;
    public static bool editingDeck;

    void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        deckBuilderUI.SetActive(true);
        if (hud != null) hud.SetActive(false);
        editingDeck = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        deckBuilderUI.SetActive(false);
        if (hud != null) hud.SetActive(true);
        editingDeck = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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