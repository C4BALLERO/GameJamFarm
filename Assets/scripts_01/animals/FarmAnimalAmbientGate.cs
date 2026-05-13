using UnityEngine;

/// <summary>
/// Una sola línea de tiempo para sonidos ambiente de granja: evita que varios animales
/// se superpongan. Reserva el instante de reproducción y el silencio posterior.
/// </summary>
public static class FarmAnimalAmbientGate
{
    private static readonly object LockObj = new object();
    private static float s_earliestNextPlayTime = -999f;

    /// <summary>
    /// Reserva cuándo puede sonar el siguiente clip (en tiempo de juego) y avanza la cola global.
    /// </summary>
    /// <param name="clipDuration">Duración del clip que va a sonar.</param>
    /// <param name="silenceAfterClipMin">Silencio mínimo tras terminar el clip, antes del siguiente sonido (cualquier animal).</param>
    /// <param name="silenceAfterClipMax">Silencio máximo tras el clip.</param>
    public static float ReserveNextPlayTime(float clipDuration, float silenceAfterClipMin, float silenceAfterClipMax)
    {
        var duration = Mathf.Max(0.01f, clipDuration);
        var gapMin = Mathf.Max(0.05f, silenceAfterClipMin);
        var gapMax = Mathf.Max(gapMin, silenceAfterClipMax);

        lock (LockObj)
        {
            var now = Time.time;
            var playAt = Mathf.Max(now, s_earliestNextPlayTime);
            var silence = Random.Range(gapMin, gapMax);
            s_earliestNextPlayTime = playAt + duration + silence;
            return playAt;
        }
    }
}
