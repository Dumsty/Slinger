using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Card card;   // assigned by DeckBuilderUI when the row is created

    public void OnPointerEnter(PointerEventData e)
    {
        if (CardTooltip.Instance != null) CardTooltip.Instance.Show(card);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (CardTooltip.Instance != null) CardTooltip.Instance.Hide();
    }

    // Rows get destroyed or the builder closes while hovered: no exit event fires
    void OnDisable()
    {
        if (CardTooltip.Instance != null) CardTooltip.Instance.Hide();
    }
}