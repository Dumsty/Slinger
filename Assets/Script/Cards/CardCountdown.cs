using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CardCountdown : NetworkBehaviour
{
    public float fillTime = 5f;
    private Slider timerBar;
    private Hand fillHand;
    private float t;
    private float retryTimer;

    void Update()
    {
        if (!IsOwner) return;

        if (timerBar == null)
        {
            GameObject obj = GameObject.Find("TimerBar");
            if (obj != null) timerBar = obj.GetComponent<Slider>();
            return;
        }

        if (fillHand == null || fillHand.playerHand == null)
        {
            retryTimer -= Time.deltaTime;
            if (retryTimer <= 0)
            {
                retryTimer = 0.5f;
                fillHand = FindAnyObjectByType<Hand>();
                if (fillHand != null) fillHand.playerHand = PlayerHand.Local();
            }
            return;
        }

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