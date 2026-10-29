import os
import tempfile
from pathlib import Path

from .cipher import procesar_bloque, validar_clave
from collections.abc import Callable

# Ruta principal de crypto-python.
RUTA_PROYECTO = Path(__file__).resolve().parents[2]

# Carpetas de entrada y salida.
CARPETA_ENCRIPTADOS = RUTA_PROYECTO / "mensajes_encriptados"
CARPETA_DESENCRIPTADOS = RUTA_PROYECTO / "mensajes_desencriptados"

# Cada bloque tendrá aproximadamente 1 MiB de texto.
TAMANO_BLOQUE = 1024 * 1024


def validar_nombre(nombre: str) -> str:
    """
    Valida el nombre del archivo y añade .txt automáticamente
    si el usuario no ha indicado una extensión.
    """

    # Eliminamos espacios y comillas innecesarias.
    nombre = nombre.strip().strip('"').strip()

    # No permitimos nombres vacíos ni rutas completas.
    if (
        not nombre
        or nombre in (".", "..")
        or "/" in nombre
        or "\\" in nombre
        or ":" in nombre
    ):
        raise ValueError("Introduce un nombre de archivo válido.")

    archivo = Path(nombre)

    # Si no tiene extensión, añadimos .txt automáticamente.
    if not archivo.suffix:
        nombre += ".txt"

    # Solo permitimos archivos .txt.
    elif archivo.suffix.lower() != ".txt":
        raise ValueError("Solo se permiten archivos .txt.")

    return nombre


def obtener_carpetas(cifrar: bool) -> tuple[Path, Path]:
    """
    Devuelve las carpetas de entrada y salida.

    Cifrar:
        desencriptados -> encriptados

    Descifrar:
        encriptados -> desencriptados
    """

    if cifrar:
        return CARPETA_DESENCRIPTADOS, CARPETA_ENCRIPTADOS

    return CARPETA_ENCRIPTADOS, CARPETA_DESENCRIPTADOS


def leer_mensaje(nombre: str, cifrar: bool) -> str:
    """
    Lee un archivo .txt completo.

    Se mantiene para trabajar con mensajes pequeños.
    """

    nombre = validar_nombre(nombre)

    carpeta_entrada, _ = obtener_carpetas(cifrar)
    carpeta_entrada.mkdir(parents=True, exist_ok=True)

    archivo = carpeta_entrada / nombre

    with archivo.open(
        "r", encoding="utf-8", newline=""
    ) as fichero:
        return fichero.read()


def guardar_mensaje(nombre: str, texto: str, cifrado: bool) -> Path:
    """
    Guarda un mensaje pequeño en su carpeta correspondiente.

    No sobrescribe archivos existentes.
    """

    nombre = validar_nombre(nombre)

    _, carpeta_salida = obtener_carpetas(cifrado)
    carpeta_salida.mkdir(parents=True, exist_ok=True)

    archivo = carpeta_salida / nombre

    with archivo.open(
        "x", encoding="utf-8", newline=""
    ) as fichero:
        fichero.write(texto)

    return archivo


def procesar_archivo_grande(
    nombre_entrada: str,
    nombre_salida: str,
    clave: str,
    cifrar: bool,
    progreso: Callable[[int, int], None] | None = None,
) -> Path:
    """
    Cifra o descifra un archivo .txt por bloques.

    No carga el archivo completo en memoria.
    Conserva la posición de la clave entre bloques.
    """

    # Validamos los nombres y la clave antes de empezar.
    nombre_entrada = validar_nombre(nombre_entrada)
    nombre_salida = validar_nombre(nombre_salida)
    clave = validar_clave(clave)

    # Elegimos automáticamente las carpetas.
    carpeta_entrada, carpeta_salida = obtener_carpetas(cifrar)

    carpeta_salida.mkdir(parents=True, exist_ok=True)

    archivo_entrada = carpeta_entrada / nombre_entrada
    archivo_salida = carpeta_salida / nombre_salida

    # Comprobamos que existe el archivo de entrada.
    if not archivo_entrada.is_file():
        raise FileNotFoundError(
            f"No se encuentra el archivo: {nombre_entrada}"
        )

    # Evitamos sobrescribir archivos existentes.
    if archivo_salida.exists():
        raise FileExistsError(
            f"El archivo de salida ya existe: {nombre_salida}"
        )

    # Conserva la posición de la clave entre los bloques.
    posicion_clave = 0

    # Tamaño total del archivo de entrada, en bytes.
    tamano_total = archivo_entrada.stat().st_size

    # Número de bytes que ya hemos procesado.
    bytes_procesados = 0

    # Guardaremos primero el resultado en un archivo temporal.
    archivo_temporal = None

    try:
        with archivo_entrada.open(
            "r", encoding="utf-8", newline=""
        ) as entrada:

            with tempfile.NamedTemporaryFile(
                mode="w",
                encoding="utf-8",
                newline="",
                dir=carpeta_salida,
                prefix=".vigenere_",
                suffix=".tmp",
                delete=False,
            ) as salida:

                archivo_temporal = Path(salida.name)

                # Leemos el archivo de forma progresiva.
                while True:
                    bloque = entrada.read(TAMANO_BLOQUE)

                    # Si no quedan caracteres, terminamos.
                    if not bloque:
                        break

                    # Procesamos el bloque conservando
                    # la posición actual de la clave.
                    resultado, posicion_clave = procesar_bloque(
                        bloque,
                        clave,
                        cifrar,
                        posicion_clave,
                    )

                    # Guardamos el bloque procesado.
                    salida.write(resultado)

                    # Contamos los bytes originales procesados.
                    # UTF-8 puede utilizar varios bytes por carácter.
                    bytes_procesados += len(bloque.encode("utf-8"))

                    # Notificamos el progreso al menú.
                    if progreso is not None:
                        progreso(bytes_procesados, tamano_total)

        # Publicamos el resultado solo cuando termina
        # correctamente todo el procesamiento.
        # os.link no sobrescribe un destino existente.
        os.link(archivo_temporal, archivo_salida)

        return archivo_salida

    finally:
        # Eliminamos el temporal tanto si la operación
        # termina correctamente como si ocurre un error.
        if archivo_temporal is not None:
            archivo_temporal.unlink(missing_ok=True)
