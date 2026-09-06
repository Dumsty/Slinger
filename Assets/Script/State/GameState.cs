using UnityEngine;

public static class GameState
{
    public static bool InputBlocked =>
        PauseMenu.paused || DeckStation.editingDeck;
}