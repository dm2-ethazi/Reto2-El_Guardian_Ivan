# El Guardián Iván — Módulo Python (Vigenère)

**Cliente de consola para cifrar y descifrar mensajes y archivos de texto mediante el algoritmo Vigenère.** Desarrollado como parte del **Reto 02 — El Guardián Iván**, con una interfaz temática de Hogwarts, procesamiento eficiente de ficheros y controles de calidad automatizados.

| | |
|---|---|
| **Lenguaje** | Python ≥ 3.14 (CI: Python 3.14.7) |
| **Versión del módulo** | `0.1.0` (`pyproject.toml`) |
| **Interfaz** | Terminal / consola interactiva |
| **Formato de archivos** | `.txt` codificados en UTF-8 |
| **Dependencias de ejecución** | Biblioteca estándar de Python |

---

## Índice

1. [Descripción y funcionalidades](#1-descripción-y-funcionalidades)
2. [Requisitos e instalación](#2-requisitos-e-instalación)
3. [Ejecución y manual de uso](#3-ejecución-y-manual-de-uso)
4. [Tratamiento de archivos](#4-tratamiento-de-archivos)
5. [Algoritmo Vigenère](#5-algoritmo-vigenère)
6. [Arquitectura del código](#6-arquitectura-del-código)
7. [Pruebas y cobertura](#7-pruebas-y-cobertura)
8. [Integración continua y calidad](#8-integración-continua-y-calidad)
9. [Sistema de registros y rendimiento](#9-sistema-de-registros-y-rendimiento)
10. [Seguridad, validaciones y limitaciones](#10-seguridad-validaciones-y-limitaciones)
11. [Documentación y equipo](#11-documentación-y-equipo)

## 1. Descripción y funcionalidades

Este módulo permite **cifrar y descifrar mensajes Vigenère** introducidos por teclado o procedentes de archivos de texto. Incorpora una aplicación interactiva por consola, validaciones de entrada, guardado seguro y herramientas de seguimiento y verificación.

| Funcionalidad | Implementación |
|---|---|
| **Cifrado y descifrado** | Vigenère con clave alfabética, conservando mayúsculas y minúsculas. |
| **Dos modos de entrada** | Mensaje escrito manualmente o archivo `.txt`. |
| **Archivos grandes** | Procesamiento por bloques de hasta **1 Mi de caracteres** sin cargar el documento entero en memoria. |
| **Vista previa** | Muestra el resultado completo para archivos de hasta **4 KiB**; en archivos mayores, muestra una vista previa de **500 caracteres**. |
| **Guardado protegido** | Escribe primero en un archivo temporal y **evita sobrescribir** resultados existentes. |
| **Seguimiento** | Registra operaciones, incidencias y tiempos en un log con rotación. |
| **Calidad** | Pruebas con `pytest`, cobertura con `pytest-cov`, análisis con `Ruff` y formato con `Black`. |
| **Automatización** | Workflow de GitHub Actions en cada *push* y *Pull Request*. |

> **Alcance del módulo:** aquí se implementa **Vigenère en Python**. Los componentes **AES**, **César de Android**, **JavaFX** y **Unity** pertenecen a otras partes del proyecto.

## 2. Requisitos e instalación

### Requisitos

- **Python 3.14 o superior**. El pipeline del repositorio utiliza **3.14.7**.
- `pip` y `venv` (incluidos habitualmente en la instalación de Python).
- Una terminal, como **PowerShell** en Windows o Bash en Linux/macOS.
- Acceso a Internet durante la instalación inicial de dependencias de desarrollo, si se van a ejecutar las herramientas de calidad.

### 2.1. Clonar el proyecto

```bash
git clone https://github.com/dm2-ethazi/Reto2-El_Guardian_Ivan.git
cd Reto2-El_Guardian_Ivan
cd crypto-python
```

Todos los comandos de este README, salvo que se indique lo contrario, se ejecutan **desde la carpeta `crypto-python/`**.

### 2.2. Crear y activar el entorno virtual

**Windows — PowerShell**

```powershell
py -3.14 -m venv .venv
.\.venv\Scripts\Activate.ps1
```

Si el comando `py -3.14` no está disponible, utiliza `python -m venv .venv` después de comprobar que `python --version` indica una versión compatible.

Si PowerShell impide ejecutar el script de activación, revisa la política de ejecución del equipo. Una opción es `Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned` cuando esté permitida por la organización. No es necesario modificarla si utilizas directamente el ejecutable de Python del entorno virtual.

### 2.3. Instalar dependencias

**Para ejecutar la aplicación** (sin herramientas de desarrollo):

```bash
python -m pip install -e .
```

**Para desarrollar, ejecutar pruebas y analizar el código:**

```bash
python -m pip install -e ".[dev]"
```

La configuración centralizada se encuentra en [`pyproject.toml`](pyproject.toml). Las versiones de las herramientas se gestionan allí; **Black no tiene versión fijada** en la configuración revisada.

## 3. Ejecución y manual de uso

Inicia la aplicación con:

```bash
python main.py
```

Aparecerá el menú:

```text
===== HOGWARTS - CIFRADO VIGENERE =====
1. Cifrar mensaje
2. Descifrar mensaje
0. Salir
======================================
```

Después de elegir **cifrar** o **descifrar**, selecciona uno de estos métodos:

- **1 — Escribir texto manualmente:** introduce el mensaje, la clave y decide si quieres guardar el resultado.
- **2 — Leer desde un archivo `.txt`:** indica el nombre del archivo de entrada, escribe la clave y decide si deseas guardar el resultado.

### 3.1. Cifrar un mensaje manual

1. Ejecuta `python main.py`.
2. Introduce `1` (**Cifrar mensaje**).
3. Elige `1` (**Escribir texto manualmente**).
4. Escribe el mensaje y una clave compuesta solo por letras ASCII de `A` a `Z` (también se aceptan minúsculas).
5. Consulta el texto cifrado y responde `s` para guardarlo o `n` para volver al menú sin guardar.

**Ejemplo conocido del algoritmo:**

```text
Texto original: ATTACKATDAWN
Clave:          LEMON
Texto cifrado:  LXFOPVEFRNHR
```

### 3.2. Descifrar un mensaje manual

1. Elige `2` (**Descifrar mensaje**) y después `1` (**Escribir texto manualmente**).
2. Introduce el mensaje cifrado y la misma clave.
3. El programa mostrará el texto recuperado.

```text
Texto cifrado:     LXFOPVEFRNHR
Clave:             LEMON
Texto descifrado:  ATTACKATDAWN
```

### 3.3. Cifrar o descifrar un archivo `.txt`

1. Coloca el archivo en la carpeta de **entrada** correspondiente (tabla de la sección siguiente).
2. En el menú, selecciona `1` para cifrar o `2` para descifrar.
3. Elige `2` (**Leer desde un archivo `.txt`**).
4. Escribe el **nombre del archivo**, no la ruta completa. Si omites `.txt`, el programa la añade automáticamente.
5. Introduce la clave y, cuando se solicite, selecciona `s` para guardar.
6. Proporciona un nombre de salida que no exista todavía.

**Navegación:** `0` permite retroceder en los pasos del asistente y salir desde el menú principal. Al escribir texto manualmente, introduce **`:volver`** para regresar a la selección de entrada; así puedes cifrar literalmente el texto `0`.

## 4. Tratamiento de archivos

### 4.1. Carpetas utilizadas

| Operación | Carpeta de entrada | Carpeta de salida |
|---|---|---|
| **Cifrar** | `mensajes_desencriptados/` | `mensajes_encriptados/` |
| **Descifrar** | `mensajes_encriptados/` | `mensajes_desencriptados/` |

Estas rutas están definidas en `src/vigenere/files.py` y son relativas a `crypto-python/`, independientemente de dónde abras la terminal.

**Ejemplo de recorrido:**

```text
mensajes_desencriptados/orden.txt
        │ Cifrar con clave «CLAVE»
        ▼
mensajes_encriptados/orden_cifrada.txt
        │ Descifrar con la misma clave
        ▼
mensajes_desencriptados/orden_recuperada.txt
```

### 4.2. Archivos pequeños y grandes

- **Hasta 4.096 bytes:** el programa lee el archivo completo y muestra todo el resultado antes de ofrecer el guardado.
- **Más de 4.096 bytes:** muestra una vista previa procesada de los primeros **500 caracteres**. Si el usuario elige guardar, procesa el archivo completo por bloques de **1.048.576 caracteres** y conserva la posición de la clave entre bloques.
- Durante el guardado de archivos grandes, muestra **porcentaje, MiB procesados, velocidad y tiempo estimado (ETA)**. Las actualizaciones se limitan aproximadamente a una cada **0,25 s**, excepto la actualización final.

El tratamiento del texto se hace en **UTF-8** y preserva los saltos de línea leídos. Como el cifrado modifica únicamente letras ASCII, los caracteres ajenos a ese alfabeto permanecen intactos.

### 4.3. Escritura segura

Los resultados se guardan de forma provisional en la carpeta de salida y **solo se publican al completar el procesamiento correctamente**. El procedimiento:

1. Comprueba que el archivo de entrada existe y que el destino no está ocupado.
2. Realiza una comprobación preventiva del espacio libre disponible.
3. Escribe el resultado en un archivo temporal UTF-8.
4. Publica el archivo sin sobrescribir uno existente.
5. Elimina el temporal al finalizar o si se produce un error/cancelación.

**Limpieza de temporales:** al arrancar, si encuentra temporales de Vigenère con más de **24 horas**, ofrece borrarlos **solo con confirmación del usuario**. No se eliminan automáticamente por antigüedad.

## 5. Algoritmo Vigenère

**Características exactas de esta versión:**

- La **clave** debe contener exclusivamente letras ASCII `A–Z` o `a–z`; se convierte a mayúsculas para calcular los desplazamientos.
- El algoritmo trabaja solo con las letras **`A–Z` y `a–z`** y conserva su capitalización.
- Los números, espacios, signos, tildes, `ñ`, emojis y otros símbolos **no se alteran ni hacen avanzar la clave**.
- Para procesar archivos grandes, `procesar_bloque()` recibe la posición de la clave del bloque anterior; así se obtiene el mismo resultado que procesando el texto completo.

**Funciones relevantes:**

| Función | Propósito |
|---|---|
| `validar_clave()` | Rechaza claves vacías, con dígitos, acentos o símbolos. |
| `preparar_clave()` | Convierte la clave en desplazamientos de 0 a 25. |
| `procesar_bloque()` | Procesa parte de un mensaje y devuelve la nueva posición de la clave. |
| `cifrar_vigenere()` | Cifra una cadena completa. |
| `descifrar_vigenere()` | Descifra una cadena completa. |

## 6. Arquitectura del código

```text
crypto-python/
├── main.py                          # Punto de entrada y ciclo de vida del log
├── pyproject.toml                   # Dependencias y configuración de calidad
├── README.md                        # Documentación del módulo
├── .gitignore
├── src/
│   └── vigenere/
│       ├── __init__.py
│       ├── cipher.py                # Algoritmo Vigenère
│       ├── cli.py                   # Menús, entrada y salida de consola
│       ├── files.py                 # Lectura, escritura y procesamiento por bloques
│       └── logging_config.py       # Configuración del registro de eventos
├── tests/
│   ├── test_cipher.py
│   ├── test_cli.py
│   ├── test_files.py
│   └── test_logging.py
├── mensajes_desencriptados/         # Textos sin cifrar y recuperados
├── mensajes_encriptados/           # Textos cifrados
└── logs/                           # Generada al ejecutar; ignorada por Git
```

**Separación de responsabilidades:**

| Archivo | Función |
|---|---|
| `main.py` | Inicializa el registro, ejecuta el menú y cierra los manejadores al terminar. |
| `cipher.py` | Implementa y valida la transformación Vigenère. |
| `cli.py` | Coordina la navegación, la entrada de mensajes, las vistas previas y el progreso. |
| `files.py` | Valida nombres, gestiona carpetas, archivos temporales y procesamiento eficiente. |
| `logging_config.py` | Configura el registro rotativo de eventos. |
| `tests/` | Comprueba algoritmo, interfaz, archivos y sistema de registros. |

## 7. Pruebas y cobertura

Las **pruebas automatizadas del componente Python** están en `crypto-python/tests/`:

| Archivo | Aspectos comprobados |
|---|---|
| `test_cipher.py` | Cifrado, descifrado, recuperación, validación de claves y continuidad entre bloques. |
| `test_cli.py` | Navegación, entrada de datos, guardado, errores, archivos grandes, cancelación y progreso. |
| `test_files.py` | Nombres de archivo, no sobrescritura, temporales, UTF-8, espacio en disco y recuperación exacta. |
| `test_logging.py` | Creación, rotación, duplicación de manejadores y protección de datos sensibles en los logs. |

**Ejecutar las pruebas con cobertura de ramas:**

```bash
python -m pytest tests/ --cov=src.vigenere --cov-branch --cov-report=term-missing --cov-fail-under=80
```

**Ver solo el resultado de las pruebas:**

```bash
python -m pytest tests/ -v
```

El archivo `pyproject.toml` exige una **cobertura mínima del 80 %**, incluida la cobertura de ramas. Si queda por debajo, el comando de cobertura devuelve un estado de error.

## 8. Integración continua y calidad

El pipeline está definido en [`.github/workflows/python-ci.yml`](../.github/workflows/python-ci.yml) y se ejecuta en **cada `push` y `pull_request`**, según su configuración actual.

El job **`calidad-python`** se ejecuta en `ubuntu-latest`, con **Python 3.14.7**, y realiza:

1. Obtención del código del repositorio.
2. Instalación de dependencias con `python -m pip install -e ".[dev]"`.
3. Ejecución de `pytest` y comprobación de cobertura **≥ 80 %**.
4. Análisis estático y ordenación de importaciones mediante **Ruff**.
5. Verificación del formato con **Black**.

### Reproducir las comprobaciones localmente

Desde `crypto-python/`, con el entorno virtual activo:

```bash
python -m pytest tests/ --cov=src.vigenere --cov-branch --cov-report=term-missing --cov-fail-under=80
python -m ruff check .
python -m black --check .
```

Para aplicar automáticamente el formato, cuando sea conveniente:

```bash
python -m black .
```

## 9. Sistema de registros y rendimiento

El sistema utiliza `logging` y `RotatingFileHandler` para mantener un registro de eventos en:

```text
crypto-python/logs/vigenere.log
```

- **Niveles utilizados:** `INFO` para operaciones y duración; `WARNING` para incidencias controladas y cancelaciones; `ERROR` para errores de guardado.
- **Rotación:** máximo **1 MiB** por archivo y **3 copias** de respaldo.
- **Formato:** fecha y hora, nivel y mensaje; archivo codificado en UTF-8.
- **Privacidad:** los mensajes de registro definidos no incluyen la **clave** ni el **contenido** de los textos cifrados/descifrados. Se registran tipos de error y tamaños/tiempos cuando corresponde.
- **Medición:** `time.perf_counter()` mide las operaciones con archivos; para archivos grandes se registra el tiempo de procesamiento completo.

La carpeta de logs está excluida del repositorio mediante el `.gitignore` de la raíz.

### Consultar los registros en PowerShell

Desde `crypto-python/`:

```powershell
# Últimos diez eventos
Get-Content .\logs\vigenere.log -Tail 10 -Encoding UTF8

# Seguir el registro mientras se utiliza la aplicación
Get-Content .\logs\vigenere.log -Tail 10 -Wait -Encoding UTF8

# Solo advertencias y errores
Get-Content .\logs\vigenere.log -Encoding UTF8 | Select-String -Pattern 'WARNING|ERROR'

# Ver registros actuales y rotados
Get-ChildItem .\logs\
```

Pulsa **Ctrl+C** para detener el seguimiento en tiempo real.

## 10. Seguridad, validaciones y limitaciones

| Aspecto | Tratamiento en el código |
|---|---|
| **Nombres de archivo** | Solo nombres simples `.txt`; se rechazan rutas, caracteres prohibidos y nombres reservados de Windows. |
| **Claves** | Solo letras ASCII; lectura oculta mediante `getpass` cuando la terminal lo permite. |
| **Entradas vacías** | Se rechazan mensajes y archivos sin texto utilizable. |
| **Codificación** | Lectura y escritura en UTF-8; los errores de decodificación no publican una salida incompleta. |
| **Archivos existentes** | No se sobrescriben; se solicita otro nombre de salida. |
| **Enlaces simbólicos** | No se admiten como entrada de las operaciones de archivos. |
| **Fallos de escritura/cancelación** | Limpieza del archivo temporal, incluido el caso de `Ctrl+C` durante el procesamiento. |
| **Espacio disponible** | Comprobación preventiva antes de escribir; no garantiza que el espacio siga libre posteriormente. |
| **Archivos grandes** | Lectura por bloques; vista previa antes de guardar, no carga completa en memoria. |
| **Seguridad criptográfica** | Vigenère **no** protege información confidencial frente a un atacante moderno. |

### Limitaciones conocidas

- Se admiten **textos `.txt`**, no archivos binarios ni imágenes en este módulo.
- La clave y el texto utilizan **solo letras latinas ASCII para el cifrado**; letras acentuadas y `ñ` permanecen sin modificar.
- El procesamiento por bloques reduce el uso de memoria, pero su rendimiento depende del tamaño del archivo, la codificación y la velocidad de disco.
- Las comprobaciones automatizadas no sustituyen la verificación funcional en los sistemas operativos de entrega.
- **No se adjuntan aquí métricas nuevas ni se afirma una prueba de aceptación de 20 MiB**: para acreditarlas deben ejecutarse y conservarse las evidencias correspondientes.

## 11. Documentación y equipo

**Proyecto:** Reto 02 — *El Guardián Iván*  
**Equipo:** Iván Alonso e Iván Salazar  
**Curso:** 2026–2027
