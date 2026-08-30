using UnityEngine;
using UnityEngine.UI;

public class CardTimer : MonoBehaviour
{
    public Slider timerBar;
    public float fillTime = 5f;
    public FillHand fillHand;
    private float t;

    void Update()
    {
        bool full = System.Array.TrueForAll(fillHand.playerHand.hand, c => c != null);
        if (full)
        {
            t = 0;
            timerBar.value = 0;
            return;
        }

        t += Time.deltaTime;
        timerBar.value = t / fillTime;

        if (t >= fillTime)
        {
            t = 0;
            GiveCard();
        }
    }

    void GiveCard()
    {
        for (int i = 0; i < fillHand.playerHand.hand.Length; i++)
            if (fillHand.playerHand.hand[i] == null)
            {
                GameObject prefab = fillHand.cardPool[Random.Range(0, fillHand.cardPool.Length)];
                Card card = Instantiate(prefab).GetComponent<Card>();
                fillHand.playerHand.SetCard(i, card);
                break;
            }
    }
}