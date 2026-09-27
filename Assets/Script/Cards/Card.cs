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

    public abstract void Play();
}