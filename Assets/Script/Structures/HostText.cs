using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current host's join code (if any) on the in-game pause
/// menu, so a client that's already hosting can share it after the fact.
/// Clicking the code copies it to the clipboard.
/// Reads NetworkUI.HostJoinCode directly rather than needing a reference,
/// since NetworkUI lives in a different scene.
/// </summary>
public class CodeHostText : MonoBehaviour
{
    [Tooltip("How long the \"copied!\" confirmation shows after clicking, in seconds.")]
    public float copiedMessageDuration = 1.5f;

    private TMP_Text text;
    private float copiedTimer;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (text == null) return;

        string code = NetworkUI.HostJoinCode;

        // Empty for a client (no join code of their own) or before hosting.
        if (string.IsNullOrEmpty(code))
        {
            text.text = "";
            return;
        }

        if (Input.GetMouseButtonDown(0) && IsPointerOverText())
        {
            GUIUtility.systemCopyBuffer = code;
            copiedTimer = copiedMessageDuration;
        }

        // Unscaled so the confirmation still clears if time is ever paused.
        if (copiedTimer > 0f) copiedTimer -= Time.unscaledDeltaTime;

        string hint = copiedTimer > 0f ? "copied!" : "click to copy";
        text.text = "Code: " + code + " <size=50%>(" + hint + ")</size>";
    }

    // Hit-tests the rendered text itself rather than using a UI click
    // handler: the pause menu's "Code Host" RectTransform has a negative
    // height, so Unity's UI raycaster never registers clicks on it.
    bool IsPointerOverText()
    {
        Canvas canvas = text.canvas;
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(text.rectTransform, Input.mousePosition, cam, out Vector2 local))
            return false;

        Bounds b = text.textBounds;
        return local.x >= b.min.x && local.x <= b.max.x && local.y >= b.min.y && local.y <= b.max.y;
    }
}
