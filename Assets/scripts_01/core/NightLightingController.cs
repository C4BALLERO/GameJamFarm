using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Refuerza la noche con luz lunar (contraste) y faroles; sin capa fullscreen de niebla/vignette.
/// </summary>
[DisallowMultipleComponent]
public sealed class NightLightingController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DayNightManager dayNight;
    [SerializeField] private Light2D moonLight;
    [SerializeField] private Light2D[] flickerLanterns;

    [Header("Luna (Light2D tipo Point / Free)")]
    [SerializeField] private float moonNightIntensity = 0.78f;
    [SerializeField] private float moonDayIntensity = 0f;
    [SerializeField] private Color moonNightColor = new(0.72f, 0.78f, 1f, 1f);
    [SerializeField] private float moonSmooth = 1.35f;
    [SerializeField] private float nightPulseAmplitude = 0.045f;
    [SerializeField] private float nightPulseHz = 0.22f;

    [Header("Faroles")]
    [SerializeField] private float flickerStrength = 0.12f;
    [SerializeField] private float flickerSpeed = 7.5f;

    private float _lanternBaseIntensity = 1f;

    private void Awake()
    {
        if (dayNight == null)
            dayNight = FindFirstObjectByType<DayNightManager>();
        CacheLanternBases();
    }

    private void OnEnable()
    {
        if (dayNight != null)
        {
            dayNight.OnNightStarted += OnNightStarted;
            dayNight.OnDayStarted += OnDayStarted;
        }
    }

    private void OnDisable()
    {
        if (dayNight != null)
        {
            dayNight.OnNightStarted -= OnNightStarted;
            dayNight.OnDayStarted -= OnDayStarted;
        }
    }

    private void Start()
    {
        if (dayNight == null)
            dayNight = FindFirstObjectByType<DayNightManager>();
        ApplyImmediateForCurrentPhase();
    }

    private void LateUpdate()
    {
        if (dayNight == null)
            dayNight = FindFirstObjectByType<DayNightManager>();
        if (dayNight == null)
            return;

        var night = dayNight.IsNight;

        if (moonLight != null)
        {
            var pulse = night ? Mathf.Sin(Time.time * (Mathf.PI * 2f) * nightPulseHz) * nightPulseAmplitude : 0f;
            var target = (night ? moonNightIntensity : moonDayIntensity) + pulse;
            moonLight.intensity = Mathf.MoveTowards(moonLight.intensity, target, Mathf.Max(0.05f, moonSmooth) * Time.deltaTime);
            moonLight.color = Color.Lerp(moonLight.color, moonNightColor, night ? 0.12f : 0.15f);
        }

        FlickerLanterns();
    }

    private void OnNightStarted() => ApplyImmediateForCurrentPhase();

    private void OnDayStarted() => ApplyImmediateForCurrentPhase();

    private void ApplyImmediateForCurrentPhase()
    {
        if (dayNight == null)
            return;
        if (moonLight != null)
            moonLight.intensity = dayNight.IsNight ? moonNightIntensity * 0.9f : moonDayIntensity;
    }

    private void CacheLanternBases()
    {
        if (flickerLanterns == null || flickerLanterns.Length == 0)
            return;
        if (flickerLanterns[0] != null)
            _lanternBaseIntensity = Mathf.Max(0.05f, flickerLanterns[0].intensity);
    }

    private void FlickerLanterns()
    {
        if (flickerLanterns == null || dayNight == null || !dayNight.IsNight)
            return;

        for (var i = 0; i < flickerLanterns.Length; i++)
        {
            var L = flickerLanterns[i];
            if (L == null)
                continue;
            var n = Mathf.PerlinNoise(Time.time * flickerSpeed * 0.1f, i * 13.7f);
            var wobble = (n - 0.5f) * 2f * flickerStrength;
            L.intensity = Mathf.Max(0.02f, _lanternBaseIntensity * (1f + wobble));
        }
    }
}
