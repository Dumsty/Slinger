using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Hides this player's renderers from other clients while invisible - the
/// owner still sees their own model. Visibility state is a NetworkVariable
/// so it automatically syncs and updates on every client via OnValueChanged.
/// </summary>

using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering;

public class PlayerInvisibility : NetworkBehaviour
{
    public Transform wizardVisual;
    public Renderer capsuleRenderer;

    private NetworkVariable<bool> invisible =
        new NetworkVariable<bool>(false);

    private Renderer[] renderers;
    private bool[] originallyEnabled;
    private ShadowCastingMode[] originalShadowModes;

    public bool IsInvisible => invisible.Value;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originallyEnabled = new bool[renderers.Length];
        originalShadowModes = new ShadowCastingMode[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originallyEnabled[i] = renderers[i].enabled;
            originalShadowModes[i] = renderers[i].shadowCastingMode;
        }
    }

    public override void OnNetworkSpawn()
    {
        invisible.OnValueChanged += OnInvisibleChanged;
        ApplyVisibility(invisible.Value);
    }

    public override void OnNetworkDespawn()
    {
        invisible.OnValueChanged -= OnInvisibleChanged;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            renderers[i].enabled = originallyEnabled[i];
            renderers[i].shadowCastingMode = originalShadowModes[i];
        }
    }

    void OnInvisibleChanged(bool oldValue, bool newValue)
    {
        ApplyVisibility(newValue);
    }

    void ApplyVisibility(bool isInvisible)
    {
        bool hideFromOthers = isInvisible && !IsOwner;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            bool isWizard = wizardVisual != null &&
                (r.transform == wizardVisual ||
                 r.transform.IsChildOf(wizardVisual));

            bool isCapsule = r == capsuleRenderer;

            // Preserve originally disabled renderers.
            r.enabled = originallyEnabled[i] &&
                        !isCapsule &&
                        !hideFromOthers;

            // Owner sees only the Wizard's shadow.
            if (IsOwner && isWizard && r.enabled)
            {
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                r.shadowCastingMode = originalShadowModes[i];
            }
        }
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
