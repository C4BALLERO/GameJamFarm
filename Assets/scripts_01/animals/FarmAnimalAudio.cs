using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sonidos de granja (Chicken, Pig, vaca): cola global sin solapes, reproducción aleatoria espaciada.
/// ChickenDeath suena al morir la gallina. El bucle <c>walk</c> va en <see cref="PlayerWalkAudio"/>, no aquí.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class FarmAnimalAudio : MonoBehaviour
{
    [Header("Clips ambiente")]
    [Tooltip("Si hay varios, en cada turno se elige uno al azar. Si está vacío, se usa solo Ambient Loop Clip.")]
    [SerializeField] private AudioClip[] ambientClips;
    [Tooltip("Respaldo cuando Ambient Clips está vacío (compatibilidad con prefabs anteriores).")]
    [SerializeField] private AudioClip ambientLoopClip;
    [SerializeField] private AudioClip deathClip;

    [Header("Silencio global (tras cada clip, antes del siguiente en cualquier animal)")]
    [SerializeField] private float minSecondsBetweenAmbient = 2.6f;
    [SerializeField] private float maxSecondsBetweenAmbient = 4.2f;

    [Header("Pausa de este animal tras sonar (reparte turnos entre animales)")]
    [SerializeField] private float minSecondsPerAnimalCooldown = 3.8f;
    [SerializeField] private float maxSecondsPerAnimalCooldown = 8.5f;

    [Header("Levels")]
    [SerializeField] [Range(0f, 1f)] private float ambientVolume = 0.62f;
    [SerializeField] [Range(0f, 1f)] private float deathVolume = 0.5f;

    private AnimalBase _animal;
    private AudioSource _source;
    private AudioClip[] _resolvedAmbient;
    private Coroutine _ambientRoutine;
    private bool _ambientStopped;

    private void Awake()
    {
        _animal = GetComponent<AnimalBase>();
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;
        _source.volume = 1f;
        _source.priority = 90;
        _source.clip = null;

        TryBindResourcesFallback();
        _resolvedAmbient = ResolveAmbientPlaylist();

        if (_animal != null)
            _animal.Died += OnAnimalDied;
    }

    private void Start()
    {
        if (_resolvedAmbient.Length > 0)
            _ambientRoutine = StartCoroutine(AmbientSpacedRoutine());
    }

    private void OnDestroy()
    {
        if (_animal != null)
            _animal.Died -= OnAnimalDied;
    }

    private IEnumerator AmbientSpacedRoutine()
    {
        var gapMin = Mathf.Max(0.05f, minSecondsBetweenAmbient);
        var gapMax = Mathf.Max(gapMin, maxSecondsBetweenAmbient);
        var coolMin = Mathf.Max(0f, minSecondsPerAnimalCooldown);
        var coolMax = Mathf.Max(coolMin, maxSecondsPerAnimalCooldown);

        while (!_ambientStopped && isActiveAndEnabled)
        {
            var clip = _resolvedAmbient[Random.Range(0, _resolvedAmbient.Length)];
            if (clip == null)
            {
                yield return null;
                continue;
            }

            var playAt = FarmAnimalAmbientGate.ReserveNextPlayTime(clip.length, gapMin, gapMax);
            yield return new WaitUntil(() => !_ambientStopped && Time.time >= playAt);
            if (_ambientStopped || !isActiveAndEnabled)
                yield break;

            _source.PlayOneShot(clip, ambientVolume);
            yield return new WaitForSeconds(Random.Range(coolMin, coolMax));
        }
    }

    private AudioClip[] ResolveAmbientPlaylist()
    {
        if (ambientClips != null && ambientClips.Length > 0)
        {
            var list = new List<AudioClip>();
            foreach (var c in ambientClips)
            {
                if (c != null)
                    list.Add(c);
            }

            if (list.Count > 0)
                return list.ToArray();
        }

        if (ambientLoopClip != null)
            return new[] { ambientLoopClip };

        return System.Array.Empty<AudioClip>();
    }

    private void OnAnimalDied()
    {
        _ambientStopped = true;
        if (_ambientRoutine != null)
        {
            StopCoroutine(_ambientRoutine);
            _ambientRoutine = null;
        }

        if (_source != null)
        {
            _source.Stop();
            _source.loop = false;
            _source.clip = null;
        }

        if (deathClip != null && _source != null)
            _source.PlayOneShot(deathClip, deathVolume);
    }

    /// <summary>Carga clips desde Resources/AnimalSounds si el prefab no los tiene asignados.</summary>
    private void TryBindResourcesFallback()
    {
        if (!TryGetComponent<FarmAnimal>(out var farm))
            return;

        if (ambientLoopClip == null && (ambientClips == null || ambientClips.Length == 0))
        {
            ambientLoopClip = farm.Kind switch
            {
                FarmAnimalKind.Cow => LoadVacaAmbientClip(),
                FarmAnimalKind.Chicken => Resources.Load<AudioClip>("AnimalSounds/Chicken"),
                FarmAnimalKind.Pig => Resources.Load<AudioClip>("AnimalSounds/Pig"),
                _ => null
            };
        }

        if (farm.Kind == FarmAnimalKind.Chicken && deathClip == null)
            deathClip = Resources.Load<AudioClip>("AnimalSounds/ChickenDeath");
    }

    private static AudioClip LoadVacaAmbientClip()
    {
        var clip = Resources.Load<AudioClip>("AnimalSounds/vaca");
        if (clip != null)
            return clip;

        clip = Resources.Load<AudioClip>("AnimalSounds/Vaca");
        if (clip != null)
            return clip;

#if UNITY_EDITOR
        clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/resources/AnimalSounds/vaca.mp3");
        if (clip != null)
            return clip;

        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/resources_08/Caminando.mp3");
#else
        return null;
#endif
    }
}
