# El Guardián Iván · Aplicación de escritorio (JavaFX)

Aplicación de escritorio del Reto 2 para **cifrar y descifrar archivos con contraseña**.
Funciona con cualquier tipo de archivo (textos, imágenes, documentos, vídeos...) y lo protege con
AES-256-GCM. Todo se hace en el propio equipo: la aplicación no se conecta a ningún servidor y la
contraseña no se guarda en ningún sitio.

Autores: **Iván Salazar** e **Iván Alonso**.

## Índice

1. [Funcionalidades](#funcionalidades)
2. [Requisitos](#requisitos)
3. [Compilar y ejecutar](#compilar-y-ejecutar)
4. [Cómo se usa](#cómo-se-usa)
5. [Cómo funciona por dentro](#cómo-funciona-por-dentro)
6. [Cómo funciona el cifrado](#cómo-funciona-el-cifrado)
7. [Registros (logs)](#registros-logs)
8. [Estructura del proyecto](#estructura-del-proyecto)
9. [Tests](#tests)
10. [Documentación del código (Javadoc)](#documentación-del-código-javadoc)
11. [Ejecutable con doble clic (fat jar y .exe)](#ejecutable-con-doble-clic-fat-jar-y-exe)
12. [Problemas frecuentes](#problemas-frecuentes)
13. [Autores](#autores)
14. [Créditos](#créditos)

## Funcionalidades

- **Cifrar** cualquier archivo y obtener un contenedor `.hgw`. El original no se borra.
- **Descifrar** un `.hgw` y recuperar el archivo original con su extensión.
- **Vista previa** al descifrar: si el resultado es una imagen o un texto plano, se muestra en la ventana de resultado.
- **Detección de errores**: si la contraseña es incorrecta o el `.hgw` está dañado o modificado, se avisa y no se genera ningún archivo.
- **Archivos grandes**: se cifran por trozos de 1 MB, así que la memoria usada no depende del tamaño del archivo.
- **Tres idiomas**: castellano, inglés y euskera. Se cambia en Ajustes y se recuerda para la próxima vez.
- **Accesibilidad**: atajos de teclado, pistas de teclado al pulsar Alt (como en Excel), foco visible, textos para lectores de pantalla y una interfaz que se adapta al tamaño de la ventana.
- **Ayuda** integrada con preguntas frecuentes, atajos y "Acerca de".
- **Registros (logs)** en consola y en fichero, sin datos sensibles.

## Requisitos

- **JDK 21** (por ejemplo Eclipse Temurin 21). Se comprueba con `java -version`.
- **No hace falta instalar Maven**: el proyecto incluye el wrapper (`mvnw` / `mvnw.cmd`), que descarga
  la versión correcta la primera vez. JavaFX, SLF4J, Logback y JUnit también los descarga Maven solo.
- Opcional: IntelliJ IDEA (Community o Ultimate).

## Compilar y ejecutar

Todos los comandos se lanzan desde la carpeta `desktop-javafx/El_Guardian_Ivan`.
En Windows se usa `.\mvnw.cmd` y en Linux o macOS `./mvnw`.

### Ejecutar la aplicación

```bash
# Windows
.\mvnw.cmd clean javafx:run

# Linux / macOS
./mvnw clean javafx:run
```

### Desde IntelliJ IDEA

1. **File > Open** y elegir la carpeta `desktop-javafx/El_Guardian_Ivan` (IntelliJ la reconoce como proyecto Maven).
2. En **File > Project Structure > Project**, elegir el JDK 21.
3. Para arrancarla: pestaña **Maven** > `El_Guardian_Ivan` > **Plugins** > **javafx** > **javafx:run**.
   También se puede lanzar `GuardianApp` con el botón ▶.
4. Para ver más detalle en los registros, añadir `-Dguardian.log.level=DEBUG` en
   **Run > Edit Configurations > Modify options > Add VM options**.

### Comandos más usados

| Comando | Qué hace |
|---|---|
| `mvnw clean compile` | Compila el código en `target/classes` |
| `mvnw clean javafx:run` | Compila y abre la aplicación |
| `mvnw test` | Ejecuta los tests |
| `mvnw javadoc:javadoc` | Genera la documentación del código |
| `mvnw -Pfatjar clean package` | Pasa los tests y genera el `.jar` único y el ejecutable (`.exe` en Windows) |
| `mvnw -Pfatjar clean package -DskipTests` | Lo mismo, sin pasar los tests (más rápido) |
| `mvnw -Pfatjar clean package -Djavafx.platform=linux -Djpackage.phase=none` | Desde Windows, genera el `.jar` para Linux (ver [más abajo](#generar-desde-windows-el-jar-para-linux-ubuntu)) |

## Cómo se usa

**Cifrar**

1. Ve a **Cifrar** y pulsa **Examinar...** para elegir el archivo.
2. Escribe una contraseña larga (el ojo sirve para ver lo que has escrito).
3. Revisa el nombre y la carpeta del archivo de salida y pulsa **Cifrar archivo**. Se crea un `.hgw`.

**Descifrar**

1. Ve a **Descifrar** y elige el archivo `.hgw`.
2. Escribe la misma contraseña que usaste al cifrar.
3. Pulsa **Descifrar archivo**. Se recupera el original y, si es una imagen o un texto, se ve en pantalla.

Si se olvida la contraseña, el archivo no se puede recuperar.

### Atajos de teclado

| Atajo | Acción |
|---|---|
| Ctrl+1 / 2 / 3 / 4 | Ir a Inicio, Cifrar, Descifrar y Ajustes |
| F1 | Abrir la ayuda |
| Ctrl+O | Elegir el archivo en Cifrar o Descifrar |
| Intro (en un campo de texto) | Cifrar o descifrar |
| Esc | Cerrar la ventana de aviso |
| Alt (pulsar y soltar) | Mostrar u ocultar las pistas de teclado (ver abajo) |

### Pistas de teclado con Alt (estilo Excel)

Al pulsar y soltar **Alt** sola, aparece una pequeña etiqueta con una letra o un número sobre cada
control de la pantalla actual. Pulsando esa tecla se activa el control, sin necesidad de ratón.
Se ocultan con **Alt** otra vez, con **Esc**, con un clic o con cualquier otra tecla.

| Tecla | Control | Dónde |
|---|---|---|
| 1 / 2 / 3 / 4 | Inicio, Cifrar, Descifrar, Ajustes | Menú lateral |
| A | Ayuda | Menú lateral |
| C / D | Tarjetas "Cifrar archivo" y "Descifrar archivo" | Inicio |
| O | Examinar... (elegir archivo) | Cifrar y Descifrar |
| U | Cambiar la ubicación de salida | Cifrar y Descifrar |
| P | Mostrar u ocultar la contraseña | Cifrar y Descifrar |
| R | Cifrar archivo / Descifrar archivo | Cifrar y Descifrar |
| L | Desplegable de idioma | Ajustes |

Solo salen las pistas de los controles que se ven y están activos. Alt se detecta **al soltarla**:
si se combina con otra tecla (Alt+Tab, Alt+F4...), no se muestran las pistas y esos atajos del
sistema siguen funcionando igual.

## Cómo funciona por dentro

La aplicación sigue el esquema habitual de JavaFX: cada pantalla es un fichero **FXML** (la vista) con
su **controlador** Java, y los estilos están en un único **CSS**.

1. **Arranque**. `Launcher.main()` arranca JavaFX con `GuardianApp`. `GuardianApp.start()` carga las
   fuentes, el CSS, los iconos y la ventana principal (`main-view.fxml` con `MainController`).
   `Launcher` existe aparte porque, desde un `.jar` único, Java no deja que la clase principal herede de `Application`.
2. **Navegación**. `MainController` tiene el menú lateral y una zona central. Cada pantalla de `View`
   (Inicio, Cifrar, Descifrar, Ajustes, Ayuda) se carga de su FXML la primera vez y se guarda, así no se pierde
   lo escrito al cambiar de sección. Las pantallas que necesitan cambiar de sección reciben el `Navigator`.
   `MainController` también registra los atajos (Ctrl+1..4, F1, Ctrl+O) y las pistas de Alt: una capa
   transparente (`keyTipsLayer` en `main-view.fxml`) encima de la interfaz donde se pintan las etiquetas.
3. **Escalado**. `ScalingPane` envuelve toda la interfaz y la amplía o reduce según el tamaño de la ventana.
4. **Idiomas**. Los textos están en `i18n/messages*.properties` y los FXML los usan con `%clave`.
   `I18n` guarda el idioma elegido con `Preferences`. Al cambiarlo en Ajustes, `GuardianApp` vuelve a cargar
   la ventana con los nuevos textos.
5. **Cifrar y descifrar**. `EncryptController` y `DecryptController` heredan de `FileOperationController`,
   que tiene los tres pasos comunes (fichero, contraseña y destino). Al pulsar el botón:
   - se comprueba que no falte nada y que el nombre de salida sea válido (`FileNameValidator`);
   - la contraseña se saca del campo como `char[]` y el campo se vacía;
   - `AesFileCipher` trabaja en un hilo aparte (`Task`) para que la ventana no se congele, y va moviendo la barra de progreso;
   - al terminar, la contraseña se borra de memoria y `ResultDialog` muestra el resultado (con vista previa
     gracias a `FilePreview`) o el error traducido a un mensaje comprensible.
6. **Registros**. Las clases escriben con SLF4J (`log.info(...)`) y Logback lo manda a consola y a fichero,
   enmascarando antes los datos sensibles (ver [Registros](#registros-logs)).

## Cómo funciona el cifrado

1. A partir de la contraseña se obtiene una clave de 256 bits con **PBKDF2-HMAC-SHA-256** (200 000 iteraciones)
   y una sal aleatoria nueva en cada cifrado. Así dos cifrados del mismo archivo con la misma contraseña dan resultados distintos.
2. El archivo se lee por **trozos de 1 MB** y cada trozo se cifra con **AES-256-GCM**, con su propio nonce
   (derivado del nonce base y del número de trozo).
3. Cada trozo lleva una **etiqueta de autenticación**. Al descifrar, si un solo byte se ha cambiado, falta un trozo
   o se han reordenado, la etiqueta no cuadra y la operación se cancela.
4. La extensión original va **cifrada dentro** del `.hgw`, para que al descifrar se recupere el tipo de archivo sin que se vea desde fuera.
5. El resultado se escribe primero en un archivo temporal y solo se publica cuando todo ha ido bien,
   así nunca queda un archivo a medias.

El formato `.hgw` está descrito byte a byte en [`docs/formato-hgw.md`](../../docs/formato-hgw.md),
para poder descifrarlo también desde otras plataformas (por ejemplo Android).

## Registros (logs)

La aplicación usa **SLF4J** en el código y **Logback** para escribir los mensajes
(configuración en `src/main/resources/logback.xml`):

- **Consola**: lo que se ve al ejecutar desde IntelliJ o con `mvnw javafx:run`.
- **Fichero**: `%LOCALAPPDATA%\ElGuardianIvan\logs\guardian.log` en Windows
  (en Linux, `~/.local/share/ElGuardianIvan/logs`). Se puede abrir escribiendo
  `%LOCALAPPDATA%\ElGuardianIvan\logs` en la barra del Explorador. Rota cada día y al llegar a 1 MB,
  guarda 14 días y nunca ocupa más de 20 MB.

Cada línea lleva fecha, nivel, hilo y clase:

```
2026-11-08 21:59:27.517 INFO  [JavaFX Application Thread] GuardianApp - Iniciando El Guardián Iván (Java 21.0.6, Windows 11 amd64, idioma es)
2026-11-08 22:01:10.204 INFO  [JavaFX Application Thread] EncryptController - Operación iniciada con fichero .png de 1.5 MB
2026-11-08 22:01:10.611 INFO  [JavaFX Application Thread] EncryptController - Operación terminada en 407 ms: fichero .hgw de 1.5 MB
2026-11-08 22:01:31.020 WARN  [JavaFX Application Thread] DecryptController - Operación rechazada tras 380 ms: contraseña incorrecta o fichero alterado
```

**Niveles**: `ERROR` (algo ha fallado), `WARN` (algo raro pero controlado, como una contraseña incorrecta),
`INFO` (lo normal: arranque, operaciones) y `DEBUG` (detalle para depurar). Por defecto se escribe desde `INFO`;
las bibliotecas de terceros solo escriben desde `WARN`.

**Qué se registra**: arranque y cierre, cambio de idioma, inicio y resultado de cada cifrado o descifrado
(tipo de fichero, tamaño y duración), contraseñas incorrectas y errores con su traza. En nivel `DEBUG`, además,
la pantalla que se abre y el tipo de fichero elegido.

**Qué no se registra nunca**: la contraseña, el contenido de los ficheros (ni la vista previa), la clave,
la sal o el nonce, y el nombre o la ruta de los ficheros. Además, todo pasa por `SensitiveDataMasker` antes de
escribirse (`%maskedMsg` y `%maskedEx` en `logback.xml`), por si algún mensaje o excepción llevara algo:

| Dato | Se escribe como |
|------|-----------------|
| Carpeta del usuario (`C:\Users\ivan\...`) | `~\...` |
| Nombre de usuario de Windows | `<usuario>` |
| `contraseña=...`, `password: ...`, `clave=...`, `token=...` | `contraseña=****` |
| Cadenas hexadecimales o Base64 largas (claves, sales) | `[oculto]` |

**Más detalle (DEBUG)**: arrancar con la opción de la máquina virtual `-Dguardian.log.level=DEBUG`
(en IntelliJ: *Run > Edit Configurations > Modify options > Add VM options*).

En los tests se usa `src/test/resources/logback-test.xml`, que solo escribe avisos en consola y no crea ficheros.

## Estructura del proyecto

```
src/main/java/el_guardian_ivan/desktop/
├── GuardianApp.java       Arranque de JavaFX: ventana, estilos, fuentes e iconos
├── Launcher.java          main() para el fat jar y el .exe
├── controller/            Controladores de cada pantalla y de la ventana de resultado
├── crypto/                AesFileCipher: cifrado y descifrado del formato .hgw
├── files/                 Validación de nombres, tamaños, tipo de archivo, vista previa y abrir ubicación
├── i18n/                  Idioma actual y textos traducidos
├── logging/               Enmascarado de datos sensibles en los registros (logs)
├── navigation/            Cambio entre pantallas
└── ui/                    ScalingPane: escala la interfaz según el tamaño de la ventana
src/main/java/module-info.java   Módulos de Java que usa la aplicación

src/main/resources/el_guardian_ivan/desktop/
├── views/                 Pantallas en FXML
├── css/hogwarts.css       Estilos y paleta de colores
├── i18n/                  messages.properties (es), messages_en, messages_eu
├── fonts/                 Cinzel e Inter (licencia OFL)
└── images/                Logo e iconos de la ventana
src/main/resources/logback.xml   Configuración de los registros (consola y fichero)

src/main/jpackage/         Icono del ejecutable (.ico para Windows, .png para Linux)

src/test/java/...          Tests de JUnit 5 (crypto, files y logging)
src/test/resources/        logback-test.xml para los tests
```

Lo que **no** se sube a git (ver `.gitignore`): `target/` (compilación), `dist/` (ejecutable),
los `.log`, y la configuración personal de IntelliJ (`workspace.xml`, `*.iml`...).

## Tests

```bash
.\mvnw.cmd test
```

Hay unos 140 tests con JUnit 5 (contando cada caso de los tests parametrizados). Comprueban el cifrado (ida y vuelta, contraseña incorrecta, archivos alterados,
truncados, vacíos y de tamaños límite), la validación de nombres, el formato de tamaños, la detección del tipo
de archivo, la vista previa y que los registros no muestren datos sensibles.
Los resultados quedan en `target/surefire-reports`.

## Documentación del código (Javadoc)

```bash
.\mvnw.cmd javadoc:javadoc
```

Se genera en `target/reports/apidocs/index.html`. Incluye todos los paquetes, también los métodos privados.
Todas las clases, métodos y campos (también los de los tests) tienen su comentario Javadoc en castellano,
y cada clase indica sus autores con `@author Iván Salazar` y `@author Iván Alonso`.

## Ejecutable con doble clic (fat jar y .exe)

```bash
.\mvnw.cmd -Pfatjar clean package
```

Genera dos cosas:

- `dist/El Guardian Ivan/El Guardian Ivan.exe`: **se abre con doble clic** y no necesita tener Java
  instalado, porque lleva dentro su propio Java reducido (por eso pesa unos 90 MB). Para pasarlo a otro equipo hay que
  copiar **la carpeta entera** `El Guardian Ivan`, no solo el `.exe`.
- `target/fatjar/El_Guardian_Ivan.jar`: un solo `.jar` con JavaFX dentro. Se lanza con
  `java -jar target/fatjar/El_Guardian_Ivan.jar`, o con doble clic si Windows tiene los `.jar` asociados a Java.

El Java reducido solo lleva los módulos que usa la aplicación (lista `addModules` del `pom.xml`).
Si se añade una biblioteca nueva y el `.exe` falla al abrirse, lo más probable es que falte un módulo ahí
(por ejemplo, Logback necesita `java.naming`).

La carpeta `dist` está fuera de `target` a propósito: Windows suele dejar el `.exe` bloqueado un rato
(antivirus, Explorador) y `clean` fallaría al borrarlo. Cada empaquetado nuevo sustituye la carpeta anterior.

También se puede generar desde IntelliJ: pestaña **Maven**, marcar el perfil **fatjar** y ejecutar
**clean** y **package**.

Hay que generarlos **en cada sistema**: en Windows sale el `.exe` y en Linux sale
`dist/El Guardian Ivan/bin/El Guardian Ivan`, cada uno con el JavaFX y el Java de ese sistema
(en Linux el perfil `linux` se activa solo para usar el icono `.png`). En Linux hace falta el JDK 21
(`sudo apt install openjdk-21-jdk`) y conviene compilar en una carpeta propia, no en una carpeta compartida.

### Generar desde Windows el .jar para Linux (Ubuntu)

Si se quiere compilar en Windows (por ejemplo desde IntelliJ) y probar la aplicación en un Ubuntu,
lo más sencillo es generar el `.jar` único con el JavaFX de Linux dentro. El ejecutable con su propio
Java (`dist/...`) no se puede generar así: `jpackage` solo crea ejecutables del sistema en el que se lanza.

1. **En Windows**, en la pestaña **Terminal** de IntelliJ, dentro de `desktop-javafx\El_Guardian_Ivan`:

   ```
   .\mvnw.cmd -Pfatjar clean package "-Djavafx.platform=linux" "-Djpackage.phase=none" -DskipTests
   ```

   - `-Djavafx.platform=linux` mete en el `.jar` el JavaFX de Linux en lugar del de Windows.
   - `-Djpackage.phase=none` evita que se cree el `.exe` en `dist`, que en Linux no sirve (y en Windows
     tampoco funcionaría, porque llevaría el JavaFX de Linux).

2. **Copiar** `target\fatjar\El_Guardian_Ivan.jar` al Ubuntu (USB, carpeta compartida...).

3. **En Ubuntu**, instalar Java 21 y abrir la aplicación:

   ```bash
   sudo apt install openjdk-21-jre
   java -jar El_Guardian_Ivan.jar
   ```

Ese `.jar` solo funciona en Linux. Para volver a generar la versión de Windows, se empaqueta de nuevo
sin las dos opciones `-D`.

## Problemas frecuentes

| Problema | Solución |
|---|---|
| `Unsupported JavaFX configuration` en consola al abrir el `.jar` o el `.exe` | Es un aviso normal de JavaFX cuando va dentro de un `.jar` único. No afecta. |
| Al empaquetar avisa de que la carpeta `dist/El Guardian Ivan` ya existe | El `.exe` sigue abierto o bloqueado: ciérralo, espera unos segundos y vuelve a empaquetar. |
| El `.exe` no abre y en el log no aparece nada | Falta un módulo de Java en `addModules` del `pom.xml` (ver arriba). Para ver el error, lanza el `.jar` con `java -jar` desde una terminal. |
| `release version 21 not supported` al compilar | Maven está usando otro JDK. Revisa `java -version` y la variable `JAVA_HOME`. |
| En Linux fallan los tests de nombres con acentos o emojis | La terminal no está en UTF-8. Lánzalos con `LANG=C.UTF-8 ./mvnw test`. |
| No encuentro los logs | `%LOCALAPPDATA%\ElGuardianIvan\logs` en Windows, `~/.local/share/ElGuardianIvan/logs` en Linux. |

## Autores

Aplicación desarrollada por:

- **Iván Salazar**
- **Iván Alonso**

Ambos aparecen también en la pantalla **Ayuda > Acerca de** de la aplicación y como `@author`
en el Javadoc de todas las clases.

## Créditos

- Fuentes **Cinzel** (Natanael Gama) e **Inter** (Rasmus Andersson), con licencia SIL Open Font License 1.1.
- Iconos **Material Icons** de Google, con licencia Apache 2.0.
- Registros con **SLF4J** y **Logback** (licencias MIT y EPL/LGPL).
