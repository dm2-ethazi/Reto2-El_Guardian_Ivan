# Reto2-El_Guardian_Ivan

## Integrantes

- Iván Alonso
- Iván Salazar

## Descripción del proyecto

El objetivo del Reto 02 es desarrollar una solución
software que permita cifrar y descifrar información
mediante diferentes algoritmos.

El proyecto integra Python, JavaFX, Android y Unity,
además de herramientas de control de versiones,
pruebas automatizadas e integración continua.

El juego de Unity (El Guardián Iván) se explica, con cómo compilarlo y modificarlo, en [juego-unity/README.md](juego-unity/README.md).

## Flujo de trabajo Git

El proyecto Reto 02 se desarrolla de forma colaborativa mediante Git y GitHub.

### Estructura de ramas

- `main`: contiene las versiones estables del proyecto.
- `develop`: rama de integración de funcionalidades.
- `feature/*`: desarrollo de nuevas funcionalidades.
- `fix/*`: corrección de errores.

Las ramas `main` y `develop` están protegidas mediante reglas de GitHub. Los cambios se integran mediante Pull Requests.

### Procedimiento de desarrollo

1. Actualizar la rama `develop`.
2. Crear una rama `feature/` o `fix/` a partir de `develop`.
3. Desarrollar los cambios necesarios.
4. Realizar commits con mensajes descriptivos.
5. Subir la rama al repositorio remoto.
6. Crear una Pull Request hacia `develop`.
7. Solicitar la revisión del otro integrante.
8. Integrar los cambios después de su aprobación.
9. Fusionar `develop` con `main` cuando exista una versión estable.

### Convención de commits

Se utilizarán mensajes en imperativo con los siguientes prefijos:

- `feat:` nueva funcionalidad.
- `fix:` corrección de errores.
- `docs:` cambios en documentación.
- `test:` incorporación o modificación de pruebas.
- `chore:` tareas de mantenimiento y configuración.

Ejemplo:

`feat: implementar cifrado Vigenere`

### Hooks de Git

Los hooks se encuentran en la carpeta `.githooks`.

**pre-commit**

Ejecuta las pruebas de Python mediante pytest cuando existe la carpeta `tests`.

La carpeta `crypto-python/tests/` ya contiene pruebas unitarias para el algoritmo, la gestión de archivos y la interfaz de consola. El hook ejecuta pytest cuando detecta la carpeta de pruebas.

**pre-push**

Impide realizar pushes directos a las ramas `main` y `develop`.

### Activación de hooks

Cada desarrollador debe ejecutar una vez, desde la raíz de su repositorio local:

```bash
git config core.hooksPath .githooks
```

Para comprobar la configuración:

```bash
git config --get core.hooksPath
```

### GitHub Projects

El proyecto utiliza GitHub Projects para organizar y hacer seguimiento de las tareas.

Los estados de trabajo son:

- **Backlog:** tareas pendientes de planificación.
- **Ready:** tareas preparadas para comenzar.
- **In progress:** tareas en desarrollo.
- **Review:** tareas pendientes de revisión.
- **Done:** tareas completadas.

Cada funcionalidad se gestiona mediante una Issue y sus cambios se registran mediante Pull Requests.

### Integración continua — módulo Python (AD-03)

El proyecto utiliza **GitHub Actions** para comprobar automáticamente la calidad del módulo Python del Reto 02. El workflow está definido en [`.github/workflows/python-ci.yml`](.github/workflows/python-ci.yml) y se configura para ejecutarse en cada **push** y **Pull Request**.

El trabajo `calidad-python` utiliza Python 3.14.7 en un runner de Ubuntu y realiza estas verificaciones:

1. Descarga el código y configura Python.
2. Instala las dependencias de desarrollo definidas en [`crypto-python/pyproject.toml`](crypto-python/pyproject.toml).
3. Ejecuta los tests unitarios con **pytest** y **pytest-cov**.
4. Comprueba que la cobertura total, incluyendo ramas, alcanza **al menos el 80 %**.
5. Realiza el análisis estático y comprueba las importaciones con **Ruff**.
6. Verifica el formato del código con **Black**.

#### Validación realizada

En la validación local del módulo Python se obtuvieron los siguientes resultados:

| Comprobación | Resultado |
| --- | --- |
| Tests unitarios | **74 superados**, 0 fallidos |
| Cobertura total con ramas | **90,77 %** |
| Cobertura mínima exigida | **80 % — superada** |
| Ruff | Sin errores |
| Black | Formato correcto |

La **primera ejecución del workflow por push** en GitHub Actions finalizó con estado **Success** en la rama `feature/python-tests-ci`. El workflow también está configurado para Pull Requests, cuya ejecución se comprobará al abrir una PR.

#### Ejecutar las comprobaciones localmente

Con las dependencias de desarrollo instaladas y el entorno Python activado, desde la carpeta `crypto-python`:

```bash
python -m pytest tests/ --cov=src.vigenere --cov-branch --cov-report=term-missing --cov-fail-under=80
python -m ruff check .
python -m black --check .
```

La documentación específica de instalación y uso del módulo está en [`crypto-python/README.md`](crypto-python/README.md).

#### Decisión técnica sobre `act`

Se ha valorado **act**, una herramienta que permite ejecutar workflows de GitHub Actions localmente mediante Docker. Por el momento **no se utiliza**, porque las comprobaciones individuales ya pueden ejecutarse desde el entorno Python y el pipeline completo se ha validado correctamente en GitHub Actions. Incorporar `act` añadiría la necesidad de instalar y configurar Docker para este flujo.

Podrá adoptarse más adelante si resulta útil ejecutar el workflow completo sin depender de GitHub o depurar diferencias entre el entorno local y el runner. Esta decisión no implica que se haya probado `act` en este proyecto.

