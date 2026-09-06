using UnityEngine;
using TMPro;

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

        if (!string.IsNullOrEmpty(NetworkUI.HostJoinCode))
            text.text = "Code: " + NetworkUI.HostJoinCode;
        else
            text.text = "";
    }
}