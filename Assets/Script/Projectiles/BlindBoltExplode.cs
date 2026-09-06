using UnityEngine;
using Unity.Netcode;

public class BlindBoltExplode : MonoBehaviour
{
    public float blindDuration = 3f;
    public float range = 30f;
    public ulong ownerId;
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
        NetworkObject netObj = col.gameObject.GetComponent<NetworkObject>();
        if (netObj != null && netObj.OwnerClientId == ownerId) { TryDestroy(); return; }

        if (netObj != null)
        {
            PlayerVision vision = col.gameObject.GetComponent<PlayerVision>();
            Debug.Log($"BlindBolt hit {col.gameObject.name}, netObj found, PlayerVision found={vision != null}");

            if (vision != null)
            {
                ClientRpcParams targetParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { netObj.OwnerClientId } }
                };
                Debug.Log($"Sending blind RPC to client {netObj.OwnerClientId}");
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