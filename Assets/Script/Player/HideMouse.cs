using UnityEngine;

/// <summary>
/// Locks and hides the cursor on start. Used in scenes where the cursor
/// should be captured immediately (e.g. gameplay) rather than waiting on
/// PauseMenu/DeckStation to manage it.
/// </summary>
public class LockCursor : MonoBehaviour
{
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}