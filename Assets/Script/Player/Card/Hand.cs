using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Thin container linking a player's PlayerHand and Deck, plus a synced
/// deckSize so the server (MatchManager) can verify a player has a full
/// deck before allowing them to ready up - Deck.deckList itself only
/// exists on the owning client, so it can't be read directly server-side.
/// </summary>
public class Hand : NetworkBehaviour
{
    public PlayerHand playerHand;
    public Deck deck;

    public NetworkVariable<int> deckSize = new NetworkVariable<int>(0);

    public static Hand Local()
    {
        foreach (var h in FindObjectsByType<Hand>(FindObjectsSortMode.None))
            if (h.IsOwner) return h;
        return null;
    }

    // Reports the starting deck size once on spawn - DeckBuilderUI also
    // calls ReportDeckSize whenever the player edits their deck.
    public override void OnNetworkSpawn()
    {
        if (IsOwner && deck != null) ReportDeckSize(deck.deckList.Count);
    }

    void Update()
    {
        if (playerHand == null) playerHand = PlayerHand.Local();
    }

    public void ReportDeckSize(int size)
    {
        if (IsServer)
            deckSize.Value = size;
        else
            ReportDeckSizeServerRpc(size);
    }

    [ServerRpc]
    void ReportDeckSizeServerRpc(int size)
    {
        deckSize.Value = size;
    }
}