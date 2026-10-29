"""Gestión segura de archivos .txt para el módulo Vigenère."""

import errno
import os
import shutil
import tempfile
import time
from collections.abc import Callable
from pathlib import Path

from .cipher import preparar_clave, procesar_bloque

# Ruta principal de crypto-python y carpetas preexistentes del proyecto.
RUTA_PROYECTO = Path(__file__).resolve().parents[2]
CARPETA_ENCRIPTADOS = RUTA_PROYECTO / "mensajes_encriptados"
CARPETA_DESENCRIPTADOS = RUTA_PROYECTO / "mensajes_desencriptados"

# Aproximadamente 1 MiB de caracteres (UTF-8 puede usar más bytes).
TAMANO_BLOQUE = 1024 * 1024
PREFIJO_TEMPORAL = ".vigenere_"
SUFIJO_TEMPORAL = ".tmp"

# Nombres de dispositivos reservados en Windows; los bloqueamos en todos los SO.
NOMBRES_RESERVADOS = frozenset(
    {"CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$"}
    | {f"COM{n}" for n in range(1, 10)}
    | {f"LPT{n}" for n in range(1, 10)}
    | {f"COM{n}" for n in "¹²³"}
    | {f"LPT{n}" for n in "¹²³"}
)
CARACTERES_PROHIBIDOS = '<>:"/\\|?*'


class ArchivoSinTextoError(ValueError):
    """El contenido del archivo no contiene texto utilizable."""


def validar_nombre(nombre: str) -> str:
    """Acepta un nombre simple .txt, portable entre Linux y Windows.

    No permite rutas completas, nombres reservados, caracteres de control ni
    extensiones distintas de .txt. Si no hay extensión, añade .txt.
    """
    if not isinstance(nombre, str):
        raise ValueError("Introduce un nombre de archivo válido.")

    # Admitimos que el usuario pegue accidentalmente el nombre entre comillas.
    nombre = nombre.strip()
    if nombre.startswith('"') and nombre.endswith('"') and len(nombre) >= 2:
        nombre = nombre[1:-1].strip()

    if not nombre or nombre in (".", ".."):
        raise ValueError("Introduce un nombre de archivo válido.")
    if any(c in CARACTERES_PROHIBIDOS or ord(c) < 32 or ord(c) == 127 for c in nombre):
        raise ValueError("El nombre contiene caracteres no permitidos.")
    if nombre.endswith((" ", ".")):
        raise ValueError("El nombre no puede terminar en punto o espacio.")
    if nombre == ".txt":
        raise ValueError("El nombre debe tener texto antes de la extensión .txt.")

    archivo = Path(nombre)
    if not archivo.suffix:
        nombre += ".txt"
    elif archivo.suffix.lower() != ".txt":
        raise ValueError("Solo se permiten archivos .txt.")

    # Windows también reserva CON.ext, COM1.ext, etc.
    base = nombre.split(".", 1)[0].upper()
    if base in NOMBRES_RESERVADOS:
        raise ValueError("Ese nombre está reservado por Windows.")
    if len(nombre.encode("utf-8")) > 240:
        raise ValueError("El nombre del archivo es demasiado largo.")

    return nombre


def obtener_carpetas(cifrar: bool) -> tuple[Path, Path]:
    """Cifrar: desencriptados → encriptados; descifrar: al revés."""
    if cifrar:
        return CARPETA_DESENCRIPTADOS, CARPETA_ENCRIPTADOS
    return CARPETA_ENCRIPTADOS, CARPETA_DESENCRIPTADOS


def comprobar_entrada(ruta: Path) -> None:
    """Evita trabajar con enlaces simbólicos como archivos de entrada."""
    if ruta.is_symlink():
        raise ValueError("No se admiten enlaces simbólicos como archivo de entrada.")
    if not ruta.is_file():
        raise FileNotFoundError(f"No se encuentra el archivo: {ruta.name}")


def leer_mensaje(nombre: str, cifrar: bool) -> str:
    """Lee un .txt pequeño en UTF-8 sin convertir los saltos de línea."""
    nombre = validar_nombre(nombre)
    carpeta_entrada, _ = obtener_carpetas(cifrar)
    carpeta_entrada.mkdir(parents=True, exist_ok=True)
    archivo = carpeta_entrada / nombre
    comprobar_entrada(archivo)
    with archivo.open("r", encoding="utf-8", newline="") as fichero:
        return fichero.read()


def comprobar_espacio(carpeta_salida: Path, bytes_necesarios: int) -> None:
    """Advierte si el espacio libre medido es insuficiente para el resultado.

    Esta comprobación es preventiva: otros procesos podrían consumir espacio
    después de realizarla.
    """
    espacio_libre = shutil.disk_usage(carpeta_salida).free
    if espacio_libre < bytes_necesarios:
        raise OSError(
            errno.ENOSPC,
            f"No hay espacio suficiente en disco: se necesitan "
            f"{bytes_necesarios / (1024 ** 2):,.1f} MiB y hay "
            f"{espacio_libre / (1024 ** 2):,.1f} MiB libres.",
        )


def publicar_sin_sobrescribir(temporal: Path, destino: Path) -> None:
    """Publica un archivo completo sin reemplazar un destino existente.

    Windows: os.rename no permite reemplazar archivos existentes.
    POSIX: os.link crea un enlace de forma atómica y exclusiva; después el
    caller elimina el nombre temporal. Si el FS no soporta enlaces físicos,
    fallamos de forma explícita en lugar de copiar a un destino incompleto.
    """
    if os.name == "nt":
        os.rename(temporal, destino)
        return

    try:
        os.link(temporal, destino)
    except FileExistsError:
        raise FileExistsError(f"El archivo de salida ya existe: {destino.name}") from None
    except OSError as error:
        if error.errno in (errno.EPERM, errno.EOPNOTSUPP, errno.ENOTSUP, errno.EXDEV):
            raise OSError(
                error.errno,
                "El sistema de archivos no admite publicación segura "
                "sin sobrescritura. Utiliza una carpeta en un disco local "
                "compatible con enlaces físicos.",
            ) from error
        raise


def _crear_temporal(carpeta_salida: Path) -> tuple[Path, object]:
    """Crea un archivo temporal UTF-8, en la misma carpeta del resultado."""
    fichero = tempfile.NamedTemporaryFile(
        mode="w", encoding="utf-8", newline="", dir=carpeta_salida,
        prefix=PREFIJO_TEMPORAL, suffix=SUFIJO_TEMPORAL, delete=False,
    )
    return Path(fichero.name), fichero


def guardar_mensaje(nombre: str, texto: str, cifrado: bool) -> Path:
    """Guarda un mensaje pequeño sin publicar archivos incompletos."""
    nombre = validar_nombre(nombre)
    _, carpeta_salida = obtener_carpetas(cifrado)
    carpeta_salida.mkdir(parents=True, exist_ok=True)
    destino = carpeta_salida / nombre

    if destino.exists() or destino.is_symlink():
        raise FileExistsError(f"El archivo de salida ya existe: {nombre}")
    comprobar_espacio(carpeta_salida, len(texto.encode("utf-8")))

    temporal = None
    try:
        temporal, fichero = _crear_temporal(carpeta_salida)
        with fichero:
            fichero.write(texto)
        publicar_sin_sobrescribir(temporal, destino)
        return destino
    finally:
        if temporal is not None:
            temporal.unlink(missing_ok=True)


def procesar_archivo_grande(
    nombre_entrada: str,
    nombre_salida: str,
    clave: str,
    cifrar: bool,
    progreso: Callable[[int, int], None] | None = None,
) -> Path:
    """Cifra/descifra un .txt por bloques, con uso acotado de memoria.

    La clave se prepara una vez, se conserva su posición entre bloques y
    una entrada en blanco o un error no genera un archivo definitivo.
    """
    nombre_entrada = validar_nombre(nombre_entrada)
    nombre_salida = validar_nombre(nombre_salida)
    desplazamientos = preparar_clave(clave)

    carpeta_entrada, carpeta_salida = obtener_carpetas(cifrar)
    carpeta_salida.mkdir(parents=True, exist_ok=True)
    archivo_entrada = carpeta_entrada / nombre_entrada
    archivo_salida = carpeta_salida / nombre_salida
    comprobar_entrada(archivo_entrada)

    if archivo_salida.exists() or archivo_salida.is_symlink():
        raise FileExistsError(f"El archivo de salida ya existe: {nombre_salida}")

    tamano_total = archivo_entrada.stat().st_size
    if tamano_total == 0:
        raise ArchivoSinTextoError("El archivo de entrada está vacío.")
    # En UTF-8, cifrar exclusivamente letras ASCII conserva el total de bytes.
    comprobar_espacio(carpeta_salida, tamano_total)

    posicion_clave = 0
    bytes_procesados = 0
    contiene_texto = False
    temporal = None

    try:
        with archivo_entrada.open("r", encoding="utf-8", newline="") as entrada:
            temporal, salida = _crear_temporal(carpeta_salida)
            with salida:
                while True:
                    bloque = entrada.read(TAMANO_BLOQUE)
                    if not bloque:
                        break

                    # La comprobación de contenido se hace durante la lectura,
                    # evitando recorrer el archivo entero una segunda vez.
                    if not contiene_texto and bloque.strip():
                        contiene_texto = True

                    resultado, posicion_clave = procesar_bloque(
                        bloque, clave, cifrar, posicion_clave,
                        desplazamientos=desplazamientos,
                    )
                    salida.write(resultado)
                    bytes_procesados += len(bloque.encode("utf-8"))
                    if progreso is not None:
                        progreso(bytes_procesados, tamano_total)

        if not contiene_texto:
            raise ArchivoSinTextoError(
                "El archivo está vacío o solo contiene espacios en blanco."
            )

        publicar_sin_sobrescribir(temporal, archivo_salida)
        return archivo_salida
    finally:
        # Finalmente eliminamos el nombre temporal, incluso si se cancela
        # la operación (Ctrl+C) o hay un error de decodificación/escritura.
        if temporal is not None:
            temporal.unlink(missing_ok=True)


def detectar_temporales_antiguos(horas: int = 24) -> list[Path]:
    """Devuelve temporales antiguos para ofrecer limpieza MANUAL al arrancar.

    No elimina nada automáticamente para no interferir en otra ejecución.
    """
    antiguos = []
    limite = time.time() - horas * 3600
    for carpeta in (CARPETA_ENCRIPTADOS, CARPETA_DESENCRIPTADOS):
        if not carpeta.is_dir():
            continue
        for ruta in carpeta.glob(f"{PREFIJO_TEMPORAL}*{SUFIJO_TEMPORAL}"):
            try:
                if ruta.is_file() and not ruta.is_symlink() and ruta.stat().st_mtime < limite:
                    antiguos.append(ruta)
            except OSError:
                pass
    return antiguos
