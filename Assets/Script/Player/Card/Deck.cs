using UnityEngine;
using System.Collections.Generic;

public class Deck : MonoBehaviour
{
    public int maxDeckSize = 15;
    public List<GameObject> deckList = new List<GameObject>();
    private List<GameObject> drawPile = new List<GameObject>();
    private List<GameObject> discardPile = new List<GameObject>();

    void Start()
    {
        drawPile = new List<GameObject>(deckList);
        Shuffle();
    }

    void Shuffle()
    {
        for (int i = drawPile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (drawPile[i], drawPile[j]) = (drawPile[j], drawPile[i]);
        }
    }

    public GameObject DrawCard()
    {
        if (drawPile.Count == 0)
        {
            if (discardPile.Count == 0) return null;
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle();
        }
        GameObject card = drawPile[0];
        drawPile.RemoveAt(0);
        return card;
    }

    public void Discard(GameObject prefab)
    {
        discardPile.Add(prefab);
    }

    public bool IsEmpty()
    {
        return drawPile.Count == 0 && discardPile.Count == 0;
    }

    public bool CanAddCard()
    {
        return deckList.Count < maxDeckSize;
    }

    public void Rebuild(List<GameObject> inPlay)
    {
        Dictionary<GameObject, int> desired = Count(deckList);
        Dictionary<GameObject, int> owned = Count(drawPile);
        AddCounts(owned, discardPile);
        AddCounts(owned, inPlay);

        foreach (var kv in desired)
        {
            int have = owned.ContainsKey(kv.Key) ? owned[kv.Key] : 0;
            for (int i = have; i < kv.Value; i++) drawPile.Add(kv.Key);
        }

        foreach (var kv in owned)
        {
            int want = desired.ContainsKey(kv.Key) ? desired[kv.Key] : 0;
            int excess = kv.Value - want;
            while (excess > 0 && drawPile.Remove(kv.Key)) excess--;
            while (excess > 0 && discardPile.Remove(kv.Key)) excess--;
        }

        Shuffle();
    }

    Dictionary<GameObject, int> Count(List<GameObject> list)
    {
        var d = new Dictionary<GameObject, int>();
        foreach (var item in list)
            d[item] = d.ContainsKey(item) ? d[item] + 1 : 1;
        return d;
    }

    void AddCounts(Dictionary<GameObject, int> target, List<GameObject> list)
    {
        foreach (var item in list)
            target[item] = target.ContainsKey(item) ? target[item] + 1 : 1;
    }

    public void RebuildFresh()
    {
        drawPile = new List<GameObject>(deckList);
        discardPile.Clear();
        Shuffle();
    }
}