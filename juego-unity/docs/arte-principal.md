# Recursos principales

Menú: FondoMenu.png (horizontal) y FondoDefensaMovil.png (vertical), seleccionados por MenuLayout sin deformar la imagen.
Partida: FondoJuego.png (1024x512), Puerta.png (80x72, transparente) y SueloPiedra.png (960x40), en Assets/Sprites/Juego. Conservan GUID y dimensiones originales.
Velas flotantes y niebla desactivadas en ambas escenas. Se conservan las llamas de antorchas.
Se eliminaron seis variantes descartadas y sus metadatos tras comprobar sus referencias.

Imágenes rediseñadas con ImageGen integrado; preparación a tamaño de importación con vecino más próximo. Se retiraron los márgenes vacíos del suelo generado.

## Prompts

### FondoJuego.png

Redesign the supplied forest game background as restrained crafted retro pixel art, simple deliberate square clusters and muted limited navy slate moss palette matching a 16-bit castle defense game. Keep wide 2:1 composition and central path from distant upper center to bottom center, open playable field across middle, dark pine trees at side edges, distant mountains, small moon and sparse clouds. Crisp clear night air: ABSOLUTELY NO FOG NO MIST NO HAZE NO CANDLES NO FLOATING LIGHTS. No characters monsters castle walls doors UI text or buttons. The empty playable ground must be clearly readable, gently varied dark grass with occasional small stones, central dirt and gravel path subtle, stronger foreground detail, avoid repetitive noisy texture. Flat restrained shading, no glossy AI fantasy painting. Keep original functional layout.

### Puerta.png

Redesign this isolated game door sprite. ONE closed double oak castle gate within a thick blue slate rounded stone arch. Straight front orthographic view, perfectly flat bottom, symmetrical structure, iron horizontal braces, small restrained brass handles. Muted brown wood, cool desaturated blue gray stones, simple clean 16-bit hand-pixelled game art with large intentional pixel clusters and 2-3 shades per material, no smooth rendering or excessive details. Fill canvas tightly, entire arch and gate visible. Width to height roughly 80:72. TRUE TRANSPARENT BACKGROUND outside the arch, no surrounding wall, no floor, no shadows outside silhouette, no torches, no fog, no candles, no text or UI. Match a moonlit navy castle defense game.

### SueloPiedra.png

Redesign this game floor strip as a seamless horizontal band of moonlit slate cobblestone paving. VERY WIDE AND SHORT 24:1 aspect ratio, full image filled edge to edge with only paving stones. Four staggered rows of broad irregular rectangular slabs in muted navy blue gray, sparse moss in joints, simple deliberate square pixel clusters and limited 16-bit palette, no noisy bevels or glossy rendering. Flat orthographic game texture, no horizon or objects, no border, no perspective vanishing point, no fog no candles no UI no text. Tileable left and right edges, consistent lighting. Match restrained retro castle defense game.


## Validación

Unity 6000.3.24f1: 18 pruebas PlayMode superadas, 0 fallos (MainMenuTests y GameLayoutTests). Referencias locales y duplicados de ambas escenas comprobados. Resultado: Temp/ArtUpdateTests.xml.
