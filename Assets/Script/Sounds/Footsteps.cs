using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Plays a looping walking sound that fades in while a player moves on the
/// ground and fades out quickly when they stop. Only plays on tagged
/// surfaces (grass) and never while sliding. The owner publishes its moving
/// state and pitch through NetworkVariables so both players hear the same
/// thing positionally - including when the walker is invisible.
/// </summary>
public class FootstepAudio : NetworkBehaviour
{
    [Header("Clip")]
    public AudioClip walkLoop;
    [Range(0f, 1f)] public float volume = 0.6f;

    [Header("3D Audio")]
    public float minDistance = 2f;
    public float maxDistance = 30f;

    [Header("Fade")]
    [Tooltip("Seconds to reach full volume when starting to move.")]
    public float fadeInTime = 0.08f;
    [Tooltip("Seconds to go silent after stopping.")]
    public float fadeOutTime = 0.12f;

    [Header("Movement")]
    [Tooltip("Minimum horizontal speed before the sound plays.")]
    public float moveThreshold = 0.5f;
    [Tooltip("Speed at which the loop plays at normal pitch.")]
    public float referenceSpeed = 6f;

    [Header("Surface")]
    [Tooltip("Tag the ground must have for this sound to play.")]
    public string surfaceTag = "Grass";
    [Tooltip("How far below the player to check for ground.")]
    public float groundCheckDistance = 1.5f;
    public LayerMask groundMask = ~0;

    // Owner writes, everyone reads - no RPC needed.
    private readonly NetworkVariable<bool> isWalking = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    // Synced too, otherwise remote listeners would always hear pitch 1.
    private readonly NetworkVariable<float> walkPitch = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private AudioSource audioSource;
    private PlayerMovement movement;
    private Rigidbody rb;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = walkLoop;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
        audioSource.spatialBlend = 1f;              // 3D - heard from this player's position
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
    }

    public override void OnNetworkSpawn()
    {
        // Runs silently at volume 0 so fades never restart the clip.
        if (walkLoop != null) audioSource.Play();
    }

    void Update()
    {
        if (walkLoop == null) return;

        if (IsOwner) PublishWalkingState();

        // Every client (including the owner) applies the synced pitch and
        // fades its own copy.
        audioSource.pitch = walkPitch.Value;

        float target = isWalking.Value ? volume : 0f;
        float rate = isWalking.Value
            ? volume / Mathf.Max(0.01f, fadeInTime)
            : volume / Mathf.Max(0.01f, fadeOutTime);

        audioSource.volume = Mathf.MoveTowards(audioSource.volume, target, rate * Time.deltaTime);
    }

    void PublishWalkingState()
    {
        bool walking;

        if (PauseMenu.paused || DeckStation.editingDeck)
        {
            walking = false;
        }
        else
        {
            Vector3 flat = rb.linearVelocity;
            flat.y = 0f;
            float speed = flat.magnitude;

            walking = speed >= moveThreshold
                      && movement.IsGrounded
                      && !movement.IsSliding
                      && OnSurface();

            // Tie playback speed to movement speed so buffs sound faster.
            // Only published on real changes, so speed wobble doesn't spam
            // a network update every frame.
            if (walking)
            {
                float p = Mathf.Clamp(speed / Mathf.Max(0.1f, referenceSpeed), 0.7f, 1.6f);
                if (Mathf.Abs(walkPitch.Value - p) > 0.02f) walkPitch.Value = p;
            }
        }

        if (isWalking.Value != walking) isWalking.Value = walking;
    }

    // Raycasts down to check what the player is standing on. Grass only.
    bool OnSurface()
    {
        if (Physics.Raycast(transform.position, Vector3.down,
                            out RaycastHit hit, groundCheckDistance, groundMask,
                            QueryTriggerInteraction.Ignore))
        {
            return hit.collider.CompareTag(surfaceTag);
        }
        return false;
    }
}