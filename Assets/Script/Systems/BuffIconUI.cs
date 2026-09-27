using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays one icon per active buff, each filled bottom-to-top to show
/// remaining duration (drains as the buff runs out). Icons are created
/// and destroyed on demand to match the current buff count.
/// </summary>
public class BuffIconUI : MonoBehaviour
{
    public GameObject iconPrefab;
    public Transform container;

    private BuffManager buffManager;

    void Update()
    {
        if (buffManager == null)
        {
            buffManager = BuffManager.Local();
            return;
        }

        int buffCount = buffManager.activeBuffs.Count;

        // Grow the icon list if there are more active buffs than icons.
        while (container.childCount < buffCount)
            Instantiate(iconPrefab, container);

        for (int i = 0; i < buffCount; i++)
        {
            Image icon = container.GetChild(i).GetComponent<Image>();
            Buff buff = buffManager.activeBuffs[i];

            icon.sprite = buff.icon;
            icon.type = Image.Type.Filled;
            icon.fillMethod = Image.FillMethod.Vertical;
            icon.fillOrigin = (int)Image.OriginVertical.Bottom;
            icon.fillAmount = buff.duration > 0
                ? Mathf.Clamp01(buff.timer / buff.duration)
                : 0;
        }

        // Shrink back down if buffs expired since the last frame.
        if (container.childCount > buffCount)
        {
            for (int i = container.childCount - 1; i >= buffCount; i--)
                Destroy(container.GetChild(i).gameObject);
        }
    }
}