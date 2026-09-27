using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// A single card type and how many copies the player owns.
/// </summary>
[System.Serializable]
public class InventoryEntry
{
    public GameObject cardPrefab;
    public int quantity;
}

/// <summary>
/// Holds the local player's owned cards - source of truth for what the
/// deck builder shows and what can be added to a deck.
/// </summary>
public class CardInventory : NetworkBehaviour
{
    public List<InventoryEntry> entries = new();

    public static CardInventory Local()
    {
        foreach (var inv in FindObjectsByType<CardInventory>(FindObjectsSortMode.None))
            if (inv.IsOwner) return inv;
        return null;
    }

    public int GetQuantity(GameObject prefab)
    {
        var entry = entries.Find(e => e.cardPrefab == prefab);
        return entry != null ? entry.quantity : 0;
    }
}