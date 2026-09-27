using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A single active buff: which effect it is (id), how long it lasts, and
/// how much time remains. Buff cards read/subtract their own bonus based
/// on IsActive(id) rather than this list managing the actual effects.
/// </summary>
public class Buff
{
    public Sprite icon;
    public float duration;
    public float timer;
    public string id;
}

/// <summary>
/// Tracks the local player's active buffs and counts them down over time.
/// Buff cards (MoveSpeedCard, etc.) call AddBuff/IsActive to drive their
/// own coroutines; this class has no knowledge of what each buff actually
/// does. Purely client-local - ClearAll() is reached from the server via
/// Health.ClearBuffs()'s ClientRpc, not called directly.
/// </summary>
public class BuffManager : MonoBehaviour
{
    public List<Buff> activeBuffs = new List<Buff>();

    public static BuffManager Local()
    {
        foreach (var bm in FindObjectsByType<BuffManager>(FindObjectsSortMode.None))
        {
            var netObj = bm.GetComponentInParent<Unity.Netcode.NetworkObject>();
            if (netObj != null && netObj.IsOwner) return bm;
        }
        return null;
    }

    public bool IsActive(string id) => activeBuffs.Exists(b => b.id == id);

    // Refreshes duration if the buff is already active, instead of
    // stacking a second copy - buff cards check IsActive before starting
    // a new coroutine, so this is the only path that resets the timer.
    public void AddBuff(string id, Sprite icon, float duration)
    {
        Buff existing = activeBuffs.Find(b => b.id == id);
        if (existing != null)
        {
            existing.timer = duration;
            return;
        }
        activeBuffs.Add(new Buff { id = id, icon = icon, duration = duration, timer = duration });
    }

    // Immediately expires every active buff. Each buff card's own
    // coroutine notices via IsActive() on its next check and reverts its
    // bonus, same as a natural expiration.
    public void ClearAll()
    {
        activeBuffs.Clear();
    }

    void Update()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].timer -= Time.deltaTime;
            if (activeBuffs[i].timer <= 0) activeBuffs.RemoveAt(i);
        }
    }
}