"""Configuración del sistema de logs de Vigenère."""

import logging
from logging.handlers import RotatingFileHandler
from pathlib import Path


def configurar_logging() -> None:
    """Configura el registro de eventos en un archivo."""

    # Creamos la carpeta donde se guardarán los logs.
    carpeta_logs = Path(__file__).resolve().parents[2] / "logs"
    carpeta_logs.mkdir(parents=True, exist_ok=True)

    # Obtenemos el logger específico de Vigenère.
    logger = logging.getLogger("vigenere")
    logger.setLevel(logging.INFO)
    logger.propagate = False

    # Evitamos añadir el mismo manejador varias veces.
    if any(isinstance(h, RotatingFileHandler) for h in logger.handlers):
        return

    # Creamos un archivo de log con rotación automática.
    manejador = RotatingFileHandler(
        carpeta_logs / "vigenere.log",
        maxBytes=1024 * 1024,
        backupCount=3,
        encoding="utf-8",
    )

    # Establecemos el formato de los registros.
    formato = logging.Formatter("%(asctime)s | %(levelname)s | %(message)s")

    manejador.setFormatter(formato)
    logger.addHandler(manejador)
