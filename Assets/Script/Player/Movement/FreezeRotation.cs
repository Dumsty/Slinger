using UnityEngine;

public class PreventTipping : MonoBehaviour
{
    [SerializeField] private RigidbodyConstraints constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

    void Awake()
    {
        GetComponent<Rigidbody>().constraints = constraints;
    }
}