using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Ready-to-use world pickup that instantly fills the mana of whoever walks
/// into it, by calling PlayerMana.RefillToFull(). Not placed in any scene yet.
///
/// To use it: add this component to an empty GameObject (in the Editor that
/// also adds the NetworkObject and trigger SphereCollider it needs), put the
/// visual - a mesh, particles - as a child, and drop it into the Game scene.
/// In-scene placed is simplest; to spawn it at runtime instead, make it a
/// prefab and register it in the NetworkManager's Network Prefabs list.
///
/// The server decides who collected it, so two players can't both grab it,
/// then hides it for everyone until it respawns.
///
/// Other ways to give mana (a shop item, a card) don't need this script -
/// call PlayerMana.RefillToFull() or AddMana() on the owning client or the
/// server.
/// </summary>
public class ManaRefillPickup : NetworkBehaviour
{
    [Tooltip("Seconds before the pickup reappears after being collected. 0 = single use: it stays gone until the scene reloads.")]
    [Min(0f)] public float respawnTime = 20f;

    [Tooltip("Only let players collect it while a match or solo practice is running.")]
    public bool onlyDuringMatch = true;

    [Tooltip("Ignore players whose mana is already full, so the pickup isn't wasted.")]
    public bool ignoreFullMana = true;

    [Tooltip("Shown while the pickup is available and hidden while it waits to respawn. Leave empty to toggle every Renderer on this object and its children instead.")]
    public GameObject visual;

    [Tooltip("Optional sound played at the pickup, for every player, when it's collected.")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 1f;

    // Server-written; every client shows/hides the pickup from it.
    private NetworkVariable<bool> available = new NetworkVariable<bool>(true);

    public bool IsAvailable => available.Value;

    public override void OnNetworkSpawn()
    {
        available.OnValueChanged += OnAvailableChanged;
        ShowAvailable(available.Value);
    }

    public override void OnNetworkDespawn()
    {
        available.OnValueChanged -= OnAvailableChanged;
    }

    void OnAvailableChanged(bool previous, bool current)
    {
        ShowAvailable(current);

        if (previous && !current && pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
    }

    void ShowAvailable(bool isAvailable)
    {
        if (visual != null)
        {
            visual.SetActive(isAvailable);
            return;
        }

        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = isAvailable;
    }

    // Trigger events fire on every machine, but only the server acts on
    // them. Stay (as well as Enter) covers a player who is already standing
    // on it when it respawns, or whose mana stops being full.
    void OnTriggerEnter(Collider other) { TryCollect(other); }
    void OnTriggerStay(Collider other) { TryCollect(other); }

    void TryCollect(Collider other)
    {
        if (!IsServer || !IsSpawned || !available.Value) return;
        if (onlyDuringMatch && (MatchManager.Singleton == null || !MatchManager.Singleton.IsMatchInProgress())) return;

        PlayerMana mana = other.GetComponentInParent<PlayerMana>();
        if (mana == null) return;
        if (ignoreFullMana && mana.IsFull) return;

        mana.RefillToFull();
        available.Value = false;

        if (respawnTime > 0f) StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnTime);
        available.Value = true;
    }

    // Editor only: runs when the component is first added (or Reset from
    // its menu) and sets up the trigger and NetworkObject it needs.
    void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        if (trigger == null) trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;

        if (GetComponentInParent<NetworkObject>() == null)
            gameObject.AddComponent<NetworkObject>();
    }
}