using UnityEngine;

public class Hand : MonoBehaviour
{
    public PlayerHand playerHand;
    public GameObject[] cardPool;

    void Update()
    {
        if (playerHand == null) playerHand = PlayerHand.Local();
    }
}