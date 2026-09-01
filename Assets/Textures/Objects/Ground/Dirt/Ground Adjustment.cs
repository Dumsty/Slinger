using UnityEngine;

public class SetTiling : MonoBehaviour
{
    public Vector2 tiling = new Vector2(1, 1);

    void Start()
    {
        GetComponent<Renderer>().material.mainTextureScale = tiling;
    }
}