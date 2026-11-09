# El Guardián Iván — Aplicación Android

Aplicación Android nativa desarrollada en **Kotlin y layouts XML** para el **Reto 02 — Hogwarts**, dentro del proyecto **El Guardián Iván** (Iván Alonso e Iván Salazar).

La aplicación representa al *Auror desplegado*: permite **consultar y descifrar información recibida**, sin incorporar funciones para cifrar nuevos mensajes o archivos.

## Funcionalidades

- **Descifrado César:** selección de archivos `.txt`, introducción del desplazamiento y visualización del texto recuperado. Conserva los caracteres no incluidos en `A–Z` y `a–z`. Valida el contenido como UTF-8 y rechaza archivos vacíos, binarios o no válidos.
- **Descifrado AES de imágenes:** apertura de contenedores `.hgw` compatibles con el formato **HOG2** del cliente JavaFX, introducción de contraseña y recuperación de imágenes PNG, JPEG o WebP.
- **Recepción de archivos:** selección mediante el selector de documentos de Android y recepción desde la opción nativa **Compartir** de otras aplicaciones (`ACTION_SEND`).
- **Resultados:** visualización, guardado y posibilidad de compartir imágenes descifradas; opción de compartir texto descifrado.
- **Ajustes:** cambio de idioma (**español, euskera e inglés**) y confirmación opcional antes de compartir resultados.
- **Gestión de errores:** validaciones y avisos ante archivos incompatibles, contraseñas incorrectas, contenido alterado o problemas de lectura.

## Requisitos y tecnologías

| Elemento | Configuración del proyecto incluido |
|---|---|
| Lenguaje | Kotlin |
| Interfaz | XML, AndroidX y Material Components |
| SDK mínimo | API 26 (Android 8.0) |
| SDK de compilación | API 37 |
| SDK objetivo | API 37 |
| Android Gradle Plugin | 9.3.3 (catálogo de versiones) |
| Gradle Wrapper | 9.5.0 |
| Identificador de aplicación | `com.elguardian.ivan` |

Se necesita Android Studio con el SDK y las herramientas de compilación compatibles configurados. Las versiones del entorno local se documentarán según la instalación efectivamente utilizada.

## Instalación y ejecución

La aplicación **El Guardián Iván** se distribuye mediante un archivo APK, que permite instalarla directamente en un dispositivo Android sin necesidad de utilizar Android Studio.

### Requisitos

- Dispositivo móvil con Android 8.0 (API 26) o superior.
- Archivo de instalación `ElGuardianIvan.apk`.
- Espacio de almacenamiento suficiente para instalar y ejecutar la aplicación.

### Pasos de instalación

1. Recibir el archivo `ElGuardianIvan.apk` proporcionado por el equipo desarrollador.
2. Guardar el archivo en el dispositivo Android.
3. Abrir el APK desde el gestor de archivos del dispositivo.
4. Si Android lo solicita, autorizar la instalación de aplicaciones desde esa fuente.
5. Pulsar **Instalar** y esperar a que finalice el proceso.
6. Abrir la aplicación **El Guardián Iván** desde el menú de aplicaciones.

### Ejecución

Una vez instalada, la aplicación podrá ejecutarse directamente desde el dispositivo, sin necesidad de conexión a un ordenador ni de herramientas de desarrollo.

La aplicación permite seleccionar o recibir archivos, descifrar mensajes mediante César y recuperar imágenes protegidas con AES, mostrando los resultados en pantalla.

**Nota:** los permisos para instalar APK desde fuentes externas dependen de la versión y configuración de Android. Se recomienda instalar únicamente el archivo proporcionado por el equipo desarrollador.

## Uso

### Descifrar un mensaje César

1. Abre **César** desde la pantalla de inicio.
2. Selecciona un archivo `.txt` que contenga texto cifrado.
3. Introduce el desplazamiento correspondiente.
4. Pulsa **Descifrar** para consultar el mensaje.
5. Si lo necesitas, utiliza la opción para compartir el texto recuperado.

El código de validación exige contenido UTF-8 y el algoritmo modifica únicamente letras ASCII; conserva números, espacios, signos y otros caracteres.

### Descifrar una imagen AES

1. Obtén una imagen cifrada en formato `.hgw` compatible con el contenedor HOG2 del proyecto JavaFX.
2. Abre **AES** y selecciona el archivo.
3. Introduce la contraseña utilizada para cifrarlo.
4. Pulsa **Descifrar**.
5. Consulta la imagen en **Resultado** y, si corresponde, guárdala o compártela.

El descifrado se realiza fuera del hilo principal de interfaz. Los resultados se gestionan temporalmente dentro del almacenamiento privado de la aplicación.

### Recibir un archivo desde otra aplicación

Abre el archivo en otra aplicación Android y utiliza **Compartir → El Guardián Iván**. La aplicación dirige los `.txt` a César y los `.hgw` a AES; para otros nombres de archivo muestra opciones de selección.

## Organización del código

```text
android-app/
├── app/
│   ├── build.gradle.kts
│   └── src/main/
│       ├── AndroidManifest.xml
│       ├── java/com/elguardian/ivan/
│       │   ├── app/
│       │   │   └── GuardianApplication.kt
│       │   ├── crypto/
│       │   │   ├── CesarCipher.kt
│       │   │   ├── CesarTextValidator.kt
│       │   │   └── HgwDecryptor.kt
│       │   ├── ui/
│       │   │   ├── GuardianActivity.kt
│       │   │   ├── MainActivity.kt
│       │   │   ├── CesarActivity.kt
│       │   │   ├── AesActivity.kt
│       │   │   ├── ResultActivity.kt
│       │   │   └── SettingsActivity.kt
│       │   └── utils/
│       │       └── FileUtils.kt
│       └── res/
│           ├── drawable/
│           ├── layout/
│           ├── layout-land/
│           ├── mipmap-*/
│           ├── values/
│           ├── values-en/
│           ├── values-eu/
│           ├── values-night/
│           └── xml/
├── docs/
│   └── formato-hgw.md
├── gradle/
├── gradlew
├── gradlew.bat
├── build.gradle.kts
├── settings.gradle.kts
└── .gitignore
```

**Responsabilidades:** `ui` gestiona pantallas y navegación; `crypto` implementa los algoritmos y las validaciones; `utils` contiene utilidades de archivos; `app` inicializa la configuración global, incluido el idioma.

## Interoperabilidad con JavaFX

El archivo `docs/formato-hgw.md` especifica el contenedor cifrado compartido. La implementación Android espera, entre otros elementos:

- Identificador `HOG2` y versión `01`.
- Cabecera de **37 bytes**, que incluye sal, nonce base y tamaño de bloque.
- **AES-256-GCM**, con derivación de clave mediante **PBKDF2-HMAC-SHA-256** y **200 000 iteraciones**.
- Autenticación de cada bloque cifrado y recuperación de la imagen únicamente si la verificación es correcta.

**Importante:** el formato implementado es un protocolo específico del proyecto; un archivo cifrado con AES genérico no tiene por qué ser compatible. La interoperabilidad real debe comprobarse cifrando una imagen desde JavaFX y descifrándola en Android.

## Seguridad y privacidad

- No se necesita permiso general para acceder a todo el almacenamiento: se utiliza el selector de documentos de Android.
- La contraseña AES no se guarda como preferencia de la aplicación.
- El intercambio de imágenes con otras aplicaciones utiliza `FileProvider` y permisos temporales de lectura.
- El contenido descifrado es sensible: evita compartirlo innecesariamente y revisa las opciones de copia de seguridad del dispositivo antes de utilizar datos reales.

## Documentación relacionada

- `docs/formato-hgw.md`: contrato técnico de interoperabilidad AES con JavaFX.
- Pliego del Reto 02: apartado 4.10, aplicación móvil; apartados 4.12 y 4.13, calidad y configuración.
- Propuesta técnica del equipo: apartado 09, aplicación Android; apartado 12, validación y pruebas.

## Equipo

**El Guardián Iván** — Reto 02, curso 2026–2027  
**Integrantes:** Iván Alonso e Iván Salazar.
