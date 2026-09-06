using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

[System.Serializable]
public class InventoryEntry
{
    public GameObject cardPrefab;
    public int quantity;
}

public class CardInventory : NetworkBehaviour
{
    public List<InventoryEntry> entries = new();

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[CardInventory] Spawned on {gameObject.name}, " +
                  $"NetworkObjectId={NetworkObjectId}, OwnerClientId={OwnerClientId}, " +
                  $"IsOwner={IsOwner}, IsServer={IsServer}, entries.Count={entries.Count}");
    }

    public static CardInventory Local()
    {
        CardInventory result = null;
        int candidateCount = 0;

        foreach (var inv in FindObjectsByType<CardInventory>(FindObjectsSortMode.None))
        {
            candidateCount++;
            Debug.Log($"[CardInventory.Local] Candidate {inv.gameObject.name}: " +
                      $"OwnerClientId={inv.OwnerClientId}, IsOwner={inv.IsOwner}, entries.Count={inv.entries.Count}");

            if (inv.IsOwner && result == null)
                result = inv;
        }

        Debug.Log($"[CardInventory.Local] Found {candidateCount} CardInventory instance(s) in scene. " +
                  $"Selected: {(result != null ? result.gameObject.name : "NULL")}");

        return result;
    }

    public int GetQuantity(GameObject prefab)
    {
        var entry = entries.Find(e => e.cardPrefab == prefab);
        return entry != null ? entry.quantity : 0;
    }
}