# Ayuda y ajustes de sonido

## Abrir el proyecto

Descomprime el ZIP y añade la carpeta `juego-unity` con **Add project from disk** en Unity Hub.
Ábrela con Unity **6000.3.24f1**, espera a que se importen los recursos y abre
`Assets/Scenes/MenuPrincipal.unity`. Pulsa **Play** para acceder a Ayuda y Sonido.
La interfaz se construye al entrar en Play a partir del MainMenu y del botón ya referenciados en la escena.

## Archivos

- Nuevo: `Assets/Scripts/UI/MenuExtras.cs` y su `.meta`.
- Menú: `MainMenu.cs` conecta la navegación y bloquea el deslizamiento mientras está abierto el panel.
  `MenuLayout.cs` reserva el alto necesario dentro del área segura.
- Audio: `MusicPlayer.cs` amplía el controlador existente con preferencias, efectos y fundidos.
- Eventos: `AnswerPanel.cs`, `EnemySpawner.cs`, `Enemy.cs`, `Door.cs` y `LevelManager.cs`.

## Audio

Una instancia persistente conserva una fuente de música, una de efectos generales y otra
para los golpes de puerta. El menú y el mapa comparten la pista original, y el combate usa
su propia pista. Al cambiar entre ellas, el controlador baja el volumen, cambia la pista
y vuelve a subirlo. El volumen general se multiplica por el de cada canal. El silencio
conserva los tres niveles. Los ajustes se guardan y restauran mediante PlayerPrefs.

Los tres WAV originales de impacto siguen activos y conservan la variación de tono y el
intervalo mínimo entre sonidos. Los siguientes eventos están conectados y esperan clips.
Para activarlos, coloca un WAV u OGG con el nombre indicado en `Assets/Resources/Sonidos/Eventos`.

| Clip | Cuándo suena |
| --- | --- |
| Button | Al pulsar un botón |
| Correct | Al enviar una respuesta correcta |
| Incorrect | Al enviar una respuesta incorrecta |
| EnemyDefeated | Al eliminar un enemigo |
| EnemyAttack | Cuando el enemigo comienza a atacar la puerta |
| Victory | Al superar un nivel o ganar la partida |
| Defeat | Cuando cae la puerta |
| BossSpawn | Al aparecer el jefe |

Si falta un clip, su evento no reproduce sonido.

## Validación

Se compiló el código del juego con Roslyn de Unity 6000.3.24f1 y las referencias
originales del proyecto. Se verificó que las escenas, recursos, niveles y metadatos
originales siguen en el paquete, y se revisaron las conexiones con los eventos de juego.
El editor en modo batch se cerró tras agotar el tiempo de inicialización de la licencia.

Pendiente en modo Play:

1. Abrir y cerrar Ayuda y Sonido. Revisar los tres idiomas y el encaje en formato horizontal y vertical.
2. Pasar de menú a mapa y a combate. Escuchar ambas pistas, el fundido y el golpe de la puerta.
3. Probar cada deslizador y el silencio. Cerrar el juego y volver a abrirlo para comprobar la persistencia.
4. Jugar los cinco niveles. Verificar que resistir el tiempo completa el nivel y que derrotar al jefe da la victoria final.
5. Probar cada efecto opcional tras añadir su clip.
