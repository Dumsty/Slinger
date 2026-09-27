using UnityEngine;

/// <summary>
/// Locks the X and Z rotation axes on this Rigidbody so it can't tip over
/// or roll - used on the player so physics collisions/slopes don't cause
/// unwanted rotation, while still allowing Y rotation (turning).
/// </summary>
public class PreventTipping : MonoBehaviour
{
    [SerializeField]
    private RigidbodyConstraints constraints =
        RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

    void Awake()
    {
        GetComponent<Rigidbody>().constraints = constraints;
    }
}