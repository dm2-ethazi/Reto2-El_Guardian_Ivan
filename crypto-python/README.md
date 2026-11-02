
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
| Tests unitarios | 74 superados |
| Cobertura total con ramas | 90,77 % |
| Cobertura mínima exigida | 80 % |
| Ruff | Sin errores |
| Black | Formato correcto |

La primera ejecución del workflow en GitHub Actions finalizó
correctamente en la rama `feature/python-tests-ci`.

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
