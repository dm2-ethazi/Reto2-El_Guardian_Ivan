"""Tests unitarios del sistema de logs de Vigenere."""

import logging
from logging.handlers import RotatingFileHandler

import pytest

from src.vigenere import cli, logging_config


@pytest.fixture
def logger_temporal(tmp_path, monkeypatch):
    """Configura los logs en una carpeta temporal de Pytest."""

    # Simulamos la estructura del proyecto en una carpeta temporal.
    ruta_simulada = tmp_path / "proyecto" / "src" / "vigenere" / "logging_config.py"

    monkeypatch.setattr(logging_config, "__file__", str(ruta_simulada))

    # Guardamos la configuracion anterior del logger.
    logger = logging.getLogger("vigenere")

    handlers_originales = list(logger.handlers)
    nivel_original = logger.level
    propagacion_original = logger.propagate

    # Retiramos los manejadores existentes para aislar cada test.
    for handler in handlers_originales:
        logger.removeHandler(handler)

    # Configuramos el sistema en la carpeta temporal.
    logging_config.configurar_logging()

    ruta_log = tmp_path / "proyecto" / "logs" / "vigenere.log"

    yield logger, ruta_log

    # Cerramos los manejadores creados durante el test.
    for handler in list(logger.handlers):
        logger.removeHandler(handler)
        handler.close()

    # Restauramos la configuracion anterior.
    for handler in handlers_originales:
        logger.addHandler(handler)

    logger.setLevel(nivel_original)
    logger.propagate = propagacion_original


def test_creacion_archivo_log(logger_temporal):
    """Comprueba que el archivo de logs se crea correctamente."""

    logger, ruta_log = logger_temporal

    assert ruta_log.exists()
    assert logger.level == logging.INFO
    assert logger.propagate is False


def test_registro_eventos(logger_temporal):
    """Comprueba que los eventos se escriben en el archivo."""

    logger, ruta_log = logger_temporal

    # Registramos un evento de prueba.
    logger.info("Inicio de cifrado")

    # Leemos el contenido del archivo generado.
    contenido = ruta_log.read_text(encoding="utf-8")

    assert "INFO" in contenido
    assert "Inicio de cifrado" in contenido


def test_no_duplicar_handlers(logger_temporal):
    """Comprueba que no se crean manejadores duplicados."""

    logger, _ = logger_temporal

    # Llamamos varias veces a la configuracion.
    logging_config.configurar_logging()
    logging_config.configurar_logging()

    # Solo debe existir un manejador de archivo.
    handlers = [h for h in logger.handlers if isinstance(h, RotatingFileHandler)]

    assert len(handlers) == 1


def test_rotacion_logs(logger_temporal):
    """Comprueba que se crean copias al alcanzar el limite."""

    logger, ruta_log = logger_temporal

    handler = logger.handlers[0]

    # Reducimos el limite para probar la rotacion rapidamente.
    handler.maxBytes = 200

    # Generamos suficientes eventos para superar el limite.
    for numero in range(40):
        logger.info(
            "Evento de prueba numero %d con informacion adicional",
            numero,
        )

    # Debe existir una copia del archivo anterior.
    assert ruta_log.with_name("vigenere.log.1").exists()

    # Nunca debe superar el numero de copias configurado.
    archivos = list(ruta_log.parent.glob("vigenere.log*"))

    assert len(archivos) <= 4


def test_no_registrar_informacion_sensible(logger_temporal, monkeypatch):
    """Comprueba que los logs no almacenan el texto ni la clave."""

    _, ruta_log = logger_temporal

    texto = "MENSAJE PRIVADO 1234"
    clave = "CLAVEPRIVADA"

    # Simulamos la entrada del usuario.
    monkeypatch.setattr("builtins.input", lambda _mensaje="": texto)

    monkeypatch.setattr(cli, "obtener_clave", lambda: clave)

    # Evitamos guardar el resultado en un archivo real.
    monkeypatch.setattr(cli, "preguntar_guardado", lambda _guardar: "menu")

    assert cli.ejecutar_texto_manual(True) == "menu"

    contenido = ruta_log.read_text(encoding="utf-8")

    # Deben registrarse los eventos, pero no los datos sensibles.
    assert "Inicio de cifrado de texto manual" in contenido
    assert "Fin de cifrado de texto manual" in contenido

    assert texto not in contenido
    assert clave not in contenido


def test_registrar_cancelacion(logger_temporal, monkeypatch):
    """Comprueba que Ctrl+C genera un registro WARNING."""

    _, ruta_log = logger_temporal

    respuestas = iter(["s", "salida"])

    # Simulamos la seleccion de guardar.
    monkeypatch.setattr(
        "builtins.input",
        lambda _mensaje="": next(respuestas),
    )

    # Simulamos una cancelacion durante el guardado.
    def guardar(_nombre):
        raise KeyboardInterrupt

    assert cli.preguntar_guardado(guardar) == "menu"

    contenido = ruta_log.read_text(encoding="utf-8")

    assert "WARNING" in contenido
    assert "Operacion cancelada por el usuario" in contenido
