# Post-Processing Challenge en Unity (6000.3.3)

Este material deja implementados los 5 filtros solicitados usando `OnRenderImage` + `Graphics.Blit`.

## 1) Archivos incluidos

- `Assets/Scripts/PostProcessFilterController.cs`
- `Assets/Scripts/WebcamFeedToMaterial.cs` (opcional para webcam en un objeto)
- `Assets/Shaders/GrayscaleLuma.shader`
- `Assets/Shaders/Pixelation.shader`
- `Assets/Shaders/EdgeSobel.shader`
- `Assets/Shaders/Invert.shader`
- `Assets/Shaders/Sepia.shader`

## 2) Configuracion rapida en Unity

1. Crear proyecto 3D en Unity `6000.3.3` (Built-in Render Pipeline).
2. Copiar la carpeta `Assets` de este entregable dentro del proyecto.
3. (Recomendado) En Unity, ejecutar menu `Tools > PostFX Challenge > Setup Scene` para auto-crear materiales y asignarlos a `Main Camera`.
3. Crear 5 materiales y asignar a cada uno su shader:
   - `Custom/PostFX/GrayscaleLuma`
   - `Custom/PostFX/Pixelation`
   - `Custom/PostFX/EdgeSobel`
   - `Custom/PostFX/Invert`
   - `Custom/PostFX/Sepia`
4. Seleccionar la `Main Camera` y agregar componente `PostProcessFilterController`.
5. Arrastrar cada material a su campo correspondiente en el inspector.
6. Ejecutar escena y cambiar filtros con teclas:
   - `1`: Escala de grises (Luma)
   - `2`: Pixelado
   - `3`: Deteccion de bordes (Sobel)
   - `4`: Inversion de color
   - `5`: Sepia

> Nota: `OnRenderImage` funciona de forma directa en Built-in Render Pipeline.  
> Si usas URP, debes migrar a un `ScriptableRendererFeature` (Custom Post-Processing).

## 3) Parametros dinamicos solicitados

- `Pixel Size`: controla tamano de bloque para pixelado.
- `Edge Threshold`: umbral de deteccion de bordes.
- `Edge Intensity`: intensidad del gradiente Sobel.

Estos parametros estan expuestos en el inspector y se aplican en tiempo real.

## 4) Opcion webcam (si quieres usar feed real)

### Opcion A: Sobre un objeto 3D (plano/cuadricula)

1. Crear un `Plane` frente a la camara.
2. Agregar `WebcamFeedToMaterial` a un GameObject.
3. En `Target Renderer`, arrastrar el renderer del plane.
4. Ejecutar y el plane mostrara la webcam.
5. La camara seguira aplicando el post-proceso sobre toda la imagen final.

### Opcion B: Solo objetos 3D

No necesitas script de webcam. El post-proceso funciona sobre cualquier render de la camara.

## 5) Puntos clave para defender

- `OnRenderImage(RenderTexture src, RenderTexture dst)` recibe la imagen ya renderizada por la camara.
- `Graphics.Blit(src, dst, material)` dibuja pantalla completa aplicando el shader.
- Los shaders usan coordenadas `UV` para muestrear la textura.
- Sobel usa vecinos del pixel por offsets con `_MainTex_TexelSize`.
- El cambio de filtros se hace por teclado en caliente (`Alpha1` a `Alpha5`).

## 6) Exportar .zip para entrega

1. Cerrar Play Mode.
2. Guardar escena principal.
3. Verificar version en `Project Settings > Editor` o Unity Hub: `6000.3.3`.
4. Comprimir carpeta completa del proyecto (incluyendo `Assets`, `Packages`, `ProjectSettings`).
5. Nombre sugerido: `PostProcessingChallenge_ApellidoNombre_6000.3.3.zip`.
