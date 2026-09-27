using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Hides this player's renderers from other clients while invisible - the
/// owner still sees their own model. Visibility state is a NetworkVariable
/// so it automatically syncs and updates on every client via OnValueChanged.
/// </summary>
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

    // Only hides renderers for non-owning clients, so the invisible player
    // still sees their own body (if visible at all in first person).
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