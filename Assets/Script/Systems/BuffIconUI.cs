using UnityEngine;
using UnityEngine.UI;

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

        if (container.childCount > buffCount)
        {
            for (int i = container.childCount - 1; i >= buffCount; i--)
                Destroy(container.GetChild(i).gameObject);
        }
    }
}