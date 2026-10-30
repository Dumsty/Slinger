using UnityEngine;

/// <summary>
/// Shared helper for one-shot local effects (impacts, explosions).
/// Not networked: callers already run on every client.
/// </summary>
public static class ImpactEffect
{
    /// <param name="normal">Surface normal to orient against; omit for no rotation.</param>
    public static GameObject Play(GameObject prefab, Vector3 position,
                                  Vector3 normal = default, float maxLifetime = 5f)
    {
        if (prefab == null) return null;

        Quaternion rot = normal.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        GameObject fx = Object.Instantiate(prefab, position, rot);
        if (maxLifetime > 0f) Object.Destroy(fx, maxLifetime);
        return fx;
    }
}