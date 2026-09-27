using UnityEngine;

/// <summary>
/// Central place to check whether gameplay input should currently be
/// blocked (e.g. while paused or editing the deck). Scripts that gate
/// input on both PauseMenu.paused and DeckStation.editingDeck can use
/// this instead of duplicating both checks everywhere.
/// </summary>
public static class GameState
{
    public static bool InputBlocked =>
        PauseMenu.paused || DeckStation.editingDeck;
}