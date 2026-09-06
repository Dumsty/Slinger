using UnityEngine;

public class RotateObject : MonoBehaviour
{
    private Transform target;

    public Vector3 rotationSpeed = new Vector3(0, 50, 0);

    private void Awake()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();

        if (renderer != null)
            target = renderer.transform;
    }

    private void Update()
    {
        if (target != null)
        {
            target.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}