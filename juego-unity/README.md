# El Guardián Iván (juego en Unity)

Minijuego 2D de cifrado César para móvil (Android) y PC, también llamado **La Puerta del Castillo**. Los enemigos avanzan hacia la puerta del castillo con un mensaje cifrado encima. Para derrotarlos hay que descifrar el mensaje y escribirlo antes de que rompan la puerta. Hay cinco niveles, el último con un jefe, y un mapa para repetir los niveles ya superados.

## Índice

1. [Cómo se juega](#cómo-se-juega)
2. [Controles](#controles)
3. [Cifrado César con Ñ](#cifrado-césar-con-ñ)
4. [Niveles y jefe](#niveles-y-jefe)
5. [Mapa de niveles, progreso y partida guardada](#mapa-de-niveles-progreso-y-partida-guardada)
6. [Idiomas](#idiomas)
7. [Requisitos y cómo abrirlo](#requisitos-y-cómo-abrirlo)
8. [Compilar](#compilar)
9. [Estructura del proyecto](#estructura-del-proyecto)
10. [Escenas, prefabs y LevelConfig](#escenas-prefabs-y-levelconfig)
11. [Física 2D](#física-2d)
12. [Pruebas](#pruebas)
13. [Modificar el juego desde Unity](#modificar-el-juego-desde-unity)
14. [Diferencias con la propuesta técnica (apartado 10)](#diferencias-con-la-propuesta-técnica-apartado-10)
15. [Autores](#autores)

## Cómo se juega

- **Objetivo**: el enemigo más cercano a la puerta queda marcado con un marco dorado y su mensaje se pinta en dorado. Ese mismo mensaje cifrado aparece en la barra superior ("Objetivo: …"). La respuesta solo se compara con él.
- **Responder**: se escribe el mensaje descifrado y se pulsa **Descifrar** (o Intro). No distingue mayúsculas y no tiene en cuenta los espacios de los extremos. Si acierta, el enemigo muere (o el jefe pierde vida y cambia de mensaje) y el objetivo pasa al siguiente más cercano. Si falla, el campo se pone rojo, se vacía y sale "Incorrecto, inténtalo de nuevo". Un campo vacío no cuenta como fallo.
- **Puerta**: empieza cada nivel con 100 de vida. Un enemigo que llega a ella se para y la golpea cada cierto tiempo (según el nivel) mientras siga en contacto. Cada golpe hace temblar la puerta y suena a madera. Si llega a 0, se pierde el nivel.
- **Superar un nivel**: resistir hasta que acabe el tiempo. Sale el panel "Nivel N completado" con **Siguiente nivel**. En el nivel 5 se gana al acabar el tiempo o antes, si se derrota al jefe.
- **Paneles**: nivel completado (Siguiente nivel), victoria y derrota (Reiniciar y Menú). Los tres tienen **Volver al mapa**. Durante la partida también hay botones de **Reiniciar** (repite el nivel actual) y **Menú**.
- **Pista**: cada nivel muestra una pista (por ejemplo, "suma 3 a cada letra").
- **Música y sonido**: una pista en bucle para el menú y el mapa y otra para la batalla. Al reiniciar el nivel no se corta. Cada golpe a la puerta suena a madera.
- **Pantalla**: el juego gira solo. En vertical y en horizontal la interfaz se recoloca y respeta el área segura (muescas).

## Controles

| Acción | Pantalla táctil / ratón | Teclado físico (PC) |
|---|---|---|
| Escribir letras | Abecedario QWERTY en pantalla, con la **Ñ** (27 letras) | Letras, incluida la ñ (se pasan a mayúsculas) |
| Espacio | Tecla **Espacio** | Barra espaciadora |
| Borrar | Tecla **Borrar** | Retroceso |
| Enviar | Botón **Descifrar** | Intro (también el del teclado numérico) |
| Regla César: subir / bajar desplazamiento | Botones **+** y **−** | Flechas **↑** y **↓** |
| Ir al nivel desde el menú | Deslizar a la **izquierda** | Arrastrar con el ratón a la izquierda |
| Volver al menú desde la partida | Deslizar a la **derecha** | Arrastrar con el ratón a la derecha |
| Volver al menú desde el mapa | Botón **Menú** | Escape (o botón Atrás de Android) |

- El campo de respuesta es de solo lectura: así en Android no se abre el teclado del sistema y solo se usa el abecedario en pantalla.
- Las teclas no se quedan con el foco, para que Intro y las flechas sigan funcionando en el juego.
- Mientras se ve un panel de nivel, la respuesta y las teclas están bloqueadas.
- **Gestos**: mientras se arrastra, la pantalla sigue al dedo y entra por el borde un panel con el destino ("Nivel 2", "Menú"), que se pone dorado cuando, al soltar, se completará el gesto. Cuenta un gesto de al menos el 35 % del ancho, o uno rápido del 15 % en 0,3 s. Un gesto que empieza sobre un botón, una tecla o la regla no cuenta.

## Cifrado César con Ñ

`CaesarCipher` (`Assets/Scripts/Crypto/CaesarCipher.cs`) usa el abecedario español de **27 letras**: `ABCDEFGHIJKLMNÑOPQRSTUVWXYZ`.

- Cifrar avanza cada letra *n* posiciones y vuelve al principio al pasar de la Z (con desplazamiento 3, `Z → C` y `N → P`, porque la Ñ cuenta).
- Descifrar es cifrar con el desplazamiento contrario. Los desplazamientos negativos o mayores que 27 se normalizan.
- Lo que no es letra del abecedario (espacios, signos) no cambia.
- Los mensajes de los niveles van en mayúsculas, sin tildes ni Ñ. Los mensajes cifrados sí pueden tener Ñ.

**Regla de ayuda** (`CaesarRuler`): 27 casillas en dos bloques (A–N y, debajo, Ñ–Z), cada una con la letra original arriba y su cifrada debajo, en rojo. El desplazamiento (0 a 26) se cambia con + y − o con ↑ y ↓, y da la vuelta al pasar de 26 a 0. Es solo una ayuda: no cambia el nivel.

## Niveles y jefe

Valores actuales de `Assets/Levels/Level1.asset` … `Level5.asset`:

| Nivel | César | Enemigos (mín.–máx.) | Aparición | Golpe cada | Duración | Tipos de enemigo (velocidad, daño por golpe) | Pista |
|---|---|---|---|---|---|---|---|
| 1 | 3 | 1–2 | 10 s | 2 s | 60 s | Lobo `Enemy` (0,4 u/s, 2) | Suma 3 a cada letra |
| 2 | 5 | 1–5 | 18 s | 2 s | 90 s | Lobo (0,3 u/s, 2) | Suma 5 a cada letra |
| 3 | 7 | 2–6 | 20 s | 3 s | 120 s | Lobo (0,3 u/s, 2) y espectro `EnemyFast` (0,6 u/s, 1) | La A se cifra como H |
| 4 | 11 | 3–8 | 25 s | 3 s | 140 s | Lobo (0,3 u/s, 2) y espectro (0,6 u/s, 1) | La letra más repetida suele ser E o A |
| 5 | 13 | 1–1 | 3,5 s | 7 s | 180 s | Solo el jefe `EnemyBoss` (0,05 u/s, 15) | La A se cifra como N |

- Si hay menos enemigos que el mínimo, aparece uno al momento. Si no, aparece otro cada intervalo mientras no se llegue al máximo. Cada uno aparece en uno de los puntos de la parte de arriba (sin repetir hasta usarlos todos) y elige un mensaje que no tenga otro enemigo activo.
- Cada enemigo tiene una fila para su mensaje (las pares encima, las impares debajo), para que no se solapen al llegar a la puerta.
- Los mensajes son más largos en los primeros niveles (nivel 1: palabras de 7–8 letras; nivel 4: de 4–6) y el jefe tiene mensajes largos con espacios ("GRIMORIO DE CUATRO HOJAS").
- **Jefe** (nivel 5): un dragón con jinete que sale al empezar en el punto central, con barra de vida. Muere con **4 aciertos**, y cada acierto le quita un cuarto de la barra y le da un mensaje nuevo. Su golpe es más fuerte y la puerta tiembla más. Al derrotarlo se para el generador y se gana el juego, sin esperar al final de la cuenta atrás.

## Mapa de niveles, progreso y partida guardada

- **Iniciar partida** abre la escena `MapaNiveles`: una ilustración (`MapaProgresion.png`) con un botón encima de cada medallón. Cada nivel puede estar:
  - **Bloqueado**: oscurecido, con candado, no se puede pulsar.
  - **Disponible**: anillo de chispas doradas.
  - **Superado**: marca verde. Se puede repetir.
- Superar el nivel N desbloquea el N+1, y repetir un nivel anterior nunca quita progreso (`LevelProgress`, clave `completedLevels` de PlayerPrefs). Vencer al jefe marca todo como superado.
- **Partida guardada** (`SaveGame`, clave `savedLevel`, 0 = nivel 1): se guarda el nivel al empezarlo. En el menú aparece **Continuar (nivel N)** encima de Iniciar partida. Al ganar el juego se borra. Las partidas guardadas de antes de existir el mapa cuentan como progreso.
- El idioma se guarda con la clave `language`. Para borrarlo todo en el editor: **Edit → Clear All PlayerPrefs**. En el móvil, borra los datos de la app.

## Idiomas

Español, inglés y euskera, con los botones del menú. El idioma elegido se recuerda y se aplica en el momento a todos los textos visibles (menú, mapa, partida, teclas Espacio y Borrar, paneles y pistas). Los textos están en la tabla de `Localization.cs` (sin el paquete Localization de Unity). Los textos fijos de las escenas usan el componente `LocalizedText`. Los **mensajes cifrados siempre van en español**, porque forman parte del juego.

## Requisitos y cómo abrirlo

- **Unity 6000.3.24f1** (está en `ProjectSettings/ProjectVersion.txt`). Ábrelo con esa versión desde Unity Hub para no tener que convertir el proyecto.
- Módulos de Unity Hub (Installs → ⚙ → Add modules):
  - **Android Build Support**, con **OpenJDK** y **Android SDK & NDK Tools**, para compilar el APK.
  - **Windows Build Support** (viene con el editor en Windows) para compilar para PC.
- Paquetes principales (se instalan solos al abrir el proyecto, ver `Packages/manifest.json`): Universal Render Pipeline 17.3 (2D), Input System 1.20, uGUI 2.0 con TextMesh Pro y Test Framework 1.6.

Pasos:

1. En Unity Hub: **Add → Add project from disk** y elige la carpeta `juego-unity`.
2. La primera vez tarda unos minutos en importar (crea la carpeta `Library`, que no va al repositorio).
3. Abre `Assets/Scenes/MenuPrincipal.unity` y pulsa **Play**. También se puede abrir `PuertaDelCastillo` y darle a Play para empezar directamente en el nivel 1.

## Compilar

Escenas en **File → Build Profiles** (Scene List), en este orden:

1. `MenuPrincipal` (tiene que ser la primera).
2. `PuertaDelCastillo`.
3. `MapaNiveles`.

### Windows (PC)

1. **File → Build Profiles** → **Windows** → si no está activa, **Switch Platform**.
2. Comprueba que las tres escenas están en la lista y marcadas.
3. **Build**, elige una carpeta vacía (por ejemplo `Builds/Windows`, que está en `.gitignore`).
4. Se genera `El Guardián Iván.exe` junto a su carpeta `_Data`. Hay que copiar las dos cosas juntas.

### Android

1. **File → Build Profiles** → **Android** → **Switch Platform**.
2. **Player Settings** (botón del mismo panel, o **Edit → Project Settings → Player**), pestaña Android. Ya está configurado:
   - **Product Name**: `El Guardián Iván` (nombre que se ve en el móvil).
   - **Icon**: el emblema del búho (`Assets/Sprites/Icono`), también como icono adaptativo.
   - **Package Name**: `com.DefaultCompany.urp_2d`. **No lo cambies** si ya hay una versión instalada: Android lo trataría como otra app.
   - **Orientation**: Auto Rotation (vertical y horizontal).
   - **Minimum API Level**: 25 (Android 7.1). **Scripting Backend**: IL2CPP. **Target Architectures**: ARM64.
3. **APK o AAB**: para instalar directamente en un móvil, deja **Build App Bundle (Google Play)** desmarcado y se genera un `.apk`. El `.aab` solo sirve para subirlo a Google Play.
4. Instalar en el móvil:
   - Activa en el móvil las **Opciones de desarrollador** y la **Depuración USB**, conéctalo por USB y pulsa **Build And Run**, o
   - pulsa **Build**, copia el `.apk` al móvil y ábrelo (permite instalar apps de origen desconocido).
5. **Keystore**: ahora se firma con la clave de depuración de Unity, suficiente para probar. Para publicar en Google Play crea una clave propia en **Player Settings → Publishing Settings → Keystore Manager** y guarda el archivo y las contraseñas fuera del repositorio.
6. Antes de cada versión nueva, sube **Version** y **Bundle Version Code** en Player Settings → Other Settings.

## Estructura del proyecto

```
juego-unity/
├── Assets/
│   ├── Fonts/         Cinzel e Inter (con sus licencias OFL)
│   ├── Levels/        Level1..Level5.asset (LevelConfig de cada nivel)
│   ├── Prefabs/       Enemy, EnemyFast y EnemyBoss
│   ├── Resources/     Musica/ (menu_musica, batalla_musica) y Sonidos/ (GolpePuerta1..3), cargados por nombre
│   ├── Scenes/        MenuPrincipal, PuertaDelCastillo y MapaNiveles
│   ├── Scripts/       Código del juego (ensamblado PuertaCastillo)
│   │   ├── Crypto/    Cifrado César
│   │   ├── Gameplay/  Niveles, enemigos, puerta, progreso, partida guardada, música
│   │   └── UI/        Menú, mapa, interfaz de la partida, regla, teclado, gestos, textos
│   ├── Settings/      URP 2D, Input System y plantilla de escena
│   ├── Sprites/       Juego (fondos, muralla, puerta, botones, mapa), Meshy (enemigos), Icono, Provisional
│   └── Tests/         EditMode y PlayMode (ensamblados de pruebas)
├── Packages/          Paquetes de Unity
└── ProjectSettings/   Nombre, icono, Android, escenas, capas y física 2D
```

Todo el código está en el espacio de nombres `PuertaCastillo` (`.Crypto`, `.Gameplay`, `.UI` y `.Tests`) y comentado con `///` en español.

### Scripts

| Script | Carpeta | Responsabilidad |
|---|---|---|
| `CaesarCipher` | Crypto | Cifrado y descifrado César con el abecedario de 27 letras (con Ñ). |
| `Door` | Gameplay | Vida de la puerta, daño, aviso de caída (eventos `HealthChanged` y `Fell`), temblor, destello y sonido de golpe. |
| `Enemy` | Gameplay | Movimiento hacia la puerta con física, colisión y golpes, mensaje cifrado, marco de objetivo, vida del jefe y animación de embestida. |
| `EnemySpawner` | Gameplay | Crea enemigos y el jefe según el `LevelConfig`, con mensajes y filas sin repetir. Aplica los aciertos y avisa al derrotar al jefe. |
| `EnemyTargeting` | Gameplay | Elige como objetivo al enemigo más cercano a la puerta y comprueba las respuestas contra él. |
| `LevelConfig` / `EnemyStats` | Gameplay | ScriptableObject con los valores de un nivel y de cada tipo de enemigo en él. |
| `LevelManager` | Gameplay | Nivel actual, cuenta atrás, nivel completado, victoria y derrota. Guarda la partida y el progreso. |
| `LevelProgress` / `LevelState` | Gameplay | Niveles superados (desbloqueo en el mapa) en PlayerPrefs. |
| `SaveGame` | Gameplay | Último nivel jugado (botón Continuar) y nivel con el que debe empezar la partida. |
| `MusicPlayer` | Gameplay | Música de fondo en bucle que se crea sola y cambia según la escena. |
| `SpriteAnimator` | Gameplay | Animación por fotogramas de los enemigos. |
| `SpriteVisual` | Gameplay | Pasa el dibujo a un hijo para animarlo sin mover el collider ni el Rigidbody2D. |
| `AnswerPanel` | UI | Campo de respuesta, botón Descifrar, teclado físico, avisos de acierto o fallo, barra del objetivo y pista. |
| `LetterKeyboard` | UI | Teclado QWERTY en pantalla con Ñ, espacio y borrar. |
| `CaesarRuler` | UI | Regla de ayuda con las 27 casillas y el desplazamiento. |
| `DoorHealthBar` | UI | Barra y texto de la vida de la puerta. |
| `LevelHud` | UI | Nivel y tiempo, paneles de fin de nivel, botones de la partida (reiniciar, menú, mapa) y gesto para volver al menú. |
| `GameLayout` | UI | Coloca la interfaz de la partida en vertical u horizontal y encuadra la cámara y los puntos de aparición. |
| `MainMenu` | UI | Menú principal: iniciar (mapa), continuar, idiomas, salir con confirmación y gesto para entrar en el nivel. |
| `MenuLayout` | UI | Centra el menú en el área segura y elige el fondo según la orientación. |
| `LevelMap` | UI | Mapa de niveles con nodos bloqueados, disponibles o superados. |
| `Localization` / `Language` | UI | Textos en español, inglés y euskera, e idioma guardado. |
| `LocalizedText` | UI | Traduce un texto fijo de la escena y lo actualiza al cambiar de idioma. |
| `ScreenFit` | UI | Tamaño lógico de la interfaz (referencia 1080×1920 o 1920×1080). |
| `SwipeNavigation` / `Swipe` | UI | Gesto de pasar de página entre el menú y la partida. |
| `ButtonAnimation` | UI | Animación de "respirar" y crecer al pasar el ratón. Ahora no está en ninguna escena; `LetterKeyboard` y `LevelHud` la quitan de los botones que copian. |
| `AmbientEffects` | UI | Decorado animado: llamas de antorcha (y velas y niebla, ahora desactivadas). |

## Escenas, prefabs y LevelConfig

### Escenas (`Assets/Scenes`)

| Escena | Contenido |
|---|---|
| `MenuPrincipal` | Fondo, título, botones (Iniciar partida, Salir, ES/EN/EU), panel de confirmación de salida. Scripts `MainMenu` y `MenuLayout`. |
| `PuertaDelCastillo` | Escenario (fondo, muralla, puerta con `Door`), `EnemySpawner` con 5 puntos de aparición, `LevelManager` con los 5 niveles, `EnemyTargeting`, y el Canvas con `AnswerPanel`, `CaesarRuler`, `DoorHealthBar`, `LevelHud` y `GameLayout`. |
| `MapaNiveles` | Canvas con `LevelMap`, que dibuja todo el mapa por código al darle a Play. |

### Prefabs (`Assets/Prefabs`)

| Prefab | Enemigo | Aciertos para derrotarlo | Notas |
|---|---|---|---|
| `Enemy` | Hombre lobo | 1 | Prefab base: `Rigidbody2D`, `CircleCollider2D`, `Enemy`, `SpriteAnimator`, texto TextMeshPro del mensaje y marco de objetivo. |
| `EnemyFast` | Espectro | 1 | Más rápido y con menos daño (lo fija el nivel). Desde el nivel 3. |
| `EnemyBoss` | Dragón con jinete | 4 | Variante de `Enemy` con barra de vida (`healthFill`). Solo en el nivel 5. |

La velocidad y el daño no están en el prefab: los pone cada nivel.

### LevelConfig

`LevelConfig` es un **ScriptableObject** (menú **Create → Puerta del Castillo → Nivel**). Cada nivel es un archivo de `Assets/Levels` que se edita en el Inspector, sin tocar código. Campos: `minEnemies`, `maxEnemies`, `spawnInterval`, `secondsBetweenHits`, `durationSeconds`, `caesarShift` (1–26), `enemies` (lista de `EnemyStats`: `prefab`, `speed`, `damagePerHit`), `boss` (sin prefab, el nivel no tiene jefe), `hintKey` y `messages`. `LevelManager` tiene la lista de niveles en orden. El último es el del jefe y su final es la victoria.

## Física 2D

- **Enemigos**: `Rigidbody2D` **Dynamic**, **Gravity Scale 0**, rotación Z congelada y **Sleeping Mode: Never Sleep** (dormido dejaría de recibir `OnCollisionStay2D`), con un `CircleCollider2D` **sólido** (no trigger). Se mueven en `FixedUpdate` asignando `linearVelocity` hacia la puerta.
- **Puerta**: solo un `BoxCollider2D` sólido, **sin Rigidbody2D**. El Rigidbody2D del enemigo basta para que Unity detecte el choque y el enemigo quede parado contra ella.
- **Capa `Enemy`** (capa 8): en la matriz de colisiones de Physics 2D no choca consigo misma, así que los enemigos no se empujan entre ellos.
- **Eventos de colisión** en `Enemy.cs`:
  - `OnCollisionEnter2D` con la puerta: el enemigo se para (`IsAttacking`) y pone a cero su contador.
  - `OnCollisionStay2D`: suma `Time.fixedDeltaTime` y, cada `secondsBetweenHits` del nivel, llama a `Door.TakeDamage` con su `damagePerHit` y lanza la animación de golpe.
  - `OnCollisionExit2D`: si dejara de tocar la puerta, vuelve a avanzar.
  - El daño termina cuando el enemigo se destruye al descifrar su mensaje.
- Las animaciones (embestida del enemigo, temblor de la puerta) mueven solo un hijo visual (`SpriteVisual`), nunca el objeto con el collider.

## Pruebas

Usan el **Unity Test Framework** (NUnit). Hay dos ensamblados en `Assets/Tests`:

- **EditMode** (`PuertaCastillo.Tests.EditMode`), rápidas y sin escenas: `CaesarCipherTests`, `LevelConfigTests`, `LevelProgressTests`, `LocalizationTests`, `SwipeNavigationTests`.
- **PlayMode** (`PuertaCastillo.Tests.PlayMode`), cargan las escenas y juegan: `AnswerTests`, `BossTests`, `CaesarRulerTests`, `DoorTests`, `EnemyAttackTests`, `EnemySpawnerTests`, `GameLayoutTests`, `LevelFlowTests`, `LevelMapTests`, `MainMenuTests`. Algunas aceleran el tiempo (`Time.timeScale`) y lo restauran al acabar.

**Desde el editor**: **Window → General → Test Runner**, pestaña **EditMode** o **PlayMode**, y **Run All**.

**Por línea de comandos** (con Unity cerrado, porque si el proyecto está abierto no se puede ejecutar en modo batch):

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults resultados-editmode.xml
```

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults resultados-playmode.xml
```

Las pruebas que miran la partida guardada, el progreso o el idioma guardan y restauran las PlayerPrefs.

> **Ojo:** `LevelConfigTests` comprueba los niveles contra las tablas de la propuesta técnica (desplazamientos, mínimos y máximos, intervalos, daño, enemigos más rápidos y mensajes más cortos en cada nivel). Los niveles se han reequilibrado después, así que si se cambian a propósito hay que actualizar también ese test (ver la última sección).

## Modificar el juego desde Unity

- **Niveles**: selecciona `Assets/Levels/LevelN.asset` y cámbialo en el Inspector (cada campo tiene una explicación al pasar el ratón). Los mensajes deben ir en mayúsculas, sin tildes ni Ñ, y debe haber **más mensajes que `maxEnemies`**. Para añadir un nivel, créalo con **Create → Puerta del Castillo → Nivel**, añádelo a la lista **Levels** del `LevelManager` en `PuertaDelCastillo` y sube **Level Count** del `LevelMap` (y una posición más en **Node Positions**).
- **Textos, pistas e idiomas**: tabla `Texts` de `Assets/Scripts/UI/Localization.cs`, con una clave y tres traducciones en orden español, inglés y euskera. Una clave nueva tiene que estar en los tres idiomas.
- **Enemigos**: **Decrypts To Kill** del componente Enemy (1 normales, 4 jefe). **Sprite Animator**: **Frames** (en `Assets/Sprites/Meshy/<enemigo>/`) y **Frames Per Second**. La fuerza de la embestida está en `Enemy.cs` (`StrikeDistance`, `StrikeOut`, `StrikeBack`, `StrikeSquash`). El temblor de la puerta y su vida están en el componente **Door**.
- **Música y sonidos**: sustituye `Assets/Resources/Musica/menu_musica.mp3` o `batalla_musica.mp3` por otro archivo **con el mismo nombre**. El volumen es la constante `Volume` (0.6) de `MusicPlayer.cs`. Los golpes son `Assets/Resources/Sonidos/GolpePuerta1..3.wav` (se cargan todos los de la carpeta).
- **Gráficos**: el arte es pixel art. Los fondos (`FondoMenu`, `FondoDefensaMovil`, `FondoJuego`), la puerta, el suelo, la muralla y los botones se generaron con ImageGen y se ajustaron a su tamaño con escalado de vecino más próximo. Los enemigos son sprites de Meshy (`Assets/Sprites/Meshy`). Para cambiar uno, sustituye el PNG **con el mismo nombre** y conserva su `.meta`. Los colores están en el Inspector de cada componente. Paleta: azul noche `#1B1B3A`, piedra `#4A4E69`, madera `#8B5A2B`, dorado `#D4A017`, pergamino `#F2E8CF`, tinta `#22223B`, rojo `#C0392B`, verde `#3C8D5A`.
- **Mapa**: componente **Level Map** de `MapaNiveles` (imagen horizontal y vertical, posición de cada nodo de 0 a 1 y tamaño de los medallones).
- **Colocación de la interfaz**: constantes al principio de `GameLayout.cs` (`Margin`, `Gap`, `TouchSize`, `KeyHeight`, `PortraitCellHeight`, `LandscapeCellHeight`, `ColumnRatio`, `WorldWidth`, `WorldHeight`…), en unidades lógicas de una pantalla de 1080×1920. El menú se coloca con `MenuLayout.cs`.
- **Gestos**: constantes `MinWidthFraction`, `FlickWidthFraction`, `FlickMaxDuration` y `Resistance` de `SwipeNavigation.cs`.
- **Icono y nombre**: **Edit → Project Settings → Player**. Imágenes en `Assets/Sprites/Icono`.

## Diferencias con la propuesta técnica (apartado 10)

La propuesta (`Propuesta_Tecnica_Reto02_v2.docx`, apartado 10 "Minijuego 2D Unity 6.3") describe una sola escena con cinco niveles. El juego final mantiene la mecánica (enemigos con mensajes César, objetivo más cercano, física con colisiones, `LevelConfig`, paneles de nivel completado, victoria y derrota), con estos cambios acordados durante el desarrollo:

| Tema | Propuesta | Juego |
|---|---|---|
| Escenas | Una escena, La Puerta del Castillo. | Tres: `MenuPrincipal`, `PuertaDelCastillo` y `MapaNiveles`. |
| Vida de la puerta entre niveles | Se conserva y se repara +20 al superar un nivel; vuelve a 100 solo al reiniciar. | Vuelve a 100 al empezar cada nivel. |
| Reiniciar | Vuelve al nivel 1. | Repite el nivel actual (en los paneles y en el botón de la partida). Para elegir otro nivel está el mapa. |
| Ritmo de golpes | Un golpe por segundo. | Cada `secondsBetweenHits` del nivel: 2, 2, 3, 3 y 7 s. |
| Física de la puerta | `BoxCollider2D` con `Rigidbody2D` Static. | Solo `BoxCollider2D` sólido; el Rigidbody2D del enemigo basta. |
| Enemigos | Un único prefab `Enemy`. | Tres prefabs: `Enemy` (lobo), `EnemyFast` (espectro, desde el nivel 3) y `EnemyBoss` (dragón, nivel 5). |
| Nivel 5 | 4–5 enemigos, 100 s, ganar resistiendo. | Solo el jefe, con barra de vida y 4 aciertos; 180 s. Se gana al acabar el tiempo o antes, si muere el jefe. |
| Tabla de niveles | Enemigos 1–2, 2–3, 3, 3–4, 4–5; 60–100 s; intervalo 6 a 3,5 s; velocidad 0,6 a 1,0 u/s; daño 2 a 6. | Ver [Niveles y jefe](#niveles-y-jefe): más enemigos y niveles más largos, enemigos más lentos (0,3–0,6 u/s; nivel 1 a 0,4) y zona de juego más grande. |
| Pistas | Visibles en los niveles 1–2 y ocultas en 3–5. | Hay pista en todos los niveles; en los últimos es menos directa. |
| Campo de respuesta | Campo de texto. | Campo de solo lectura con abecedario QWERTY en pantalla (con Ñ) y teclado físico en PC. |
| Añadidos | — | Menú con idiomas (es/en/eu), mapa de niveles con progreso, partida guardada (Continuar), regla César, gestos de deslizar, música y sonidos, botones de reiniciar y menú en la partida, interfaz adaptada a vertical y horizontal. |

Se mantiene igual que en la propuesta: objetivo = enemigo más cercano con marco dorado y mensaje en la barra superior; respuesta sin espacios sobrantes y en mayúsculas; acierto destruye al enemigo y fallo marca el campo en rojo; enemigos con `Rigidbody2D` Dynamic, gravedad 0, rotación congelada y `CircleCollider2D` sólido; capa de enemigos que no choca consigo misma; `OnCollisionEnter2D`, `OnCollisionStay2D` y `OnCollisionExit2D`; `LevelConfig` como ScriptableObject; mensajes sin repetir entre enemigos activos; desplazamientos 3, 5, 7, 11 y 13; paleta de colores.

## Autores

- Iván Salazar
- Iván Alonso
