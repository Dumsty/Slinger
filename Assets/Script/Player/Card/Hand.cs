using UnityEngine;
using Unity.Netcode;

public class Hand : NetworkBehaviour
{
    public PlayerHand playerHand;
    public Deck deck;

    public static Hand Local()
    {
        foreach (var h in FindObjectsByType<Hand>(FindObjectsSortMode.None))
            if (h.IsOwner) return h;
        return null;
    }

    void Update()
    {
        if (playerHand == null) playerHand = PlayerHand.Local();
    }
}