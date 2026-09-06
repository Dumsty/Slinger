using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckBuilderUI : MonoBehaviour
{
    public GameObject rowPrefab;
    public Transform contentParent;
    public TMP_Text deckSizeText;
    private CardInventory inventory;
    private Deck deck;
    public static bool deckChanged;

    void OnEnable()
    {
        inventory = CardInventory.Local();
        Hand h = Hand.Local();
        deck = h != null ? h.deck : null;
        deckChanged = false;

        ReconcileDeckWithInventory();
        Refresh();
    }

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