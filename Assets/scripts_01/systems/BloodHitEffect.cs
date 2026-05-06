using UnityEngine;

/// <summary>
/// Emite partículas de sangre al recibir daño o morir.
/// Compatible con EnemyBase y AnimalBase; añadir al mismo GameObject.
/// </summary>
[DisallowMultipleComponent]
public sealed class BloodHitEffect : MonoBehaviour
{
    private ParticleSystem _ps;
    private EnemyBase  _enemy;
    private AnimalBase _animal;

    private void Awake()
    {
        BuildParticleSystem();
    }

    private void Start()
    {
        _enemy  = GetComponent<EnemyBase>();
        _animal = GetComponent<AnimalBase>();

        if (_enemy != null)
        {
            _enemy.Damaged += OnHit;
            _enemy.Died    += OnDied;
        }
        if (_animal != null)
        {
            _animal.Damaged += OnHit;
            _animal.Died    += OnDied;
        }
    }

    private void OnDestroy()
    {
        if (_enemy  != null) { _enemy.Damaged  -= OnHit; _enemy.Died  -= OnDied; }
        if (_animal != null) { _animal.Damaged -= OnHit; _animal.Died -= OnDied; }
    }

    private void OnHit()  => _ps?.Emit(6);
    private void OnDied() => _ps?.Emit(18);

    private void BuildParticleSystem()
    {
        var psGo = new GameObject("BloodFX");
        psGo.transform.SetParent(transform, false);
        _ps = psGo.AddComponent<ParticleSystem>();

        var main = _ps.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                   new Color(0.78f, 0.04f, 0.04f, 1f),
                                   new Color(0.45f, 0.01f, 0.01f, 1f));
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 5.5f);
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.8f);
        main.maxParticles    = 80;

        var emission = _ps.emission;
        emission.enabled = false;

        var shape = _ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.15f;

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 15;
        renderer.renderMode   = ParticleSystemRenderMode.Billboard;

        _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
