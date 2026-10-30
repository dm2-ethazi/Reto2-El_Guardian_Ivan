
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
python -m pytest --cov=src.vigenere --cov-report=term-missing
```

El objetivo mínimo de cobertura es del 80 %.

### Análisis estático con Ruff

```powershell
python -m ruff check .
```

### Comprobar formato con Black

```powershell
python -m black --check .
```

## Integración continua

Se configurará GitHub Actions para ejecutar automáticamente las comprobaciones de calidad en los push y Pull Requests.
