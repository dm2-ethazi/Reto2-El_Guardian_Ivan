# El Guardián Iván (juego en Unity)

Minijuego de cifrado César para móvil y PC. Los enemigos avanzan hacia la puerta del castillo con un mensaje cifrado encima; para derrotarlos hay que descifrar el mensaje y escribirlo antes de que rompan la puerta.

## Cómo se juega

- **Objetivo**: el enemigo más cercano a la puerta queda marcado con un marco dorado y su mensaje cifrado aparece en la barra superior ("Objetivo").
- **Regla**: debajo de cada letra original aparece su letra cifrada. Con los botones **−** y **+** (o las flechas ↑ y ↓ del teclado) se cambia el desplazamiento hasta encontrar el del nivel. La regla es solo una ayuda: no cambia el nivel.
- **Respuesta**: se escribe con el abecedario QWERTY en pantalla (con la Ñ, espacio y borrar) y se envía con **Descifrar**. En PC también se puede usar el teclado físico (letras, Retroceso e Intro). No distingue mayúsculas ni tiene en cuenta los espacios de los extremos.
- **Niveles**: hay 5. Cada uno dura un tiempo fijo; si la puerta aguanta, se pasa al siguiente. En el nivel 5 sale un **jefe** (dragón) con barra de vida que muere con **4 aciertos**. Cada acierto le cambia el mensaje.
- **Partida guardada**: se guarda el nivel al empezarlo. En el menú aparece **Continuar (nivel N)**. Al ganar el juego se borra.
- **Gestos**: en el menú, desliza a la **izquierda** para entrar en el nivel (el guardado o el 1). En la partida, desliza a la **derecha** para volver al menú. Mientras arrastras, la pantalla sigue al dedo y aparece un panel con el destino, que se pone dorado cuando al soltar se completará el gesto. En PC funciona arrastrando con el ratón.
- **Idiomas**: español, inglés y euskera (botones del menú). El idioma elegido se recuerda.
- La pantalla gira sola: en vertical y en horizontal la interfaz se recoloca para que todo quepa.

## Requisitos

- **Unity 6000.3.24f1** (la versión está en `ProjectSettings/ProjectVersion.txt`). Ábrelo con esa versión desde Unity Hub para no tener que convertir el proyecto.
- Módulos de Unity Hub (Installs → ⚙ → Add modules):
  - **Android Build Support**, con **OpenJDK** y **Android SDK & NDK Tools**, para compilar el APK.
  - **Windows Build Support** (viene con el editor en Windows) para compilar para PC.
- Paquetes principales (se instalan solos al abrir el proyecto, ver `Packages/manifest.json`): Universal Render Pipeline 17.3, Input System 1.20, TextMesh Pro (uGUI 2.0) y Test Framework 1.6.

## Abrir el proyecto

1. En Unity Hub: **Add → Add project from disk** y elige la carpeta `juego-unity`.
2. La primera vez tarda unos minutos en importar (crea la carpeta `Library`, que no va al repositorio).
3. Escenas, en `Assets/Scenes` y en este orden en **File → Build Profiles** (Scene List):
   1. `MenuPrincipal`: menú inicial (tiene que ser la primera).
   2. `PuertaDelCastillo`: la partida, con los 5 niveles.
4. Para probar, abre `MenuPrincipal` y pulsa **Play**.

## Compilar

### Windows (PC)

1. **File → Build Profiles** → **Windows** → si no está activa, **Switch Platform**.
2. Comprueba que las dos escenas están en la lista y marcadas.
3. **Build**, elige una carpeta vacía (por ejemplo `Builds/Windows`, que está en `.gitignore`).
4. Se genera `El Guardián Iván.exe` junto a su carpeta `_Data`. Hay que copiar las dos cosas juntas.

### Android

1. **File → Build Profiles** → **Android** → **Switch Platform**.
2. **Player Settings** (botón del mismo panel, o **Edit → Project Settings → Player**), pestaña Android. Ya está configurado:
   - **Product Name**: `El Guardián Iván` (nombre que se ve en el móvil).
   - **Icon**: el emblema del búho (`Assets/Sprites/Icono`), también como icono adaptativo (fondo azul noche y emblema delante).
   - **Package Name**: `com.DefaultCompany.urp_2d`. **No lo cambies** si ya hay una versión instalada: Android lo trataría como otra app.
   - **Orientation**: Auto Rotation (vertical y horizontal).
   - **Minimum API Level**: 25 (Android 7.1). **Scripting Backend**: IL2CPP. **Target Architectures**: ARM64.
3. **APK o AAB**: para instalar directamente en un móvil, deja **Build App Bundle (Google Play)** desmarcado y se genera un `.apk`. El `.aab` solo sirve para subirlo a Google Play.
4. Instalar en el móvil:
   - Activa en el móvil las **Opciones de desarrollador** y la **Depuración USB**, conéctalo por USB y pulsa **Build And Run**, o
   - pulsa **Build**, copia el `.apk` al móvil y ábrelo (permite instalar apps de origen desconocido).
5. **Keystore**: ahora se firma con la clave de depuración de Unity, suficiente para probar. Para publicar en Google Play crea una clave propia en **Player Settings → Publishing Settings → Keystore Manager** y guarda el archivo y las contraseñas fuera del repositorio. Si la pierdes, no podrás actualizar la app publicada.
6. Antes de cada versión nueva, sube **Version** (`1.0`) y **Bundle Version Code** (`1`) en Player Settings → Other Settings.

## Modificar el juego desde Unity

### Niveles (`Assets/Levels/Level1.asset` … `Level5.asset`)

Selecciona el archivo y cámbialo en el **Inspector** (cada campo tiene una explicación al pasar el ratón):

| Campo | Qué hace |
|---|---|
| **Min Enemies** / **Max Enemies** | Enemigos que siempre hay y máximo a la vez. |
| **Spawn Interval** | Segundos entre un enemigo y el siguiente. |
| **Seconds Between Hits** | Segundos entre golpe y golpe de un enemigo en la puerta. |
| **Duration Seconds** | Tiempo que hay que resistir para superar el nivel. |
| **Caesar Shift** | Desplazamiento César de los mensajes (1 a 26). |
| **Enemies** | Tipos de enemigo del nivel. En cada uno: **Prefab** (`Enemy`, `EnemyFast`), **Speed** (velocidad) y **Damage Per Hit** (vida que quita por golpe). |
| **Boss** | Jefe que sale al empezar. Sin **Prefab**, el nivel no tiene jefe (solo el 5 lo tiene: `EnemyBoss`). |
| **Hint Key** | Clave de la pista en `Localization.cs` (por ejemplo `hint.level1`). |
| **Messages** | Mensajes que pueden salir: en **mayúsculas**, sin tildes ni Ñ, y más que **Max Enemies** para que no se repitan en pantalla. |

Valores actuales: desplazamientos 3, 5, 7, 11 y 13; duración 60, 70, 80, 90 y 180 s.

Si añades un nivel, crea el archivo con **clic derecho → Create → Puerta del Castillo → Nivel** y añádelo a la lista **Levels** del objeto `LevelManager` en la escena `PuertaDelCastillo`.

### Textos, pistas e idiomas (`Assets/Scripts/UI/Localization.cs`)

Todos los textos están en la tabla `Texts`, con una clave y tres traducciones en orden **español, inglés, euskera**. Por ejemplo, `["menu.title"]` es el título del menú y `["hint.level3"]` la pista del nivel 3. Para cambiar un texto, cámbialo ahí. Si creas una clave nueva, ponla en los tres idiomas. Los textos fijos de la escena usan el componente **Localized Text** con su **Key**.

### Enemigos (`Assets/Prefabs`)

- `Enemy` (hombre lobo), `EnemyFast` (espectro) y `EnemyBoss` (dragón, variante de `Enemy`).
- **Decrypts To Kill** (componente Enemy): aciertos para derrotarlo. Es 1 en los normales y 4 en `EnemyBoss`.
- **Sprite Animator**: **Frames** (fotogramas de andar o volar, en `Assets/Sprites/Meshy/<enemigo>/`) y **Frames Per Second**.
- La velocidad y el daño no están en el prefab: se ponen en cada nivel.
- La animación de golpe a la puerta se hace por código en `Enemy.cs` (`StrikeDistance`, `StrikeOut`, `StrikeBack`, `StrikeSquash`; el jefe usa valores más fuertes). El temblor de la puerta está en el componente **Door** (**Shake Amplitude**, **Shake Duration**, **Hit Flash Color**) y su vida en **Max Health**.

### Música (`Assets/Resources/Musica`)

- `menu_musica.mp3` suena en bucle en el menú y `batalla_musica.mp3` en la partida. Al reiniciar el nivel no se corta.
- Para cambiar una canción, sustituye el archivo **con el mismo nombre** (`MusicPlayer.cs` busca `Musica/menu_musica` y `Musica/batalla_musica`). El volumen es la constante `Volume` (0.6) de `MusicPlayer.cs`.
- No hace falta ningún objeto en las escenas: el reproductor se crea solo al arrancar.

### Icono y nombre

**Edit → Project Settings → Player**: **Product Name** e iconos. Las imágenes están en `Assets/Sprites/Icono` (`IconoJuego.png`, y para el icono adaptativo de Android `IconoAdaptivoFondo.png` y `IconoAdaptivoFrente.png`). Para cambiarlo basta con sustituir esos PNG manteniendo el nombre.

### Gráficos

- Fondos, muralla, puerta, botones, teclas, iconos y paneles: `Assets/Sprites/Juego`. Para cambiar uno, sustituye el PNG **con el mismo nombre** y conserva su `.meta`, así no se pierde ninguna referencia.
- Colores: están en el Inspector de cada componente (por ejemplo **Label Color** y **Target Label Color** en Enemy; **Normal Color**, **Error Color** y **Key Text Color** en AnswerPanel; **Victory Color** y **Defeat Color** en LevelHud; los de las casillas en CaesarRuler). La paleta del juego es pergamino `#F2E8CF`, dorado `#D4A017`, azul noche `#1B1B3A`, tinta `#22223B` y rojo `#C0392B`.
- Efectos de fondo (velas, niebla, llamas): componente **Ambient Effects**.

### Colocación de la interfaz (`Assets/Scripts/UI/GameLayout.cs`)

La interfaz de la partida se coloca por código según el tamaño y la orientación de la pantalla. Las medidas son constantes al principio del archivo, en unidades lógicas (la pantalla de referencia mide 1080×1920 en vertical): `Margin`, `Gap`, `TouchSize` (botones), `KeyHeight` (teclas), `PortraitCellHeight` / `LandscapeCellHeight` (casillas de la regla), `ColumnRatio` (ancho de la columna derecha en horizontal), `WorldWidth` / `WorldHeight` (parte del escenario que siempre se ve), etc. El menú se coloca con `MenuLayout.cs`.

### Gestos (`Assets/Scripts/UI/SwipeNavigation.cs`)

Constantes: `MinWidthFraction` (0.35: recorrido para que cuente al soltar), `FlickWidthFraction` y `FlickMaxDuration` (gesto rápido corto) y `Resistance` (0.6: cuánto sigue la pantalla al dedo). La dirección de cada pantalla se elige en `MainMenu.cs` y `LevelHud.cs` (`swipe.Direction`).

### Partida guardada

Se guarda en **PlayerPrefs** con la clave `savedLevel` (0 es el nivel 1), y el idioma con `language`. Para borrarla en el editor: **Edit → Clear All PlayerPrefs**. En el móvil, borra los datos de la app.

## Tests

**Window → General → Test Runner**:

- **EditMode**: cifrado, niveles, traducciones y gestos. Son rápidos.
- **PlayMode**: cargan las escenas y prueban la partida (enemigos, puerta, respuesta, regla, jefe, menú, colocación de la interfaz).

Pulsa **Run All** en cada pestaña. Ojo: `LevelConfigTests` comprueba los valores de los niveles contra la propuesta técnica (desplazamientos, mínimos y máximos, intervalos, daño, enemigos más rápidos cada nivel, mensajes más cortos del nivel 1 al 4, jefe solo en el 5). Si cambias el equilibrio de los niveles a propósito, actualiza también ese test.

## Estructura

```
juego-unity/
├── Assets/
│   ├── Levels/        Configuración de los 5 niveles
│   ├── Prefabs/       Enemigos
│   ├── Resources/     Música (se carga por nombre)
│   ├── Scenes/        MenuPrincipal y PuertaDelCastillo
│   ├── Scripts/
│   │   ├── Crypto/    Cifrado César
│   │   ├── Gameplay/  Niveles, enemigos, puerta, partida guardada, música
│   │   └── UI/        Menú, interfaz de la partida, regla, teclado, gestos, textos
│   ├── Sprites/       Gráficos (Juego, Meshy con los enemigos, Icono)
│   ├── Fonts/         Cinzel e Inter (con sus licencias OFL)
│   └── Tests/         EditMode y PlayMode
├── Packages/          Paquetes de Unity
└── ProjectSettings/   Nombre, icono, Android, escenas…
```
