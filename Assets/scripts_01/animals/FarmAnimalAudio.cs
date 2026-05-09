using UnityEngine;

/// <summary>
/// Sonidos de granja por animal: ambiente en loop y clip opcional al morir.
/// Asigna los <see cref="AudioClip"/> en el prefab o con el menú de editor Tools.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class FarmAnimalAudio : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip ambientLoopClip;
    [SerializeField] private AudioClip deathClip;

    [Header("Levels")]
    [SerializeField] [Range(0f, 1f)] private float ambientVolume = 0.15f;
    [SerializeField] [Range(0f, 1f)] private float deathVolume = 0.35f;

    private AnimalBase _animal;
    private AudioSource _source;

    private void Awake()
    {
        _animal = GetComponent<AnimalBase>();
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;

        TryBindResourcesFallback();

        if (_animal != null)
            _animal.Died += OnAnimalDied;

        if (ambientLoopClip != null)
        {
            _source.loop = true;
            _source.clip = ambientLoopClip;
            _source.volume = ambientVolume;
            _source.Play();
        }
    }

    private void OnDestroy()
    {
        if (_animal != null)
            _animal.Died -= OnAnimalDied;
    }

    private void OnAnimalDied()
    {
        if (_source != null)
        {
            _source.Stop();
            _source.loop = false;
            _source.clip = null;
        }

        if (deathClip != null)
            AudioSource.PlayClipAtPoint(deathClip, transform.position, deathVolume);
    }

    /// <summary>Carga clips desde Resources/AnimalSounds si el prefab no los tiene asignados.</summary>
    private void TryBindResourcesFallback()
    {
        if (!TryGetComponent<FarmAnimal>(out var farm))
            return;

        if (ambientLoopClip == null)
        {
            ambientLoopClip = farm.Kind switch
            {
                FarmAnimalKind.Cow => Resources.Load<AudioClip>("AnimalSounds/Cow"),
                FarmAnimalKind.Chicken => Resources.Load<AudioClip>("AnimalSounds/Chicken"),
                FarmAnimalKind.Pig => Resources.Load<AudioClip>("AnimalSounds/Pig"),
                _ => null
            };
        }

        if (farm.Kind == FarmAnimalKind.Chicken && deathClip == null)
            deathClip = Resources.Load<AudioClip>("AnimalSounds/ChickenDeath");
    }
}
