"""Pruebas unitarias de la gestión de archivos Vigenère."""

import errno
import os
import time
from types import SimpleNamespace

import pytest

from src.vigenere import files


@pytest.fixture
def carpetas_temporales(tmp_path, monkeypatch):
    """Aísla todas las operaciones de las carpetas reales de la aplicación."""
    desencriptados = tmp_path / "mensajes_desencriptados"
    encriptados = tmp_path / "mensajes_encriptados"
    desencriptados.mkdir()
    encriptados.mkdir()

    monkeypatch.setattr(files, "CARPETA_DESENCRIPTADOS", desencriptados)
    monkeypatch.setattr(files, "CARPETA_ENCRIPTADOS", encriptados)
    return desencriptados, encriptados


@pytest.mark.parametrize(
    "nombre, esperado",
    [
        ("mensaje", "mensaje.txt"),
        ("mensaje.txt", "mensaje.txt"),
        ("MENSAJE.TXT", "MENSAJE.TXT"),
    ],
)
def test_validar_nombres_correctos(nombre, esperado):
    assert files.validar_nombre(nombre) == esperado


@pytest.mark.parametrize(
    "nombre",
    [
        "",
        ".txt",
        "CON.txt",
        "COM1",
        "archivo.pdf",
        "carpeta/archivo.txt",
        "mal?.txt",
        "mensaje.",
        "archivo\x00.txt",
    ],
)
def test_rechazar_nombres_invalidos(nombre):
    with pytest.raises(ValueError):
        files.validar_nombre(nombre)


def test_guardar_y_leer_mensaje(carpetas_temporales):
    _, encriptados = carpetas_temporales
    destino = files.guardar_mensaje("mensaje", "Hola, señor!", cifrado=True)

    assert destino == encriptados / "mensaje.txt"
    assert destino.read_text(encoding="utf-8") == "Hola, señor!"
    assert files.leer_mensaje("mensaje", cifrar=False) == "Hola, señor!"


def test_no_sobrescribir_archivo(carpetas_temporales):
    _, encriptados = carpetas_temporales
    files.guardar_mensaje("existente", "ORIGINAL", cifrado=True)

    with pytest.raises(FileExistsError):
        files.guardar_mensaje("existente", "CAMBIADO", cifrado=True)

    assert (encriptados / "existente.txt").read_text() == "ORIGINAL"


def test_guardado_no_deja_temporales(carpetas_temporales):
    _, encriptados = carpetas_temporales
    files.guardar_mensaje("correcto", "Texto", cifrado=True)

    assert list(encriptados.glob(".vigenere_*.tmp")) == []


def test_lectura_archivo_inexistente(carpetas_temporales):
    with pytest.raises(FileNotFoundError):
        files.leer_mensaje("no_existe", cifrar=True)


def test_cifrar_y_descifrar_por_bloques(carpetas_temporales, monkeypatch):
    desencriptados, encriptados = carpetas_temporales
    monkeypatch.setattr(files, "TAMANO_BLOQUE", 7)
    texto = "Hola, señor!\r\nAño 2026\nÚltima línea 😀"
    original = desencriptados / "original.txt"
    original.write_bytes(texto.encode("utf-8"))

    progreso = []
    files.procesar_archivo_grande(
        "original.txt",
        "cifrado.txt",
        "CLAVE",
        True,
        progreso=lambda procesados, total: progreso.append((procesados, total)),
    )
    files.procesar_archivo_grande("cifrado.txt", "recuperado.txt", "CLAVE", False)

    assert (desencriptados / "recuperado.txt").read_bytes() == original.read_bytes()
    assert (encriptados / "cifrado.txt").read_bytes() != original.read_bytes()
    assert progreso[-1] == (original.stat().st_size, original.stat().st_size)


def test_rechazar_archivo_vacio(carpetas_temporales):
    desencriptados, encriptados = carpetas_temporales
    (desencriptados / "vacio.txt").write_bytes(b"")

    with pytest.raises(files.ArchivoSinTextoError):
        files.procesar_archivo_grande("vacio.txt", "salida.txt", "CLAVE", True)

    assert not (encriptados / "salida.txt").exists()


def test_rechazar_archivo_solo_espacios(carpetas_temporales):
    desencriptados, encriptados = carpetas_temporales
    (desencriptados / "espacios.txt").write_text(" \n\t  ")

    with pytest.raises(files.ArchivoSinTextoError):
        files.procesar_archivo_grande("espacios.txt", "salida.txt", "CLAVE", True)

    assert not (encriptados / "salida.txt").exists()
    assert not list(encriptados.glob(".vigenere_*.tmp"))


def test_archivo_corrupto_no_deja_salida(carpetas_temporales):
    desencriptados, encriptados = carpetas_temporales
    (desencriptados / "corrupto.txt").write_bytes(b"Hola" + b"\xff")

    with pytest.raises(UnicodeDecodeError):
        files.procesar_archivo_grande("corrupto.txt", "salida.txt", "CLAVE", True)

    assert not (encriptados / "salida.txt").exists()
    assert not list(encriptados.glob(".vigenere_*.tmp"))


def test_cancelacion_limpia_temporales(carpetas_temporales, monkeypatch):
    desencriptados, encriptados = carpetas_temporales
    monkeypatch.setattr(files, "TAMANO_BLOQUE", 4)
    (desencriptados / "largo.txt").write_text("Hola mundo" * 30)

    def cancelar(_procesados, _total):
        raise KeyboardInterrupt

    with pytest.raises(KeyboardInterrupt):
        files.procesar_archivo_grande(
            "largo.txt", "cancelado.txt", "CLAVE", True, progreso=cancelar
        )

    assert not (encriptados / "cancelado.txt").exists()
    assert not list(encriptados.glob(".vigenere_*.tmp"))


def test_comprobar_espacio_insuficiente(carpetas_temporales, monkeypatch):
    _, encriptados = carpetas_temporales
    monkeypatch.setattr(
        files.shutil, "disk_usage", lambda _carpeta: SimpleNamespace(free=10)
    )

    with pytest.raises(OSError) as error:
        files.comprobar_espacio(encriptados, 11)

    assert error.value.errno == errno.ENOSPC


def test_detectar_temporales_antiguos(carpetas_temporales):
    _, encriptados = carpetas_temporales
    antiguo = encriptados / ".vigenere_antiguo.tmp"
    reciente = encriptados / ".vigenere_reciente.tmp"
    antiguo.write_text("antiguo")
    reciente.write_text("reciente")
    fecha_antigua = time.time() - 25 * 3600
    os.utime(antiguo, (fecha_antigua, fecha_antigua))

    encontrados = files.detectar_temporales_antiguos(horas=24)

    assert antiguo in encontrados
    assert reciente not in encontrados
