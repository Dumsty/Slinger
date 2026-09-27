using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Holds the local player's hand of card GameObjects and lays them out in
/// a fanned arc. Card data itself (hand[]) lives only on the owning
/// client - it is not synced, so server-side code must go through
/// ClearHand()/ClientRpc rather than touching hand[] directly.
/// </summary>
public class PlayerHand : NetworkBehaviour
{
    public Card[] hand = new Card[7];
    public Transform handCenter;

    [Header("Fan Layout")]
    public float cardSpacing = 0.15f;
    public float depthOffset = 0.02f;
    public float fanAngle = 10f;
    public AnimationCurve fanCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public bool reverseFan = false;
    public float archHeight = 0.05f;
    public AnimationCurve archCurve = AnimationCurve.Linear(0, 1, 1, 0);

    public static PlayerHand Local()
    {
        foreach (var ph in FindObjectsByType<PlayerHand>(FindObjectsSortMode.None))
            if (ph.IsOwner) return ph;
        return null;
    }

    public void SetCard(int slot, Card card)
    {
        hand[slot] = card;
        Center();
    }

    // deck is optional so a card can be silently destroyed instead of
    // discarded back into the pool when that's not appropriate (rare) -
    // pass it whenever possible so the card isn't lost from the deck.
    public void RemoveCard(int slot, Deck deck = null)
    {
        if (deck != null && hand[slot].prefabRef != null) deck.Discard(hand[slot].prefabRef);
        Destroy(hand[slot].gameObject);
        hand[slot] = null;
        Center();
    }

    // hand[] only exists on the owning client, so the server can't clear
    // it directly - this routes the request through a ClientRpc to
    // whichever client actually owns this hand.
    public void ClearHand()
    {
        if (IsServer)
            ClearHandClientRpc();
    }

    [ClientRpc]
    void ClearHandClientRpc()
    {
        if (!IsOwner) return;

        Hand h = GetComponent<Hand>();
        Deck deck = h != null ? h.deck : null;

        for (int i = 0; i < hand.Length; i++)
            if (hand[i] != null)
                RemoveCard(i, deck);
    }

    // Repositions all occupied cards into a centered, fanned arc under
    // handCenter based on the configured spacing/angle/arch curves.
    void Center()
    {
        Card[] cards = System.Array.FindAll(hand, c => c != null);
        float startX = -(cards.Length - 1) * cardSpacing / 2f;
        for (int i = 0; i < cards.Length; i++)
        {
            float offsetFromCenter = startX + i * cardSpacing;
            float t = Mathf.Clamp01(Mathf.Abs(offsetFromCenter) / (cardSpacing * (cards.Length / 2f) + 0.001f));
            float angle = -Mathf.Sign(offsetFromCenter) * fanAngle * fanCurve.Evaluate(t);
            float z = (reverseFan ? (cards.Length - 1 - i) : i) * depthOffset;
            float y = archHeight * archCurve.Evaluate(t);

            cards[i].transform.SetParent(handCenter);
            cards[i].transform.localPosition = new Vector3(offsetFromCenter, y, z);
            cards[i].transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // Re-runs the layout in the editor when curve/spacing values are
    // tweaked in the Inspector during Play mode.
    void OnValidate()
    {
        if (Application.isPlaying) Center();
    }
}