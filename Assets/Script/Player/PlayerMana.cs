using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// The player's mana. Playing a card spends its Card.manaCost (see
/// SelectCard), and mana refills by regenAmount every regenInterval seconds
/// up to maxMana. Every number here is an Inspector field on the Player 1
/// prefab; each card's cost is the "Mana Cost" field on its card prefab.
///
/// Mana is owner-authoritative, like playing cards: the owning client
/// regenerates and spends it and writes currentMana, which everyone can
/// read. Server code never writes it directly - ResetMana(), RefillToFull()
/// and AddMana() go through a ClientRpc to the owner when called on the
/// server, the same way Health.ClearBuffs() reaches BuffManager.
///
/// The owner also drives the HUD: "ManaBar" (a Filled Image) and the
/// optional "ManaText" label, found by name the same way Health finds
/// "HealthBar".
/// </summary>
public class PlayerMana : NetworkBehaviour
{
    [Header("Mana")]
    [Tooltip("Most mana a player can hold. The HUD bar shows current / max.")]
    [Min(1)] public int maxMana = 10;

    [Tooltip("Mana a player has at the start of every round, after dying, and back in the lobby.")]
    [Min(0)] public int startingMana = 5;

    [Header("Regeneration")]
    [Tooltip("Mana gained each time the regen timer completes.")]
    [Min(0)] public int regenAmount = 1;

    [Tooltip("Seconds between regen ticks.")]
    [Min(0.1f)] public float regenInterval = 3f;

    [Tooltip("Only regenerate while a match or solo practice is running (the same rule as card drawing). Untick to regenerate in the lobby too.")]
    public bool regenOnlyDuringMatch = true;

    [Header("HUD")]
    [Tooltip("Color the mana bar and number flash when you try to play a card you can't afford. Also used for the selected card's cost while it's too expensive.")]
    public Color notEnoughManaColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("How long the not-enough-mana flash lasts, in seconds.")]
    [Min(0f)] public float notEnoughManaFlashTime = 0.4f;

    // Written only by the owning client (see summary) - read it anywhere.
    public NetworkVariable<int> currentMana = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool IsFull => currentMana.Value >= maxMana;

    // Cost line color on the HUD while the selected card is affordable.
    private const string AffordableCostColor = "#8CCBFF";

    private float regenTimer;
    private float flashTimer;

    // HUD (owner only)
    private Image manaBar;
    private TMP_Text manaText;
    private Color manaBarColor = Color.white;
    private Color manaTextColor = Color.white;
    private float hudRetryTimer;
    private SelectCard selectCard;

    // Last values written to manaText, so the string is only rebuilt on change.
    private int shownMana = -1;
    private int shownMax = -1;
    private int shownCost = -2;
    private bool shownAffordable;

    // Scans for the owned instance, like PlayerHand.Local() (but using the
    // FindObjectsByType overload that isn't deprecated in Unity 6.5).
    public static PlayerMana Local()
    {
        foreach (var pm in FindObjectsByType<PlayerMana>())
            if (pm.IsOwner) return pm;
        return null;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner) ApplyReset();
    }

    void OnValidate()
    {
        if (startingMana > maxMana) startingMana = maxMana;
    }

    void Update()
    {
        if (!IsOwner) return;

        // maxMana can be lowered in the Inspector during Play mode.
        if (currentMana.Value > maxMana) SetMana(maxMana);

        TickRegen();
        UpdateHud();
    }

    void TickRegen()
    {
        bool matchRunning = MatchManager.Singleton != null && MatchManager.Singleton.IsMatchInProgress();
        if ((regenOnlyDuringMatch && !matchRunning) || regenAmount <= 0 || IsFull)
        {
            // Held at zero, so after spending from full (or when a round
            // starts) the next point arrives a full interval later.
            regenTimer = 0f;
            return;
        }

        float interval = Mathf.Max(0.1f, regenInterval);
        regenTimer += Time.deltaTime;
        if (regenTimer < interval) return;

        regenTimer -= interval;
        SetMana(currentMana.Value + regenAmount);
    }

    public bool CanAfford(int cost)
    {
        return cost <= 0 || currentMana.Value >= cost;
    }

    // Owner only (cards are played on the owning client). Spends the mana and
    // returns true, or returns false and flashes the HUD if there isn't enough,
    // in which case nothing is spent.
    public bool TrySpend(int cost)
    {
        if (!IsOwner) return false;
        if (cost <= 0) return true;

        if (currentMana.Value < cost)
        {
            flashTimer = notEnoughManaFlashTime;
            return false;
        }

        SetMana(currentMana.Value - cost);
        return true;
    }

    // Hook for the mana refill item (see ManaRefillPickup): instantly fills
    // this player's mana to maxMana. Call it on the owning client or the server.
    public void RefillToFull()
    {
        if (IsOwner) SetMana(maxMana);
        else if (IsServer) RefillToFullClientRpc();
    }

    // Test aid: in Play mode, pick this from the component's three-dot menu
    // on your own player to try the refill hook without placing a pickup.
    [ContextMenu("Refill Mana To Full")]
    void RefillFromContextMenu()
    {
        if (IsSpawned) RefillToFull();
    }

    // Adds mana (or removes it, if negative), clamped to 0..maxMana. Call it
    // on the owning client or the server.
    public void AddMana(int amount)
    {
        if (amount == 0) return;
        if (IsOwner) SetMana(currentMana.Value + amount);
        else if (IsServer) AddManaClientRpc(amount);
    }

    // Back to startingMana with a fresh regen timer. MatchManager calls this
    // (server-side) wherever it resets health: round start, death, lobby.
    public void ResetMana()
    {
        if (IsOwner) ApplyReset();
        else if (IsServer) ResetManaClientRpc();
    }

    [ClientRpc]
    void RefillToFullClientRpc()
    {
        if (!IsOwner) return;
        SetMana(maxMana);
    }

    [ClientRpc]
    void AddManaClientRpc(int amount)
    {
        if (!IsOwner) return;
        SetMana(currentMana.Value + amount);
    }

    [ClientRpc]
    void ResetManaClientRpc()
    {
        if (!IsOwner) return;
        ApplyReset();
    }

    void ApplyReset()
    {
        regenTimer = 0f;
        flashTimer = 0f;
        SetMana(startingMana);
    }

    void SetMana(int value)
    {
        int clamped = Mathf.Clamp(value, 0, maxMana);
        if (currentMana.Value != clamped) currentMana.Value = clamped;
    }

    void UpdateHud()
    {
        // ManaBar lives in the HUD, which may not exist yet when this spawns,
        // so keep retrying until it's found (same as Health's HealthBar).
        if (manaBar == null)
        {
            hudRetryTimer -= Time.deltaTime;
            if (hudRetryTimer > 0f) return;
            hudRetryTimer = 0.5f;

            FindHud();
            if (manaBar == null) return;
        }

        if (flashTimer > 0f) flashTimer -= Time.deltaTime;
        float flash = notEnoughManaFlashTime > 0f ? Mathf.Clamp01(flashTimer / notEnoughManaFlashTime) : 0f;

        manaBar.fillAmount = maxMana > 0 ? (float)currentMana.Value / maxMana : 0f;
        manaBar.color = Color.Lerp(manaBarColor, notEnoughManaColor, flash);

        if (manaText != null)
        {
            manaText.color = Color.Lerp(manaTextColor, notEnoughManaColor, flash);
            UpdateHudText();
        }
    }

    void FindHud()
    {
        GameObject bar = GameObject.Find("ManaBar");
        if (bar == null) return;

        manaBar = bar.GetComponent<Image>();
        if (manaBar == null) return;
        manaBarColor = manaBar.color;

        GameObject text = GameObject.Find("ManaText");
        manaText = text != null ? text.GetComponent<TMP_Text>() : null;
        if (manaText != null) manaTextColor = manaText.color;
        shownMana = -1; // force a text refresh
    }

    // "current/max", plus the selected card's cost above it (blue if you can
    // afford it, red if you can't).
    void UpdateHudText()
    {
        if (selectCard == null) selectCard = GetComponent<SelectCard>();
        Card selected = selectCard != null ? selectCard.SelectedCard : null;
        int cost = selected != null ? selected.manaCost : -1;
        bool affordable = CanAfford(cost);

        if (currentMana.Value == shownMana && maxMana == shownMax && cost == shownCost && affordable == shownAffordable)
            return;

        shownMana = currentMana.Value;
        shownMax = maxMana;
        shownCost = cost;
        shownAffordable = affordable;

        string text = currentMana.Value + "/" + maxMana;
        if (cost >= 0)
        {
            string color = affordable ? AffordableCostColor : "#" + ColorUtility.ToHtmlStringRGB(notEnoughManaColor);
            text = "<size=70%><color=" + color + ">Cost " + cost + "</color></size>\n" + text;
        }
        manaText.text = text;
    }
}