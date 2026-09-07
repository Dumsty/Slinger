using UnityEngine;
using Unity.Netcode;

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
        NetworkObject netObj = col.gameObject.GetComponentInParent<NetworkObject>();
        Debug.Log($"BlindBolt hit {col.gameObject.name}, netObj found={netObj != null}" +
                (netObj != null ? $", OwnerClientId={netObj.OwnerClientId}, boltOwnerId={ownerId.Value}" : ""));

        if (netObj != null && netObj.OwnerClientId == ownerId.Value) { TryDestroy(); return; }
        if (!IsServer) return;

        if (netObj != null)
        {
            PlayerVision vision = netObj.GetComponentInChildren<PlayerVision>();
            Debug.Log($"PlayerVision found={vision != null} on {netObj.gameObject.name}");
            if (vision != null)
            {
                ClientRpcParams targetParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { netObj.OwnerClientId } }
                };
                vision.ApplyBlindClientRpc(blindDuration, targetParams);
                Debug.Log($"Sent ApplyBlindClientRpc to client {netObj.OwnerClientId}");
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