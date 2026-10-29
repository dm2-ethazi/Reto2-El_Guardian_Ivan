"""Menú de Hogwarts para cifrar y descifrar mensajes con Vigenère."""

from collections.abc import Callable
from pathlib import Path

from .cipher import cifrar_vigenere, descifrar_vigenere, validar_clave
from .files import (
    guardar_mensaje,
    leer_mensaje,
    obtener_carpetas,
    procesar_archivo_grande,
    validar_nombre,
)


# Hasta 4 KiB mostramos el contenido completo del archivo.
TAMANO_MAXIMO_PANTALLA = 4096

# Para archivos grandes mostramos únicamente 500 caracteres.
TAMANO_VISTA_PREVIA = 500

# Cantidad de caracteres que leemos cada vez para comprobar un archivo.
TAMANO_BLOQUE_VALIDACION = 64 * 1024


def pedir_dato(mensaje: str) -> str | None:
    """Lee un dato; '0' indica que se quiere volver al paso anterior."""
    respuesta = input(mensaje)
    return None if respuesta.strip() == "0" else respuesta


def mostrar_menu() -> None:
    """Muestra el menú principal (único lugar donde 0 significa salir)."""
    print("\n===== HOGWARTS - CIFRADO VIGENERE =====")
    print("1. Cifrar mensaje")
    print("2. Descifrar mensaje")
    print("0. Salir")
    print("======================================")


def mostrar_progreso(procesados: int, total: int) -> None:
    """Actualiza en una sola línea el porcentaje del archivo procesado."""
    porcentaje = 100.0 if total == 0 else min(100.0, procesados * 100 / total)
    longitud_barra = 30
    completados = int(longitud_barra * porcentaje / 100)
    barra = "#" * completados + "-" * (longitud_barra - completados)

    procesados_mib = procesados / (1024 * 1024)
    total_mib = total / (1024 * 1024)

    print(
        f"\r[{barra}] {porcentaje:6.2f}% "
        f"({procesados_mib:,.1f} / {total_mib:,.1f} MiB)",
        end="",
        flush=True,
    )


def obtener_clave() -> str | None:
    """Solicita una clave válida o vuelve al dato anterior con 0."""
    while True:
        clave = pedir_dato("\nIntroduce la clave (0 para volver): ")
        if clave is None:
            return None

        try:
            return validar_clave(clave.strip())
        except ValueError as error:
            print(f"\nError: {error}")
            print("Introduce otra clave.")


def archivo_sin_texto(ruta: Path) -> bool:
    """
    Comprueba si un .txt está vacío o solo contiene espacios en blanco.

    Lee fragmentos pequeños para no cargar archivos grandes en memoria.
    Devuelve False en cuanto encuentra un carácter que no sea espacio.
    """
    with ruta.open("r", encoding="utf-8", newline="") as fichero:
        while True:
            bloque = fichero.read(TAMANO_BLOQUE_VALIDACION)
            if not bloque:
                return True
            if bloque.strip():
                return False


def obtener_nombre_entrada(cifrar: bool) -> str | None:
    """Pide un archivo existente en la carpeta de entrada correspondiente."""
    carpeta_entrada, _ = obtener_carpetas(cifrar)
    carpeta_entrada.mkdir(parents=True, exist_ok=True)

    print(f"\nCarpeta de entrada: {carpeta_entrada}")

    while True:
        nombre = pedir_dato("Nombre del archivo (0 para volver): ")
        if nombre is None:
            return None

        try:
            # validar_nombre añade .txt automáticamente si falta.
            nombre = validar_nombre(nombre)
            ruta = carpeta_entrada / nombre

            if not ruta.is_file():
                raise FileNotFoundError(f"No existe el archivo: {nombre}")

            # Rechazamos archivos de 0 bytes o con solo espacios/saltos
            # de línea. Se comprueba sin leer el archivo completo en RAM.
            if archivo_sin_texto(ruta):
                raise ValueError(
                    "El archivo está vacío o solo contiene espacios en blanco."
                )

            return nombre
        except (ValueError, OSError, UnicodeError) as error:
            print(f"\nError: {error}")
            print("Introduce otro nombre.")


def preguntar_guardado(guardar: Callable[[str], Path]) -> str:
    """
    Pregunta qué hacer con un resultado.

    Devuelve 'atras' si se pulsa 0 en el menú de guardado.
    Devuelve 'menu' si se guarda o se decide no guardar.
    """
    while True:
        print("\n¿Qué quieres hacer?")
        print("s. Guardar resultado")
        print("n. No guardar y volver al menú principal")
        print("0. Volver al paso anterior")

        respuesta = pedir_dato("Selecciona una opción: ")
        if respuesta is None:
            return "atras"

        respuesta = respuesta.strip().lower()

        if respuesta == "n":
            return "menu"

        if respuesta != "s":
            print("Opción no válida. Inténtalo de nuevo.")
            continue

        while True:
            nombre = pedir_dato("\nNombre de salida (0 para volver): ")

            # Vuelve a la pregunta de guardado, no al menú principal.
            if nombre is None:
                break

            try:
                ruta = guardar(nombre)
                print("\nArchivo guardado correctamente.")
                print(f"Ubicación: {ruta}")
                return "menu"
            except (ValueError, FileExistsError) as error:
                print(f"\nError: {error}")
                print("Introduce otro nombre.")
            except (OSError, UnicodeError) as error:
                print(f"\nError al guardar: {error}")
                print("No se ha publicado un resultado incompleto.")
                return "menu"


def ejecutar_texto_manual(cifrar: bool) -> str:
    """Procesa mensajes introducidos manualmente, permitiendo retroceder."""
    while True:
        texto = pedir_dato("\nIntroduce el mensaje (0 para volver): ")
        if texto is None:
            return "atras"  # Volver a la selección de tipo de entrada.

        # No aceptamos una cadena vacía ni mensajes solo con espacios.
        if not texto.strip():
            print("\nError: El mensaje no puede estar vacío.")
            print("Introduce un texto válido.")
            continue

        while True:
            clave = obtener_clave()
            if clave is None:
                break  # Volver a introducir el texto.

            if cifrar:
                resultado = cifrar_vigenere(texto, clave)
                print("\n--- MENSAJE CIFRADO ---")
            else:
                resultado = descifrar_vigenere(texto, clave)
                print("\n--- MENSAJE DESCIFRADO ---")

            print(resultado)

            def guardar(nombre: str) -> Path:
                return guardar_mensaje(nombre, resultado, cifrado=cifrar)

            estado = preguntar_guardado(guardar)
            if estado == "menu":
                return "menu"
            # Si pulsa 0, vuelve a la clave.


def ejecutar_archivo(cifrar: bool) -> str:
    """Procesa archivos pequeños o grandes conservando la navegación."""
    while True:
        nombre_entrada = obtener_nombre_entrada(cifrar)
        if nombre_entrada is None:
            return "atras"  # Volver a elegir manual o archivo.

        carpeta_entrada, carpeta_salida = obtener_carpetas(cifrar)
        archivo_entrada = carpeta_entrada / nombre_entrada

        while True:
            clave = obtener_clave()
            if clave is None:
                break  # Volver a elegir el archivo de entrada.

            try:
                tamano = archivo_entrada.stat().st_size

                if tamano <= TAMANO_MAXIMO_PANTALLA:
                    # Para archivos pequeños mostramos todo el resultado.
                    texto = leer_mensaje(nombre_entrada, cifrar)
                    if cifrar:
                        resultado = cifrar_vigenere(texto, clave)
                        print("\n--- MENSAJE CIFRADO ---")
                    else:
                        resultado = descifrar_vigenere(texto, clave)
                        print("\n--- MENSAJE DESCIFRADO ---")
                    print(resultado)
                else:
                    # Para archivos grandes solo leemos los primeros caracteres.
                    with archivo_entrada.open(
                        "r", encoding="utf-8", newline=""
                    ) as fichero:
                        muestra = fichero.read(TAMANO_VISTA_PREVIA)

                    if cifrar:
                        vista_previa = cifrar_vigenere(muestra, clave)
                        print("\n--- VISTA PREVIA DEL CIFRADO ---")
                    else:
                        vista_previa = descifrar_vigenere(muestra, clave)
                        print("\n--- VISTA PREVIA DEL DESCIFRADO ---")

                    print(vista_previa)
                    print(f"\n[Primeros {TAMANO_VISTA_PREVIA} caracteres]")
                    print(f"Tamaño: {tamano:,} bytes")

            except (OSError, UnicodeError) as error:
                print(f"\nError al leer el archivo: {error}")
                break  # Volver a seleccionar otro archivo.

            def guardar(nombre: str) -> Path:
                # Los archivos pequeños pueden guardarse directamente.
                if tamano <= TAMANO_MAXIMO_PANTALLA:
                    return guardar_mensaje(nombre, resultado, cifrado=cifrar)

                # Los archivos grandes se cifran o descifran por bloques.
                print("\nProcesando archivo por bloques...")

                try:
                    ruta = procesar_archivo_grande(
                        nombre_entrada,
                        nombre,
                        clave,
                        cifrar,
                        progreso=mostrar_progreso,
                    )
                finally:
                    # Deja libre la línea incluso si hay un error.
                    print()

                return ruta

            print(f"\nCarpeta de salida: {carpeta_salida}")

            estado = preguntar_guardado(guardar)
            if estado == "menu":
                return "menu"
            # Si pulsa 0, vuelve a introducir la clave.


def ejecutar_operacion(cifrar: bool) -> None:
    """Permite elegir entre mensaje manual y archivo .txt."""
    while True:
        print("\n¿Cómo quieres introducir el texto?")
        print("1. Escribir texto manualmente")
        print("2. Leer desde un archivo .txt")
        print("0. Volver al menú principal")

        opcion = pedir_dato("Selecciona una opción: ")
        if opcion is None:
            return

        opcion = opcion.strip()
        if opcion == "1":
            estado = ejecutar_texto_manual(cifrar)
        elif opcion == "2":
            estado = ejecutar_archivo(cifrar)
        else:
            print("Opción no válida. Inténtalo de nuevo.")
            continue

        if estado == "menu":
            return
        # 'atras' vuelve a elegir entre texto manual y archivo.


def main() -> None:
    """El programa solo se cierra con 0 desde el menú principal."""
    try:
        while True:
            mostrar_menu()
            opcion = input("\nSelecciona una opción: ").strip()

            if opcion == "0":
                print("\nSaliendo de Hogwarts. ¡Hasta pronto!")
                break
            elif opcion == "1":
                ejecutar_operacion(cifrar=True)
            elif opcion == "2":
                ejecutar_operacion(cifrar=False)
            else:
                print("Opción no válida. Inténtalo de nuevo.")
    except (KeyboardInterrupt, EOFError):
        print("\nPrograma interrumpido.")


if __name__ == "__main__":
    main()
