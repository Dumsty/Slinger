using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Lets gameplay scripts check whether the player is typing in a text field.
/// </summary>
public static class UITyping
{
    public static bool IsTyping
    {
        get
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null) return false;

            var field = es.currentSelectedGameObject.GetComponent<TMP_InputField>();
            return field != null && field.isFocused;
        }
    }
}