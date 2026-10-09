using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CardTooltip : MonoBehaviour
{
    public static CardTooltip Instance { get; private set; }

    [SerializeField] RectTransform panel;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] Vector2 offset = new Vector2(16f, -16f);

    void Awake()
    {
        Instance = this;
        if (panel) panel.gameObject.SetActive(false);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Show(Card card)
    {
        if (card == null || panel == null) return;
        nameText.text = card.DisplayName;
        descriptionText.text = card.description;
        panel.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        Follow();
    }

    public void Hide()
    {
        if (panel) panel.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (panel != null && panel.gameObject.activeSelf) Follow();
    }

    void Follow()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 mouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        Vector2 mouse = Input.mousePosition;
#endif
        // Flip the pivot near screen edges so the tooltip stays on screen
        float px = mouse.x > Screen.width * 0.6f ? 1f : 0f;
        float py = mouse.y < Screen.height * 0.4f ? 0f : 1f;
        panel.pivot = new Vector2(px, py);

        Vector2 o = new Vector2(px == 1f ? -offset.x : offset.x, py == 0f ? -offset.y : offset.y);
        panel.position = mouse + o;
    }
}