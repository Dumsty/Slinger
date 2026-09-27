using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Lobby - waiting to ready up (players can shop/move freely, no damage).
/// Countdown - both players readied, 10s countdown before match starts.
/// InProgress - actual 1v1 match, first to winScore kills wins.
/// MatchOver - brief pause showing the winner before returning to Lobby.
/// Solo - single-player practice mode; see soloPracticing for whether
/// the player is actively practicing vs. still in the solo lobby.
/// </summary>
public enum MatchPhase { Lobby, Countdown, InProgress, MatchOver, Solo }

/// <summary>
/// Server-authoritative match state machine. Owns spawn point assignment,
/// ready-up/countdown flow, scoring, win detection, and resetting players
/// (position/health/buffs/hand) between rounds. All state lives in
/// NetworkVariables so every client can read it directly for their own UI.
/// </summary>
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Singleton;

    public Transform lobbySpawnPoint;
    public Transform spawnPointA;
    public Transform spawnPointB;
    public float countdownDuration = 10f;
    public int winScore = 3;

    public NetworkVariable<MatchPhase> phase = new NetworkVariable<MatchPhase>(MatchPhase.Lobby);
    public NetworkVariable<bool> playerAReady = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> playerBReady = new NetworkVariable<bool>(false);
    public NetworkVariable<int> scoreA = new NetworkVariable<int>(0);
    public NetworkVariable<int> scoreB = new NetworkVariable<int>(0);
    public NetworkVariable<float> countdownRemaining = new NetworkVariable<float>(0f);
    public NetworkVariable<bool> isSoloMode = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> soloPracticing = new NetworkVariable<bool>(false);
    public NetworkVariable<int> winner = new NetworkVariable<int>(0); // 0 = none, 1 = A, 2 = B

    // clientA/clientB are the server's own working copies of who occupies
    // each slot. clientAId/clientBId are the networked mirrors of the same
    // values, so every client can independently determine "am I A or B?"
    // (needed since clientA/clientB themselves are never synced).
    private ulong clientA = ulong.MaxValue;
    private ulong clientB = ulong.MaxValue;
    public NetworkVariable<ulong> clientAId = new NetworkVariable<ulong>(ulong.MaxValue);
    public NetworkVariable<ulong> clientBId = new NetworkVariable<ulong>(ulong.MaxValue);

    private Coroutine countdownRoutine;

    void Awake()
    {
        Singleton = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Catches the host's own connection, which happens before this
        // object spawns and would otherwise never fire OnClientConnected.
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            OnClientConnected(clientId);
    }

    // Frees the disconnecting player's slot so a future connection can
    // claim it, and drops an in-progress match back to Lobby since a 1v1
    // can't continue with only one player.
    void OnClientDisconnected(ulong clientId)
    {
        if (clientId == clientA)
        {
            clientA = ulong.MaxValue;
            clientAId.Value = ulong.MaxValue;
            playerAReady.Value = false;
        }
        else if (clientId == clientB)
        {
            clientB = ulong.MaxValue;
            clientBId.Value = ulong.MaxValue;
            playerBReady.Value = false;
        }

        if (phase.Value == MatchPhase.InProgress || phase.Value == MatchPhase.Countdown)
            phase.Value = MatchPhase.Lobby;
    }

    // Assigns the first two connecting clients to slots A/B in order; any
    // further connection attempt while both slots are full is ignored.
    void OnClientConnected(ulong clientId)
    {
        if (clientA == ulong.MaxValue)
        {
            clientA = clientId;
            clientAId.Value = clientId;
        }
        else if (clientB == ulong.MaxValue && clientId != clientA)
        {
            clientB = clientId;
            clientBId.Value = clientId;
        }
        else
        {
            return;
        }

        TeleportPlayer(clientId, lobbySpawnPoint);

        Health h = GetHealthForClient(clientId);
        if (h != null) h.ResetHealth();
    }

    public void EnterSoloMode()
    {
        if (!IsServer) return;
        isSoloMode.Value = true;
        soloPracticing.Value = false;
        phase.Value = MatchPhase.Solo;
    }

    // Used by Health/CardCountdown to gate damage and card drawing.
    public bool IsMatchInProgress()
    {
        if (isSoloMode.Value) return soloPracticing.Value;
        return phase.Value == MatchPhase.InProgress;
    }

    public bool IsClientReady(ulong clientId)
    {
        if (clientId == clientAId.Value) return playerAReady.Value;
        if (clientId == clientBId.Value) return playerBReady.Value;
        return false;
    }

    // Called by ReadyToggle when the player presses R. Ready requests are
    // rejected (with a message back to the requester) unless their deck
    // is full, so both players always compete with an equal deck size.
    public void SetReady(ulong clientId, bool ready)
    {
        if (!IsServer) { SetReadyServerRpc(clientId, ready); return; }
        if (phase.Value != MatchPhase.Lobby && phase.Value != MatchPhase.Countdown && phase.Value != MatchPhase.Solo) return;

        if (ready && !HasFullDeck(clientId))
        {
            ClientRpcParams targetParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            };
            ReadyRejectedClientRpc("Deck must be full to ready up", targetParams);
            return;
        }

        if (clientId == clientA) playerAReady.Value = ready;
        else if (clientId == clientB) playerBReady.Value = ready;

        if (isSoloMode.Value)
        {
            if (ready && playerAReady.Value)
            {
                StartSoloPractice();
            }
            else if (!ready && soloPracticing.Value)
            {
                // Un-readying mid-practice sends the player back to the
                // lobby, same cleanup as dying.
                soloPracticing.Value = false;
                TeleportPlayer(clientA, lobbySpawnPoint);
                Health h = GetHealthForClient(clientA);
                if (h != null) { h.ResetHealth(); h.ClearBuffs(); }
                ClearHandForClient(clientA);
            }
        }
        else
        {
            CheckBothReady();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SetReadyServerRpc(ulong clientId, bool ready)
    {
        SetReady(clientId, ready);
    }

    [ClientRpc]
    void ReadyRejectedClientRpc(string message, ClientRpcParams rpcParams = default)
    {
        ReadyToggle.ShowRejectionMessage(message);
    }

    // Deck.deckList only exists on the owning client, so this checks the
    // synced Hand.deckSize instead of reading the deck directly.
    bool HasFullDeck(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return false;
        if (client.PlayerObject == null) return false;

        Hand hand = client.PlayerObject.GetComponent<Hand>();
        if (hand == null || hand.deck == null) return false;

        return hand.deckSize.Value >= hand.deck.maxDeckSize;
    }

    // Solo has no countdown - readying up starts practice immediately.
    void StartSoloPractice()
    {
        soloPracticing.Value = true;
        TeleportPlayer(clientA, spawnPointA);
        Health h = GetHealthForClient(clientA);
        if (h != null) h.ResetHealth();
    }

    void CheckBothReady()
    {
        if (clientA == ulong.MaxValue || clientB == ulong.MaxValue) return;
        if (playerAReady.Value && playerBReady.Value && countdownRoutine == null)
            countdownRoutine = StartCoroutine(RunCountdown());
    }

    // Ticks the countdown down each frame; aborts back to Lobby if either
    // player un-readies before it completes.
    IEnumerator RunCountdown()
    {
        phase.Value = MatchPhase.Countdown;
        countdownRemaining.Value = countdownDuration;

        while (countdownRemaining.Value > 0)
        {
            if (!playerAReady.Value || !playerBReady.Value)
            {
                phase.Value = MatchPhase.Lobby;
                countdownRoutine = null;
                yield break;
            }
            countdownRemaining.Value -= Time.deltaTime;
            yield return null;
        }

        StartMatch();
        countdownRoutine = null;
    }

    void StartMatch()
    {
        scoreA.Value = 0;
        scoreB.Value = 0;
        winner.Value = 0;
        phase.Value = MatchPhase.InProgress;
        ResetPositionsAndHealth();
    }

    // Called by Health when a player's health hits zero. Solo deaths just
    // send the player back to the lobby with no scoring; real matches
    // credit the attacker and check for a win.
    public void ReportKill(ulong attackerId, ulong victimId)
    {
        if (!IsServer) return;

        if (phase.Value == MatchPhase.Solo)
        {
            soloPracticing.Value = false;
            TeleportPlayer(victimId, lobbySpawnPoint);
            Health h = GetHealthForClient(victimId);
            if (h != null) { h.ResetHealth(); h.ClearBuffs(); }
            ClearHandForClient(victimId);
            return;
        }

        if (phase.Value != MatchPhase.InProgress) return;

        if (attackerId == clientA) scoreA.Value++;
        else if (attackerId == clientB) scoreB.Value++;

        if (scoreA.Value >= winScore || scoreB.Value >= winScore)
        {
            winner.Value = scoreA.Value >= winScore ? 1 : 2;
            phase.Value = MatchPhase.MatchOver;
            StartCoroutine(ReturnToLobbyAfterDelay(4f));
        }
        else
        {
            ResetPositionsAndHealth();
        }
    }

    // Shows the winner for a few seconds, then resets both players and
    // returns everyone to the lobby to ready up again.
    IEnumerator ReturnToLobbyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        playerAReady.Value = false;
        playerBReady.Value = false;
        winner.Value = 0;
        phase.Value = MatchPhase.Lobby;

        TeleportPlayer(clientA, lobbySpawnPoint);
        TeleportPlayer(clientB, lobbySpawnPoint);

        Health ha = GetHealthForClient(clientA);
        if (ha != null) { ha.ResetHealth(); ha.ClearBuffs(); }

        Health hb = GetHealthForClient(clientB);
        if (hb != null) { hb.ResetHealth(); hb.ClearBuffs(); }

        ClearHandForClient(clientA);
        ClearHandForClient(clientB);
    }

    // Shared between "match just started" and "someone just got a kill" -
    // teleports both players to their fixed spawn points and resets
    // health/buffs/hand for a clean start to the round.
    void ResetPositionsAndHealth()
    {
        TeleportPlayer(clientA, spawnPointA);
        TeleportPlayer(clientB, spawnPointB);

        Health ha = GetHealthForClient(clientA);
        if (ha != null) { ha.ResetHealth(); ha.ClearBuffs(); }

        Health hb = GetHealthForClient(clientB);
        if (hb != null) { hb.ResetHealth(); hb.ClearBuffs(); }

        ClearHandForClient(clientA);
        ClearHandForClient(clientB);
    }

    void ClearHandForClient(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        if (client.PlayerObject == null) return;

        PlayerHand playerHand = client.PlayerObject.GetComponent<PlayerHand>();
        if (playerHand == null) return;

        // PlayerHand.hand[] only exists on the owning client - ClearHand()
        // routes this through a ClientRpc rather than touching it directly.
        playerHand.ClearHand();
    }

    void TeleportPlayer(ulong clientId, Transform point)
    {
        if (point == null) return;
        Health h = GetHealthForClient(clientId);
        if (h != null) h.TeleportTo(point.position);
    }

    Health GetHealthForClient(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return null;
        if (client.PlayerObject == null) return null;
        return client.PlayerObject.GetComponent<Health>();
    }

    // Unsubscribe from NetworkManager events on despawn so a destroyed
    // MatchManager can't have a stale callback fire later (this caused a
    // UI glitch on the host's return to the main menu before it was added).
    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    void OnDestroy()
    {
        if (Singleton == this) Singleton = null;
    }
}