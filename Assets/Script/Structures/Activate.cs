using UnityEngine;

public class GameSceneInit : MonoBehaviour
{
    public GameObject hud;

    void Awake()
    {
        if (hud != null) hud.SetActive(true);
    }
}