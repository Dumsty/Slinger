using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SavedDeck
{
    public string name;
    public List<string> cards = new List<string>();   // prefab names, duplicates allowed
}

[Serializable]
class SavedDeckFile
{
    public List<SavedDeck> decks = new List<SavedDeck>();
}

/// <summary>
/// Reads/writes the player's saved deck builds as JSON on disk.
/// Local to this machine; nothing here touches the network.
/// </summary>
public static class DeckSaveStore
{
    static string FilePath => Path.Combine(Application.persistentDataPath, "decks.json");
    static SavedDeckFile cache;

    static SavedDeckFile Data
    {
        get
        {
            if (cache == null) cache = ReadFromDisk();
            return cache;
        }
    }

    static SavedDeckFile ReadFromDisk()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonUtility.FromJson<SavedDeckFile>(File.ReadAllText(FilePath)) ?? new SavedDeckFile();
        }
        catch (Exception e) { Debug.LogWarning("[DeckSave] Could not read saves: " + e.Message); }
        return new SavedDeckFile();
    }

    static void WriteToDisk()
    {
        try { File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true)); }
        catch (Exception e) { Debug.LogWarning("[DeckSave] Could not write saves: " + e.Message); }
    }

    public static List<string> Names()
    {
        var names = new List<string>();
        foreach (var d in Data.decks) names.Add(d.name);
        return names;
    }

    public static SavedDeck Get(string name) =>
        Data.decks.Find(d => d.name == name);

    // Saves a build under this name, overwriting an existing build with the same name.
    public static void Save(string name, List<GameObject> deckList)
    {
        var entry = Get(name);
        if (entry == null)
        {
            entry = new SavedDeck { name = name };
            Data.decks.Add(entry);
        }

        entry.cards.Clear();
        foreach (var card in deckList) entry.cards.Add(card.name);
        WriteToDisk();
    }

    public static void Delete(string name)
    {
        Data.decks.RemoveAll(d => d.name == name);
        WriteToDisk();
    }
}