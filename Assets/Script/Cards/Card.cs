using UnityEngine;

public abstract class Card : MonoBehaviour
{
    public Sprite icon;
    [HideInInspector] public GameObject prefabRef;
    public abstract void Play();
}