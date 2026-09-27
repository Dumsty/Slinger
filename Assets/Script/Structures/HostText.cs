using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current host's join code (if any) on the in-game pause
/// menu, so a client that's already hosting can share it after the fact.
/// Reads NetworkUI.HostJoinCode directly rather than needing a reference,
/// since NetworkUI lives in a different scene.
/// </summary>
public class CodeHostText : MonoBehaviour
{
    private TMP_Text text;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (text == null) return;

        // Empty for a client (no join code of their own) or before hosting.
        if (!string.IsNullOrEmpty(NetworkUI.HostJoinCode))
            text.text = "Code: " + NetworkUI.HostJoinCode;
        else
            text.text = "";
    }
}