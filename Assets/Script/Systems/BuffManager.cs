using UnityEngine;
using System.Collections.Generic;

public class Buff
{
    public Sprite icon;
    public float duration;
    public float timer;
    public string id;
}

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