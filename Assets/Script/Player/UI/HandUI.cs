using UnityEngine;

public class HandUI : MonoBehaviour
{
    public PlayerHand playerHand;

    void Update()
    {
        for (int i = 0; i < playerHand.hand.Length; i++)
            if (playerHand.hand[i] != null)
                playerHand.hand[i].gameObject.SetActive(true);
    }
}