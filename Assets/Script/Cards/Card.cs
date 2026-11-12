using UnityEngine;

/// <summary>
/// Base class for all playable cards. Subclasses implement Play() to
/// define what happens when the card is used from the player's hand.
/// </summary>
public abstract class Card : MonoBehaviour
{
    [Tooltip("Icon shown in the player's hand and deck builder UI.")]
    public Sprite icon;

    // Reference to the prefab this card instance was created from, so it
    // can be discarded back into the deck's pool when removed from hand.
    [HideInInspector] public GameObject prefabRef;

    [Header("Display")]
    public string cardName;
    [TextArea(2, 5)] public string description;

    [Header("Mana")]
    [Tooltip("Mana spent to play this card. The card can't be played without at least this much mana. 0 = free.")]
    [Min(0)] public int manaCost = 1;

    // Falls back to the object name if cardName is left blank
    public string DisplayName => string.IsNullOrEmpty(cardName) ? gameObject.name : cardName;

    public abstract void Play();
}