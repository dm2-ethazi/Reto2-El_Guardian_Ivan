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

Actualmente permite los commits si la carpeta todavía no existe. Este comportamiento se actualizará al incorporar las pruebas definitivas.

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

### Integración continua

El proyecto incorporará GitHub Actions para ejecutar automáticamente los tests y comprobar la calidad del código.

El objetivo de cobertura de las pruebas unitarias es de al menos el 80 %, según el pliego técnico.
