
# Reto 02 — Módulo Python

Aplicación de cifrado y descifrado Vigenère desarrollada en Python para el Reto 02 - El Guardián.

## Requisitos

- Python 3.14.7
- pip
- Entorno virtual (venv)

## Configuración del entorno

Desde la carpeta `crypto-python`:

### Concede permisos

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Crear el entorno virtual

```powershell
python -m venv .venv
```

### Activar el entorno

En Windows (PowerShell):

```powershell
.\.venv\Scripts\Activate.ps1
```

### Instalar las dependencias

```powershell
python -m pip install -e ".[dev]"
```

Las dependencias están centralizadas en `pyproject.toml`.

## Herramientas de desarrollo

| Herramienta | Versión | Función |
|---|---|---|
| Python | 3.14.7 | Lenguaje de programación |
| Pytest | 9.1.1 | Tests unitarios |
| Coverage.py | 7.17.0 | Medición de cobertura |
| pytest-cov | 7.1 | Integración de cobertura con Pytest |
| Ruff | 0.17.3 | Análisis estático y linting |
| Black | 26.10.1 | Formato del código |

## Ejecutar la aplicación

```powershell
python main.py
```

La aplicación permite cifrar y descifrar mensajes mediante Vigenère, tanto de forma manual como desde archivos de texto.

## Calidad del código

Los siguientes comandos se utilizarán durante el desarrollo de los tests y la configuración del pipeline CI/CD.

### Ejecutar tests y medir cobertura

```powershell
.\crypto-python\.venv\Scripts\python.exe -m pytest crypto-python/tests/ --cov=src.vigenere --cov-config=crypto-python/pyproject.toml --cov-branch --cov-report=term-missing --cov-fail-under=80
```

El objetivo mínimo de cobertura es del 80 %.

### Análisis estático con Ruff

```powershell
.\crypto-python\.venv\Scripts\python.exe -m ruff check crypto-python
```

### Comprobar formato con Black

```powershell
.\crypto-python\.venv\Scripts\python.exe -m black --check crypto-python
```

## Integración continua (CI/CD)

El módulo Python utiliza GitHub Actions para ejecutar automáticamente
las comprobaciones de calidad del código.

El workflow se encuentra en:

`.github/workflows/python-ci.yml` (raíz del repositorio).

### Funcionamiento del pipeline

El pipeline se ejecuta automáticamente en cada push y Pull Request.

Realiza las siguientes operaciones:

1. Descarga el código del repositorio.
2. Configura Python 3.14.7.
3. Instala las dependencias definidas en `pyproject.toml`.
4. Ejecuta los tests unitarios mediante Pytest.
5. Comprueba una cobertura mínima del 80 % con pytest-cov.
6. Analiza el código mediante Ruff.
7. Comprueba el formato mediante Black.

Si alguna comprobación falla, GitHub Actions marca la ejecución
como fallida e indica el paso donde se ha producido el error.

### Resultados de validación

En las pruebas locales se obtuvieron los siguientes resultados:

| Comprobación | Resultado |
|---|---|
| Tests unitarios | 80 superados |
| Cobertura total con ramas | 91,35 % |
| Cobertura mínima exigida | 80 % |
| Cobertura de `logging_config.py` | 100 % |
| Ruff | Sin errores |
| Black | Formato correcto |

### Decisión técnica sobre act

Se ha valorado utilizar `act`, una herramienta que permite
ejecutar workflows de GitHub Actions localmente mediante Docker.

Por el momento, se ha decidido no incorporarla porque:

- GitHub Actions ya ejecuta correctamente el pipeline.
- Pytest, Ruff y Black pueden ejecutarse localmente.
- act requiere instalar y configurar Docker.
- La validación final se realiza en el entorno de GitHub Actions.

Si en el futuro necesitamos ejecutar el pipeline completo sin
conexión a GitHub, podremos reconsiderar su utilización.

## Sistema de logs

La aplicación incorpora un sistema de registro de eventos mediante el módulo `logging` de Python. Su objetivo es facilitar el seguimiento de las operaciones y la identificación de errores durante el cifrado y descifrado.

### Eventos registrados

El sistema registra los siguientes eventos:

- Inicio y finalización de la aplicación.
- Inicio y finalización del cifrado y descifrado de texto manual.
- Inicio y finalización del procesamiento de archivos pequeños y grandes.
- Tamaño de los archivos procesados.
- Duración de las operaciones de cifrado y descifrado de archivos.
- Errores de lectura y guardado.
- Cancelaciones realizadas por el usuario.

Se utilizan distintos niveles de registro:

- `INFO`: operaciones realizadas correctamente.
- `WARNING`: incidencias controladas, validaciones rechazadas y cancelaciones.
- `ERROR`: errores durante el guardado.

### Almacenamiento y rotación

Los registros se guardan en:

`crypto-python/logs/vigenere.log`

El sistema utiliza `RotatingFileHandler` para evitar que los archivos de registro crezcan indefinidamente.

- Tamaño máximo por archivo: 1 MiB.
- Número máximo de copias de seguridad: 3.
- Codificación: UTF-8.

La carpeta `logs/` está incluida en `.gitignore`, por lo que los registros generados no se incorporan al repositorio.

Consultar los logs

Los siguientes comandos se ejecutan desde PowerShell, situándose en la raíz del repositorio Reto2-El_Guardian_Ivan.

Ver los últimos 10 registros:

Get-Content .\crypto-python\logs\vigenere.log -Tail 10 -Encoding UTF8

Ver todos los registros:

Get-Content .\crypto-python\logs\vigenere.log -Encoding UTF8

Ver los logs en tiempo real:

Get-Content .\crypto-python\logs\vigenere.log -Tail 10 -Wait -Encoding UTF8

Este último comando permite observar los nuevos eventos mientras se utiliza la aplicación. Para detener la visualización, pulsar Ctrl+C.

Consultar únicamente advertencias y errores:

Get-Content .\crypto-python\logs\vigenere.log -Encoding UTF8 | Select-String -Pattern "WARNING|ERROR"

Consultar los archivos de log y sus copias de seguridad:

Get-ChildItem .\crypto-python\logs\

Si la terminal está situada dentro de crypto-python, la ruta se simplifica a .\logs\vigenere.log.

### Seguridad de los registros

Los logs no almacenan las claves utilizadas ni el contenido de los mensajes cifrados o descifrados.

Para identificar incidencias se registra el tipo de error, evitando incluir información sensible de los archivos procesados.

### Tiempos de procesamiento

La aplicación registra la duración de las operaciones con archivos mediante `time.perf_counter()`.

- **Archivos pequeños:** se mide el tiempo empleado por el algoritmo de cifrado o descifrado.
- **Archivos grandes:** se mide el tiempo de procesamiento y publicación del archivo completo.

Estas mediciones permiten observar la duración de las operaciones sin necesidad de ejecutar herramientas adicionales de análisis de rendimiento.

### Pruebas unitarias

El archivo `tests/test_logging.py` contiene seis pruebas que verifican:

- La creación del archivo de registros.
- El almacenamiento correcto de eventos.
- La ausencia de manejadores duplicados.
- La rotación automática de logs.
- La protección de claves y contenido sensible en el cifrado manual.
- El registro de cancelaciones.
