using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Spawns a local, non-networked effect where this object despawned.
/// Runs on every client, so each sees the impact without an RPC.
/// </summary>
public class SpawnEffectOnDespawn : NetworkBehaviour
{
    [SerializeField] private GameObject impactEffect;

    [Tooltip("Safety cleanup if the effect doesn't destroy itself.")]
    [SerializeField] private float maxLifetime = 5f;

    public override void OnNetworkDespawn()
    {
        ImpactEffect.Play(impactEffect, transform.position, default, maxLifetime);
    }
}