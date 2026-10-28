using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Frees child particle systems when this networked object despawns, so
/// trails fade out naturally instead of vanishing with the projectile.
/// Each system should have Stop Action set to Destroy so it cleans itself up.
/// </summary>
public class DetachParticlesOnDespawn : NetworkBehaviour
{
    public override void OnNetworkDespawn()
    {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>())
        {
            ps.transform.SetParent(null, true);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}