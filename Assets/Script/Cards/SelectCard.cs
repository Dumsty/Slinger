using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles selecting and playing cards from the local player's hand:
/// number-key and scroll-wheel selection, visual fan layout, and playing
/// the active card on left click.
/// </summary>
public class SelectCard : NetworkBehaviour
{
    private const int HandSize = 7;

    public PlayerHand playerHand;
    private Deck deck;
    private PlayerInvisibility invisibility;

    // Index into playerHand.hand of the currently selected card, or -1 if none.
    public int activeSlot = -1;

    void Update()
    {
        if (!IsOwner) return;

        // Lazily resolve dependencies - these may not exist yet on the
        // frame this object spawns.
        if (playerHand == null) { playerHand = PlayerHand.Local(); return; }
        if (deck == null)
        {
            Hand h = Hand.Local();
            if (h != null) deck = h.deck;
            return;
        }
        if (invisibility == null) invisibility = GetComponent<PlayerInvisibility>();

        if (PauseMenu.paused) return;
        if (DeckStation.editingDeck) return;

        bool hasAnyCard = HasAnyCard();

        if (activeSlot == -1 && hasAnyCard)
            activeSlot = FindFirstOccupiedSlot();
        else if (!hasAnyCard)
            activeSlot = -1;

        HandleNumberKeySelection();

        if (hasAnyCard)
            HandleScrollSelection();

        LayoutHand();
        HandlePlayInput();
    }

    private bool HasAnyCard()
    {
        for (int i = 0; i < HandSize; i++)
            if (playerHand.hand[i] != null) return true;
        return false;
    }

    private int FindFirstOccupiedSlot()
    {
        for (int i = 0; i < HandSize; i++)
            if (playerHand.hand[i] != null) return i;
        return -1;
    }

    // Number keys select by visual position among occupied slots (e.g. "2"
    // selects the second card currently in hand), not by raw slot index.
    private void HandleNumberKeySelection()
    {
        for (int i = 0; i < HandSize; i++)
        {
            if (!Input.GetKeyDown(KeyCode.Alpha1 + i)) continue;

            int count = -1;
            for (int s = 0; s < HandSize; s++)
            {
                if (playerHand.hand[s] == null) continue;
                if (++count == i)
                {
                    activeSlot = s;
                    break;
                }
            }
        }
    }

    // Scrolls to the next occupied slot in either direction, wrapping
    // around, and bounded to at most HandSize steps so it can never loop
    // forever even if activeSlot starts at -1 or the hand is empty.
    private void HandleScrollSelection()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0) return;

        int dir = scroll > 0 ? 1 : -1;
        int next = activeSlot;

        for (int step = 0; step < HandSize; step++)
        {
            next = (next + dir + HandSize) % HandSize;
            if (playerHand.hand[next] != null)
            {
                activeSlot = next;
                return;
            }
        }
    }

    // Arranges occupied cards in a fanned arc, raising and pulling the
    // active card slightly forward/up so it's visually distinct.
    private void LayoutHand()
    {
        Card[] active = System.Array.FindAll(playerHand.hand, c => c != null);
        if (active.Length == 0) return;

        float startX = -(active.Length - 1) * playerHand.cardSpacing / 2f;
        for (int i = 0; i < active.Length; i++)
        {
            float offsetFromCenter = startX + i * playerHand.cardSpacing;
            float t = Mathf.Clamp01(Mathf.Abs(offsetFromCenter) / (playerHand.cardSpacing * (active.Length / 2f) + 0.001f));
            float archY = playerHand.archHeight * playerHand.archCurve.Evaluate(t);
            float z = (playerHand.reverseFan ? (active.Length - 1 - i) : i) * playerHand.depthOffset;
            bool isActive = activeSlot != -1 && playerHand.hand[activeSlot] == active[i];

            active[i].transform.localPosition = new Vector3(
                offsetFromCenter,
                archY + (isActive ? 0.2f : 0f),
                isActive ? -0.1f : z);
        }
    }

    // Plays the active card on left click, then re-selects a neighboring
    // occupied slot (preferring left) so selection doesn't jump to -1
    // unnecessarily. Blocked entirely while invisible.
    private void HandlePlayInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (activeSlot == -1 || playerHand.hand[activeSlot] == null) return;
        if (invisibility != null && invisibility.IsInvisible) return;

        int played = activeSlot;
        playerHand.hand[activeSlot].Play();
        playerHand.RemoveCard(activeSlot, deck);

        int left = played - 1;
        while (left >= 0 && playerHand.hand[left] == null) left--;

        int right = played + 1;
        while (right < HandSize && playerHand.hand[right] == null) right++;

        activeSlot = left >= 0 ? left : (right < HandSize ? right : -1);
    }
}