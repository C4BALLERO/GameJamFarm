using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Música de fondo en bucle. Pon tu audio exportado como <c>Assets/Resources/BackgroundMusic.mp3</c>
/// (o .wav/.ogg) para que cargue en runtime; en editor también puedes asignar el clip en el inspector.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplayBackgroundMusic : MonoBehaviour
{
    public static GameplayBackgroundMusic Instance { get; private set; }

    [SerializeField] private AudioClip musicClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.42f;
    [SerializeField] private bool pauseWhenGameplayFrozen = true;
    [SerializeField] [Range(0f, 1f)] private float pausedVolumeMultiplier = 0.35f;

    private AudioSource _source;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.loop = true;
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;

        if (musicClip == null)
            musicClip = Resources.Load<AudioClip>("BackgroundMusic");
#if UNITY_EDITOR
        if (musicClip == null)
            musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/resources_08/MusicaFondo.mp3");
        if (musicClip == null)
            musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/resources_08/BackgroundMusic.mp3");
#endif

        if (musicClip != null)
        {
            _source.clip = musicClip;
            _source.volume = volume;
            _source.Play();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!pauseWhenGameplayFrozen || _source == null || !_source.isPlaying)
            return;

        var frozen = GameManager.Instance != null && GameManager.Instance.IsGameplayFrozen;
        var target = frozen ? volume * pausedVolumeMultiplier : volume;
        _source.volume = Mathf.MoveTowards(_source.volume, target, Time.unscaledDeltaTime * 0.8f);
    }

    /// <summary>Para asignar clip desde editor script.</summary>
    public void SetClip(AudioClip clip)
    {
        musicClip = clip;
        if (_source != null && clip != null)
        {
            _source.clip = clip;
            if (!_source.isPlaying)
                _source.Play();
        }
    }

    /// <summary>Crea el reproductor global una sola vez si aún no existe.</summary>
    public static void EnsureGlobal()
    {
        if (FindFirstObjectByType<GameplayBackgroundMusic>() != null)
            return;

        var go = new GameObject("GameplayBackgroundMusic");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<GameplayBackgroundMusic>();
    }
}
