using UnityEngine;

/// <summary>
/// Bucle constante del sonido <c>walk</c> durante la partida (se detiene en pausa o si el jugador muere).
/// Coloca <c>Assets/resources/AnimalSounds/walk.mp3</c> o asigna el clip en el Inspector.
/// No usa la cola aleatoria de granja; Chicken / Pig / vaca siguen en <see cref="FarmAnimalAudio"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerWalkAudio : MonoBehaviour
{
    [SerializeField] private AudioClip walkLoopClip;
    [Tooltip("Si no hay clip asignado: AnimalSounds/walk, luego PlayerSounds/WalkLoop.")]
    [SerializeField] private string resourcePathFallback = "AnimalSounds/walk";
    [SerializeField] [Range(0f, 1f)] private float volume = 0.32f;

    private PlayerHealth _health;
    private AudioSource _source;

    private void Reset()
    {
        _health = GetComponent<PlayerHealth>();
    }

    private void Awake()
    {
        if (_health == null)
            _health = GetComponent<PlayerHealth>();

        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = true;
        _source.spatialBlend = 0f;
        _source.priority = 64;

        var clip = walkLoopClip;
        if (clip == null)
            clip = Resources.Load<AudioClip>("AnimalSounds/walk");
        if (clip == null)
            clip = Resources.Load<AudioClip>("AnimalSounds/Walk");
        if (clip == null && !string.IsNullOrWhiteSpace(resourcePathFallback))
            clip = Resources.Load<AudioClip>(resourcePathFallback);
        if (clip == null)
            clip = Resources.Load<AudioClip>("PlayerSounds/WalkLoop");

        _source.clip = clip;
        _source.volume = volume;
    }

    private void Update()
    {
        if (_source == null || _source.clip == null)
            return;

        if (Time.timeScale <= 0f)
        {
            if (_source.isPlaying)
                _source.Stop();
            return;
        }

        var dead = _health != null && _health.IsDead;
        if (dead)
        {
            if (_source.isPlaying)
                _source.Stop();
            return;
        }

        if (!_source.isPlaying)
            _source.Play();
    }
}
