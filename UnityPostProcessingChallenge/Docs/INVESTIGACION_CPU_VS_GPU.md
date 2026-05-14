# Investigacion: PDI en CPU (WPF/Bitmap) vs GPU (Unity/Shaders)

## CPU (WPF con Bitmap)

En un enfoque tradicional de `Bitmap`, el procesamiento suele recorrer pixeles con bucles en C#:

- Se lee cada pixel (`GetPixel` o buffer bloqueado).
- Se calcula el nuevo valor (gris, negativo, etc.).
- Se escribe de vuelta (`SetPixel` o buffer).

### Ventajas

- Logica muy clara y facil de depurar paso a paso.
- Buena para prototipos pequenos o imagenes estaticas.
- Menor curva de entrada para quienes vienen de programacion imperativa.

### Desventajas

- Costoso en tiempo para resoluciones altas y tiempo real.
- CPU trabaja secuencialmente (aunque se puede paralelizar, sigue siendo mas limitada).
- Transferencias de memoria pueden ser cuello de botella.
- Menos adecuado para video en vivo y escenas 3D con muchos FPS.

## GPU (Unity Shaders)

En Unity, la imagen final de camara se procesa en un shader fragmento:

- `OnRenderImage` entrega la textura renderizada.
- `Graphics.Blit` aplica un material de pantalla completa.
- Cada fragment shader calcula el color de su pixel en paralelo masivo.

### Ventajas

- Altisimo paralelismo: miles de nucleos procesan pixeles simultaneamente.
- Excelente para tiempo real, video, webcam y juegos.
- Menor carga de CPU para operaciones por pixel.
- Muy eficiente para filtros de post-proceso y convoluciones.

### Desventajas

- Programacion mas tecnica (HLSL/ShaderLab, precision, sampling).
- Depuracion menos directa que un bucle en C#.
- Algunas operaciones complejas requieren varias pasadas (multi-pass).

## Comparacion directa para este desafio

- Escala de grises, inversion y sepia: en GPU son operaciones naturales de 1 pasada.
- Pixelado: en GPU se logra con cuantizacion de UV, sin tocar buffers en CPU.
- Bordes Sobel: en GPU se toman vecinos con offsets de texel de forma muy eficiente.
- Resultado general: para feed de camara en tiempo real, GPU es la opcion correcta.

## Conclusiones para exposicion

1. CPU es didactica y simple, pero no escala bien a tiempo real.
2. GPU esta disenada para operaciones por pixel y mantiene FPS altos.
3. Unity + shaders permite filtros en caliente con parametros dinamicos sin reescribir datos en RAM.
