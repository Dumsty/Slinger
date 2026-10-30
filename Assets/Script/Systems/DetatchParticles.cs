using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Frees child particle systems when this networked object despawns, so
/// trails fade out naturally instead of vanishing with the projectile.
/// Each detached system destroys itself once its last particle has died.
/// </summary>
public class DetachParticlesOnDespawn : NetworkBehaviour
{
    [Tooltip("How much faster detached particles finish their fade.")]
    [SerializeField] private float fadeOutSpeed = 3f;

    [Tooltip("Used if a system's lifetime can't be read (e.g. curve mode).")]
    [SerializeField] private float fallbackLifetime = 5f;

    public override void OnNetworkDespawn()
    {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>())
        {
            ps.transform.SetParent(null, true);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            var main = ps.main;
            main.simulationSpeed *= fadeOutSpeed;

            // Longest a living particle can still last, scaled by how fast
            // we're now simulating, plus a small safety margin.
            float life = main.startLifetime.constantMax;
            if (life <= 0f) life = fallbackLifetime;
            float remaining = life / Mathf.Max(0.01f, main.simulationSpeed) + 0.5f;

            Destroy(ps.gameObject, remaining);
        }
    }
}