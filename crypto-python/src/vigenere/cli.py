"""Menú de Hogwarts para cifrar y descifrar mensajes con Vigenère."""

from collections.abc import Callable
from getpass import getpass
from pathlib import Path
import sys
import time

from .cipher import cifrar_vigenere, descifrar_vigenere, validar_clave
from .files import (
    ArchivoSinTextoError,
    detectar_temporales_antiguos,
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

# Tiempo mínimo entre actualizaciones de progreso en pantalla.
INTERVALO_PROGRESO = 0.25


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


def crear_indicador_progreso() -> Callable[[int, int], None]:
    """Devuelve un indicador con porcentaje, velocidad y tiempo estimado.

    Evita imprimir después de cada bloque si aún no han transcurrido 0,25 s.
    """
    inicio = time.monotonic()
    ultima_actualizacion = inicio
    ancho_anterior = 0

    def mostrar_progreso(procesados: int, total: int) -> None:
        nonlocal ultima_actualizacion, ancho_anterior
        ahora = time.monotonic()
        if procesados < total and ahora - ultima_actualizacion < INTERVALO_PROGRESO:
            return
        ultima_actualizacion = ahora

        porcentaje = 100.0 if total == 0 else min(100.0, procesados * 100 / total)
        completados = int(30 * porcentaje / 100)
        barra = "#" * completados + "-" * (30 - completados)
        procesados_mib = procesados / (1024 * 1024)
        total_mib = total / (1024 * 1024)
        transcurrido = max(ahora - inicio, 0.001)
        velocidad = procesados_mib / transcurrido
        restante = max(total - procesados, 0)
        segundos_restantes = int(restante / (procesados / transcurrido)) if procesados else 0
        tiempo_estimado = f"{segundos_restantes // 60:02}:{segundos_restantes % 60:02}" if procesados else "--:--"

        linea = (
            f"[{barra}] {porcentaje:6.2f}% "
            f"({procesados_mib:,.1f}/{total_mib:,.1f} MiB) "
            f"{velocidad:.1f} MiB/s ETA {tiempo_estimado}"
        )
        print("\r" + linea.ljust(ancho_anterior), end="", flush=True)
        ancho_anterior = len(linea)

    return mostrar_progreso


def pedir_clave_oculta() -> str:
    """Oculta la contraseña en un terminal; acepta entrada redirigida."""
    mensaje = "\nIntroduce la clave (0 para volver): "
    if sys.stdin.isatty() and sys.stderr.isatty():
        return getpass(mensaje)
    return input(mensaje)


def obtener_clave() -> str | None:
    """Solicita una clave válida o vuelve al dato anterior con 0."""
    while True:
        clave = pedir_clave_oculta()
        if clave.strip() == "0":
            return None

        try:
            return validar_clave(clave.strip())
        except ValueError as error:
            print(f"\nError: {error}")
            print("Introduce otra clave.")


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

            if ruta.is_symlink():
                raise ValueError("No se admiten enlaces simbólicos como entrada.")
            if ruta.stat().st_size == 0:
                raise ArchivoSinTextoError("El archivo de entrada está vacío.")
            # Los archivos con solo espacios se detectan durante el procesado
            # para evitar leer los grandes dos veces.

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
            except ArchivoSinTextoError as error:
                print(f"\nError en el contenido: {error}")
                return "menu"
            except (ValueError, FileExistsError) as error:
                print(f"\nError: {error}")
                print("Introduce otro nombre.")
            except (OSError, UnicodeError) as error:
                print(f"\nError al guardar: {error}")
                print("El destino no se ha publicado incompleto.")
                return "menu"
            except KeyboardInterrupt:
                print("\nOperación cancelada. El archivo temporal se ha limpiado.")
                return "menu"


def ejecutar_texto_manual(cifrar: bool) -> str:
    """Procesa mensajes introducidos manualmente, permitiendo retroceder."""
    while True:
        # :volver permite cifrar literalmente el texto "0".
        texto = input("\nIntroduce el mensaje (:volver para regresar): ")
        if texto.strip().lower() == ":volver":
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
                    if not texto.strip():
                        raise ArchivoSinTextoError(
                            "El archivo está vacío o solo contiene espacios en blanco."
                        )
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

            except (OSError, UnicodeError, ArchivoSinTextoError) as error:
                print(f"\nError al leer el archivo: {error}")
                break  # Volver a seleccionar otro archivo.

            def guardar(nombre: str) -> Path:
                # Los mensajes pequeños también utilizan guardado temporal.
                if tamano <= TAMANO_MAXIMO_PANTALLA:
                    return guardar_mensaje(nombre, resultado, cifrado=cifrar)

                # Los archivos grandes se cifran o descifran por bloques.
                print("\nProcesando archivo por bloques...")
                print("Pulsa Ctrl+C para cancelar sin dejar archivos incompletos.")

                try:
                    ruta = procesar_archivo_grande(
                        nombre_entrada,
                        nombre,
                        clave,
                        cifrar,
                        progreso=crear_indicador_progreso(),
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


def ofrecer_limpieza_temporales() -> None:
    """Ofrece eliminar temporales de más de 24 h, con confirmación."""
    temporales = detectar_temporales_antiguos()
    if not temporales:
        return
    print(f"\nSe encontraron {len(temporales)} archivos temporales antiguos.")
    print("Asegúrate de que no haya otra ejecución trabajando con ellos.")
    if input("¿Quieres eliminarlos? (s/N): ").strip().lower() != "s":
        return
    borrados = 0
    for ruta in temporales:
        try:
            if ruta.is_file() and not ruta.is_symlink():
                ruta.unlink()
                borrados += 1
        except OSError as error:
            print(f"No se pudo eliminar {ruta.name}: {error}")
    print(f"Temporales eliminados: {borrados}")


def main() -> None:
    """El programa solo se cierra con 0 desde el menú principal."""
    try:
        ofrecer_limpieza_temporales()
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
