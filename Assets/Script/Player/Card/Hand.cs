using UnityEngine;
using Unity.Netcode;

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