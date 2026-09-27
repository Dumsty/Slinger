using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Runs the local player's card draw timer: fills a UI bar over time and
/// deals a new card into their hand once it completes. Only draws while a
/// match (or solo practice) is actually in progress.
/// </summary>
public class CardCountdown : NetworkBehaviour
{
    [Tooltip("Base time between card draws, in seconds.")]
    public float fillTime = 5f;

    [Tooltip("Subtracted from fillTime while a draw-speed buff is active.")]
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

        // TimerBar lives in the HUD, which may not exist yet when this
        // spawns, so keep retrying until it's found.
        if (timerBar == null)
        {
            GameObject obj = GameObject.Find("TimerBar");
            if (obj != null) timerBar = obj.GetComponent<Slider>();
            return;
        }

        // Same idea for Hand/PlayerHand/Deck - retry every 0.5s until resolved.
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

        // Safety net: periodically re-check that the cached PlayerHand is
        // still the real one, in case it was resolved to a stale/wrong
        // instance during a timing race at match start.
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

        // No drawing in the lobby or between rounds.
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
        // Re-fetch the current hand rather than trusting the cached
        // reference, so a stale fillHand can't cause a card to be lost.
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