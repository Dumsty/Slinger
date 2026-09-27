using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Projectile that blinds the enemy player on impact. Owner is stored in
/// a NetworkVariable (not a plain field) so it correctly syncs to every
/// client - without that, clients running their own local physics would
/// see the default/unset value instead of the real owner.
/// </summary>
public class BlindBoltExplode : NetworkBehaviour
{
    public float blindDuration = 3f;
    public float range = 30f;
    public NetworkVariable<ulong> ownerId = new NetworkVariable<ulong>(ulong.MaxValue);

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (Vector3.Distance(startPos, transform.position) >= range)
            TryDestroy();
    }

    void OnCollisionEnter(Collision col)
    {
        // GetComponentInParent rather than GetComponent, since the actual
        // collider hit may belong to a child hitbox rather than the
        // player's root object where NetworkObject/PlayerVision live.
        NetworkObject netObj = col.gameObject.GetComponentInParent<NetworkObject>();

        if (netObj != null && netObj.OwnerClientId == ownerId.Value) { TryDestroy(); return; }

        // Only the server applies effects - a client's own physics may
        // fire this same collision event locally, but shouldn't act on it.
        if (!IsServer) return;

        if (netObj != null)
        {
            PlayerVision vision = netObj.GetComponentInChildren<PlayerVision>();
            if (vision != null)
            {
                // Target only the hit player's own client, not a broadcast.
                ClientRpcParams targetParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { netObj.OwnerClientId } }
                };
                vision.ApplyBlindClientRpc(blindDuration, targetParams);
            }
        }

        TryDestroy();
    }

    void TryDestroy()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            if (NetworkManager.Singleton.IsServer)
                netObj.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}