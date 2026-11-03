# El Guardián Iván · Aplicación de escritorio (JavaFX)

Aplicación de escritorio del Reto 2 para **cifrar y descifrar archivos con contraseña**.
Funciona con cualquier tipo de archivo (textos, imágenes, documentos, vídeos...) y lo protege con
AES-256-GCM. Todo se hace en el propio equipo: la aplicación no se conecta a ningún servidor y la
contraseña no se guarda en ningún sitio.

Autores: Iván Alonso e Iván Salazar.

## Funcionalidades

- **Cifrar** cualquier archivo y obtener un contenedor `.hgw`. El original no se borra.
- **Descifrar** un `.hgw` y recuperar el archivo original con su extensión.
- **Vista previa** al descifrar: si el resultado es una imagen o un texto plano, se muestra en la ventana de resultado.
- **Detección de errores**: si la contraseña es incorrecta o el `.hgw` está dañado o modificado, se avisa y no se genera ningún archivo.
- **Archivos grandes**: se cifran por trozos de 1 MB, así que la memoria usada no depende del tamaño del archivo.
- **Tres idiomas**: castellano, inglés y euskera. Se cambia en Ajustes y se recuerda para la próxima vez.
- **Accesibilidad**: atajos de teclado, foco visible, textos para lectores de pantalla y una interfaz que se adapta al tamaño de la ventana.
- **Ayuda** integrada con preguntas frecuentes, atajos y "Acerca de".

## Requisitos

- JDK 21 (por ejemplo Eclipse Temurin 21).
- No hace falta instalar Maven: el proyecto incluye el wrapper (`mvnw` / `mvnw.cmd`).

## Cómo ejecutarla

Desde la carpeta `desktop-javafx/El_Guardian_Ivan`:

```bash
# Windows
.\mvnw.cmd clean javafx:run

# Linux / macOS
./mvnw clean javafx:run
```

También se puede abrir la carpeta en IntelliJ IDEA como proyecto Maven y lanzar `GuardianApp`.

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

## Estructura del proyecto

```
src/main/java/el_guardian_ivan/desktop/
├── GuardianApp.java       Arranque de JavaFX: ventana, estilos, fuentes e iconos
├── Launcher.java          main() para el fat jar y el .exe
├── controller/            Controladores de cada pantalla y de la ventana de resultado
├── crypto/                AesFileCipher: cifrado y descifrado del formato .hgw
├── files/                 Validación de nombres, tamaños, tipo de archivo, vista previa y abrir ubicación
├── i18n/                  Idioma actual y textos traducidos
├── navigation/            Cambio entre pantallas
└── ui/                    ScalingPane: escala la interfaz según el tamaño de la ventana

src/main/resources/el_guardian_ivan/desktop/
├── views/                 Pantallas en FXML
├── css/hogwarts.css       Estilos y paleta de colores
├── i18n/                  messages.properties (es), messages_en, messages_eu
├── fonts/                 Cinzel e Inter (licencia OFL)
└── images/                Logo e iconos de la ventana

src/main/jpackage/         Icono del ejecutable (.ico para Windows, .png para Linux)
```

## Tests

```bash
.\mvnw.cmd test
```

Hay 120 tests con JUnit 5. Comprueban el cifrado (ida y vuelta, contraseña incorrecta, archivos alterados,
truncados, vacíos y de tamaños límite), la validación de nombres, el formato de tamaños, la detección del tipo
de archivo y la vista previa.

## Documentación del código (Javadoc)

```bash
.\mvnw.cmd javadoc:javadoc
```

Se genera en `target/reports/apidocs/index.html`. Incluye todos los paquetes, también los métodos privados.

## Ejecutable con doble clic (fat jar y .exe)

```bash
.\mvnw.cmd -Pfatjar clean package
```

Genera dos cosas:

- `dist/El Guardian Ivan/El Guardian Ivan.exe`: **se abre con doble clic** y no necesita tener Java
  instalado, porque lleva dentro su propio Java (por eso pesa unos 90 MB). Para pasarlo a otro equipo hay que
  copiar **la carpeta entera** `El Guardian Ivan`, no solo el `.exe`.
- `target/fatjar/El_Guardian_Ivan.jar`: un solo `.jar` con JavaFX dentro. Se lanza con
  `java -jar target/fatjar/El_Guardian_Ivan.jar`, o con doble clic si Windows tiene los `.jar` asociados a Java.

La carpeta `dist` está fuera de `target` a propósito: Windows suele dejar el `.exe` bloqueado un rato
(antivirus, Explorador) y `clean` fallaría al borrarlo. Cada empaquetado nuevo sustituye la carpeta anterior;
si avisa de que la carpeta ya existe, es que sigue bloqueada: cierra la aplicación y vuelve a intentarlo.
La carpeta `dist` no se sube a git.

También se puede generar desde IntelliJ: pestaña **Maven**, marcar el perfil **fatjar** y ejecutar
**clean** y **package**.

Hay que generarlos **en cada sistema**: en Windows sale el `.exe` y en Linux sale
`dist/El Guardian Ivan/bin/El Guardian Ivan`, cada uno con el JavaFX y el Java de ese sistema
(en Linux el perfil `linux` se activa solo para usar el icono `.png`). En Linux hace falta el JDK 21
(`sudo apt install openjdk-21-jdk`) y conviene compilar en una carpeta propia, no en una carpeta compartida. El aviso `Unsupported JavaFX configuration` en consola es normal y no afecta.

## Créditos

- Fuentes **Cinzel** (Natanael Gama) e **Inter** (Rasmus Andersson), con licencia SIL Open Font License 1.1.
- Iconos **Material Icons** de Google, con licencia Apache 2.0.
