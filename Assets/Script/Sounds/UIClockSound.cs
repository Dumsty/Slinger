using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Plays a randomly chosen click sound for every UI Button in the game.
/// Hooks all buttons automatically on scene load; call Hook() for buttons
/// created at runtime (e.g. the deck builder's generated rows).
/// </summary>
public class UIClickSound : MonoBehaviour
{
    public static UIClickSound Instance { get; private set; }

    [Tooltip("One of these is picked at random on each click.")]
    public AudioClip[] clickSounds;
    [Range(0f, 1f)] public float volume = 0.7f;

    private AudioSource audioSource;

    void Awake()
    {
        // Survives scene loads, so only one copy should ever exist.
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;   // 2D - UI sounds aren't positional

        SceneManager.sceneLoaded += OnSceneLoaded;
        HookAll();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) => HookAll();

    // Finds every button in the loaded scenes, including inactive ones
    // (menus that start hidden), and attaches the click sound.
    public void HookAll()
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Hook(b);
    }

    // Remove-then-add so a button never gets the listener twice.
    public void Hook(Button b)
    {
        if (b == null) return;
        b.onClick.RemoveListener(Play);
        b.onClick.AddListener(Play);
    }

    public void Play()
    {
        if (clickSounds == null || clickSounds.Length == 0) return;
        audioSource.PlayOneShot(clickSounds[Random.Range(0, clickSounds.Length)], volume);
    }
}