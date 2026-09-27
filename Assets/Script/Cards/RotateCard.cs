using UnityEngine;

/// <summary>
/// Continuously rotates a child renderer - used for things like a spinning
/// card icon or pickup item, not tied to any networking.
/// </summary>
public class RotateObject : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second, per axis.")]
    public Vector3 rotationSpeed = new Vector3(0, 50, 0);

    private Transform target;

    private void Awake()
    {
        // Rotates the first child renderer found, not this object itself,
        // so the parent's own transform/collider stays unaffected.
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
            target = renderer.transform;
    }

    private void Update()
    {
        if (target != null)
            target.Rotate(rotationSpeed * Time.deltaTime);
    }
}