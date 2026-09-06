using UnityEngine;
using Unity.Netcode;

public class PlayerInvisibility : NetworkBehaviour
{
    private NetworkVariable<bool> invisible = new NetworkVariable<bool>(false);
    private Renderer[] renderers;

    public bool IsInvisible => invisible.Value;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    public override void OnNetworkSpawn()
    {
        invisible.OnValueChanged += OnInvisibleChanged;
        ApplyVisibility(invisible.Value);
    }

    public override void OnNetworkDespawn()
    {
        invisible.OnValueChanged -= OnInvisibleChanged;
    }

    void OnInvisibleChanged(bool oldValue, bool newValue)
    {
        ApplyVisibility(newValue);
    }

    void ApplyVisibility(bool isInvisible)
    {
        bool shouldHide = isInvisible && !IsOwner;
        foreach (var r in renderers)
            r.enabled = !shouldHide;
    }

    public void SetInvisible(bool value)
    {
        if (IsServer)
            invisible.Value = value;
        else
            SetInvisibleServerRpc(value);
    }

    [ServerRpc]
    void SetInvisibleServerRpc(bool value)
    {
        invisible.Value = value;
    }
}