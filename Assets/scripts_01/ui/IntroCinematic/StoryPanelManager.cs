using UnityEngine;

/// <summary>
/// Datos de los tres beats narrativos (rutas Resources para ilustración + texto).
/// Puedes editar los textos en el Inspector sin tocar código.
/// </summary>
[DisallowMultipleComponent]
public sealed class StoryPanelManager : MonoBehaviour
{
    [System.Serializable]
    public sealed class StoryBeat
    {
        [Tooltip("Sin extensión, carpeta Resources. Ej: panel1")]
        public string illustrationResourcePath = "panel1";

        [TextArea(5, 18)]
        public string narrative = string.Empty;
    }

    [SerializeField] private StoryBeat[] beats = CreateTemplateBeats();

    public StoryBeat[] Beats => beats != null && beats.Length > 0 ? beats : CreateTemplateBeats();

    /// <summary>Plantilla por defecto (tres viñetas) para el Inspector y para secuencias generadas en runtime.</summary>
    public static StoryBeat[] CreateTemplateBeats()
    {
        return new[]
        {
            new StoryBeat
            {
                illustrationResourcePath = "panel1",
                narrative =
                    "En un rincón olvidado del mundo, una granja maldita aguarda entre nieblas eternas.\n\n" +
                    "El aislamiento lo devora todo: campos silenciosos, noches que no acaban, y un silencio demasiado denso.\n\n" +
                    "Tú —un granjero solitario— has llegado aquí sin saber si fue elección… o condena."
            },
            new StoryBeat
            {
                illustrationResourcePath = "panel2",
                narrative =
                    "Las plantas ya no crecen: se retuercen, envenenan la tierra y despiertan con la luna.\n\n" +
                    "Cuando cae la noche, el peligro se arrastra hasta los muros: sombras con dientes, raíces como garras.\n\n" +
                    "Cada amanecer es un suspiro; cada ocaso, una promesa de asedio. Sobrevivir no es hábito… es instinto."
            },
            new StoryBeat
            {
                illustrationResourcePath = "panel3",
                narrative =
                    "Aún quedan animales: frágiles, hambrientos, pero vivos. Son carne, leche, plumas… y también compañía maldita.\n\n" +
                    "Protegerlos es defender el último hilo de la granja: alimentarlos, encerrarlos, alzar vallas contra la oscuridad.\n\n" +
                    "Si aguantas la noche, tal vez —solo tal vez— la granja te devuelva un amanecer digno de confiar."
            }
        };
    }
}
