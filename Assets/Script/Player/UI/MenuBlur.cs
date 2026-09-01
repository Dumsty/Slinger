using UnityEngine;
using UnityEngine.Rendering;

public class MenuBlur : MonoBehaviour
{
    public Volume blurVolume;
    public GameObject startMenu;
    public float fadeSpeed = 5f;
    public float minWeight = 0f;
    public float maxWeight = 1f;
    private float targetWeight;

    void Update()
    {
        targetWeight = (PauseMenu.paused || startMenu.activeInHierarchy) ? maxWeight : minWeight;
        blurVolume.weight = Mathf.MoveTowards(blurVolume.weight, targetWeight, fadeSpeed * Time.deltaTime);
    }
}