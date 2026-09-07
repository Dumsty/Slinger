using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CardCountdown : NetworkBehaviour
{
    public float fillTime = 5f;
    public float DrawSpeedBonus;
    private Slider timerBar;
    private Hand fillHand;
    private float t;
    private float retryTimer;
    private float revalidateTimer;

    public static CardCountdown Local()
    {
        foreach (var cc in FindObjectsByType<CardCountdown>(FindObjectsSortMode.None))
            if (cc.IsOwner) return cc;
        return null;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (DeckStation.editingDeck) return;

        if (timerBar == null)
        {
            GameObject obj = GameObject.Find("TimerBar");
            if (obj != null) timerBar = obj.GetComponent<Slider>();
            return;
        }

        if (fillHand == null || fillHand.playerHand == null || fillHand.deck == null)
        {
            retryTimer -= Time.deltaTime;
            if (retryTimer <= 0)
            {
                retryTimer = 0.5f;
                fillHand = Hand.Local();
                if (fillHand != null && fillHand.playerHand == null) fillHand.playerHand = PlayerHand.Local();
            }
            return;
        }

        revalidateTimer -= Time.deltaTime;
        if (revalidateTimer <= 0)
        {
            revalidateTimer = 2f;
            PlayerHand actualHand = PlayerHand.Local();
            if (actualHand != null && actualHand != fillHand.playerHand)
            {
                Debug.LogWarning("CardCountdown: fillHand.playerHand was stale, correcting reference.");
                fillHand.playerHand = actualHand;
            }
        }

        if (MatchManager.Singleton == null || !MatchManager.Singleton.IsMatchInProgress())
        {
            t = 0;
            timerBar.value = 0;
            return;
        }

        bool full = System.Array.TrueForAll(fillHand.playerHand.hand, c => c != null);
        bool deckEmpty = fillHand.deck.IsEmpty();
        if (full || deckEmpty)
        {
            t = 0;
            timerBar.value = 0;
            return;
        }

        float effectiveFillTime = Mathf.Max(0.1f, fillTime - DrawSpeedBonus);

        t += Time.deltaTime;
        timerBar.value = t / effectiveFillTime;

        if (t >= effectiveFillTime)
        {
            t = 0;
            GiveCard();
        }
    }

    void GiveCard()
    {
        PlayerHand target = PlayerHand.Local();
        if (target == null) target = fillHand.playerHand;

        GameObject prefab = fillHand.deck.DrawCard();
        if (prefab == null) return;

        for (int i = 0; i < target.hand.Length; i++)
            if (target.hand[i] == null)
            {
                Card card = Instantiate(prefab).GetComponent<Card>();
                card.prefabRef = prefab;
                target.SetCard(i, card);
                break;
            }
    }
}