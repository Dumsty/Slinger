using UnityEngine;

public class FillHand : MonoBehaviour
{
    public PlayerHand playerHand;
    public GameObject[] cardPool;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            for (int i = 0; i < playerHand.hand.Length; i++)
                if (playerHand.hand[i] == null)
                {
                    GameObject prefab = cardPool[Random.Range(0, cardPool.Length)];
                    Card card = Instantiate(prefab).GetComponent<Card>();
                    playerHand.SetCard(i, card);
                }
    }
}