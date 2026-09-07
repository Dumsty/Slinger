using UnityEngine;
using Unity.Netcode;
using System.Collections;

public enum MatchPhase { Lobby, Countdown, InProgress, MatchOver, Solo }

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
    public NetworkVariable<int> winner = new NetworkVariable<int>(0);
    public NetworkVariable<ulong> clientAId = new NetworkVariable<ulong>(ulong.MaxValue);
    public NetworkVariable<ulong> clientBId = new NetworkVariable<ulong>(ulong.MaxValue);

    private ulong clientA = ulong.MaxValue;
    private ulong clientB = ulong.MaxValue;
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

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            OnClientConnected(clientId);
    }

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

    bool HasFullDeck(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return false;
        if (client.PlayerObject == null) return false;

        Hand hand = client.PlayerObject.GetComponent<Hand>();
        if (hand == null || hand.deck == null) return false;

        return hand.deckSize.Value >= hand.deck.maxDeckSize;
    }

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