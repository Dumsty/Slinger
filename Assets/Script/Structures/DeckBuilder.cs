using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Deck builder screen: lists owned cards from CardInventory, lets the
/// player add/remove copies to their Deck up to maxDeckSize, and reports
/// the resulting deck size to the server via Hand for the ready-up check.
/// </summary>
public class DeckBuilderUI : MonoBehaviour
{
    public GameObject rowPrefab;
    public Transform contentParent;
    public TMP_Text deckSizeText;

    private CardInventory inventory;
    private Deck deck;
    private Hand hand;

    // Set whenever the deck is edited - DeckStation checks this on exit
    // to decide whether to wipe the player's current hand.
    public static bool deckChanged;

    void OnEnable()
    {
        inventory = CardInventory.Local();
        hand = Hand.Local();
        deck = hand != null ? hand.deck : null;
        deckChanged = false;

        ReconcileDeckWithInventory();
        Refresh();
    }

    // Trims the deck to match what's actually owned (in case inventory
    // changed since the deck was last built) and to respect maxDeckSize.
    void ReconcileDeckWithInventory()
    {
        if (inventory == null || deck == null) return;

        var owned = new Dictionary<GameObject, int>();
        foreach (var entry in inventory.entries)
            owned[entry.cardPrefab] = entry.quantity;

        var kept = new List<GameObject>(deck.deckList.Count);
        var used = new Dictionary<GameObject, int>();

        foreach (var card in deck.deckList)
        {
            int limit = owned.TryGetValue(card, out int q) ? q : 0;
            used.TryGetValue(card, out int soFar);

            if (soFar < limit && kept.Count < deck.maxDeckSize)
            {
                kept.Add(card);
                used[card] = soFar + 1;
            }
        }

        if (kept.Count != deck.deckList.Count)
        {
            deck.deckList.Clear();
            deck.deckList.AddRange(kept);
            deckChanged = true;
        }
    }

    void Refresh()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        if (inventory == null || deck == null) return;

        UpdateDeckSizeText();
        if (hand != null) hand.ReportDeckSize(deck.deckList.Count);

        foreach (var entry in inventory.entries)
        {
            GameObject row = Instantiate(rowPrefab, contentParent);
            int inDeck = deck.deckList.FindAll(c => c == entry.cardPrefab).Count;

            row.transform.Find("Name").GetComponent<TMP_Text>().text = entry.cardPrefab.name;
            row.transform.Find("Count").GetComponent<TMP_Text>().text = inDeck + " / " + entry.quantity;

            Button addBtn = row.transform.Find("AddButton").GetComponent<Button>();
            Button removeBtn = row.transform.Find("RemoveButton").GetComponent<Button>();

            addBtn.onClick.AddListener(() =>
            {
                if (inDeck < entry.quantity && deck.CanAddCard())
                {
                    deck.deckList.Add(entry.cardPrefab);
                    deckChanged = true;
                    Refresh();
                }
            });

            removeBtn.onClick.AddListener(() =>
            {
                if (inDeck > 0) { deck.deckList.Remove(entry.cardPrefab); deckChanged = true; Refresh(); }
            });
        }
    }

    void UpdateDeckSizeText()
    {
        if (deckSizeText == null) return;
        deckSizeText.text = "Card Capacity: " + deck.deckList.Count + "/" + deck.maxDeckSize;
    }
}