"""Punto de entrada de la aplicación Vigenère de Hogwarts."""

import logging

from src.vigenere.cli import main
from src.vigenere.logging_config import configurar_logging

if __name__ == "__main__":
    # Configuramos el sistema de logs.
    try:
        configurar_logging()

    except OSError as error:
        # Si no se pueden crear los logs, la aplicación puede continuar.
        print(f"Aviso: no se pudo configurar el registro: {error}")

    else:
        # Registramos el inicio de la aplicación.
        logging.getLogger("vigenere").info("Aplicacion iniciada")

    try:
        # Ejecutamos la interfaz principal de Vigenère.
        main()

    finally:
        # Registramos la finalización de la aplicación.
        logger = logging.getLogger("vigenere")

        if logger.handlers:
            logger.info("Aplicacion finalizada")

        logging.shutdown()
