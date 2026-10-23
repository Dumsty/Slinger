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

    [Header("Save / Load")]
    public TMP_InputField buildNameInput;
    public Button saveButton;
    public TMP_Dropdown buildsDropdown;
    public Button deleteButton;
    public TMP_Text statusText;          // optional, shows "Saved" / "Loaded (2 cards skipped)"
    const string Placeholder = "Select build...";

    private List<string> buildNames = new List<string>();

    void OnEnable()
    {
        inventory = CardInventory.Local();
        hand = Hand.Local();
        deck = hand != null ? hand.deck : null;
        deckChanged = false;

        ReconcileDeckWithInventory();
        Refresh();

        if (buildNameInput != null) buildNameInput.characterLimit = 24;

        HookButton(saveButton, SaveBuild);
        HookButton(deleteButton, DeleteBuild);
        RebuildBuildsDropdown(null);
        SetStatus("");
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

            var card = entry.cardPrefab.GetComponent<Card>();
            row.transform.Find("Name").GetComponent<TMP_Text>().text = card != null ? card.DisplayName : entry.cardPrefab.name;
            row.transform.Find("Count").GetComponent<TMP_Text>().text = inDeck + " / " + entry.quantity;

            var hover = row.GetComponent<CardHoverTooltip>();
            if (hover != null) hover.card = card;

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

    void HookButton(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b == null) return;
        b.onClick.RemoveListener(action);
        b.onClick.AddListener(action);
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    void RebuildBuildsDropdown(string select)
    {
        if (buildsDropdown == null) return;

        buildNames = DeckSaveStore.Names();

        // Entry 0 is the placeholder, real builds start at index 1
        var options = new List<string> { Placeholder };
        options.AddRange(buildNames);

        buildsDropdown.onValueChanged.RemoveListener(OnBuildPicked);
        buildsDropdown.ClearOptions();
        buildsDropdown.AddOptions(options);

        int index = select != null ? buildNames.IndexOf(select) + 1 : 0;
        buildsDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
        buildsDropdown.RefreshShownValue();
        buildsDropdown.onValueChanged.AddListener(OnBuildPicked);

        if (deleteButton != null) deleteButton.interactable = buildNames.Count > 0;
    }

    string SelectedBuildName()
    {
        if (buildsDropdown == null) return null;
        int i = buildsDropdown.value - 1;   // minus the placeholder
        return (i >= 0 && i < buildNames.Count) ? buildNames[i] : null;
    }

    // Fires when the player picks a build from the dropdown
    void OnBuildPicked(int index)
    {
        if (index <= 0) return;   // placeholder
        LoadBuild();
    }

    void SaveBuild()
    {
        if (deck == null) return;

        string n = buildNameInput != null ? buildNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(n)) n = "Build " + (buildNames.Count + 1);

        bool overwrote = buildNames.Contains(n);
        DeckSaveStore.Save(n, deck.deckList);
        RebuildBuildsDropdown(n);
        SetStatus(overwrote ? "Overwrote \"" + n + "\"" : "Saved \"" + n + "\"");
    }

    void LoadBuild()
    {
        string n = SelectedBuildName();
        if (n == null || inventory == null || deck == null) return;

        var saved = DeckSaveStore.Get(n);
        if (saved == null) return;

        // Look cards up from what the player actually owns
        var byName = new Dictionary<string, GameObject>();
        var owned = new Dictionary<GameObject, int>();
        foreach (var entry in inventory.entries)
        {
            byName[entry.cardPrefab.name] = entry.cardPrefab;
            owned[entry.cardPrefab] = entry.quantity;
        }

        deck.deckList.Clear();
        var used = new Dictionary<GameObject, int>();
        int skipped = 0;

        foreach (string id in saved.cards)
        {
            if (byName.TryGetValue(id, out GameObject prefab))
            {
                used.TryGetValue(prefab, out int soFar);
                if (soFar < owned[prefab] && deck.deckList.Count < deck.maxDeckSize)
                {
                    deck.deckList.Add(prefab);
                    used[prefab] = soFar + 1;
                    continue;
                }
            }
            skipped++;   // no longer owned, over quantity, or over deck size
        }

        deckChanged = true;
        Refresh();   // rebuilds rows and reports the new deck size
        SetStatus(skipped > 0 ? "Loaded \"" + n + "\" (" + skipped + " cards skipped)" : "Loaded \"" + n + "\"");
    }

    void DeleteBuild()
    {
        string n = SelectedBuildName();
        if (n == null) return;

        DeckSaveStore.Delete(n);
        RebuildBuildsDropdown(null);
        SetStatus("Deleted \"" + n + "\"");
    }
}