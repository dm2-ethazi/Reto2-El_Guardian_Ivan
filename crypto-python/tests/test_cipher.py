"""Tests unitarios del algoritmo Vigenère."""

import pytest

from src.vigenere.cipher import (
    cifrar_vigenere,
    descifrar_vigenere,
    preparar_clave,
    procesar_bloque,
    validar_clave,
)


def test_cifrado():
    """Comprueba el cifrado clásico de Vigenère."""
    resultado = cifrar_vigenere("ATTACKATDAWN", "LEMON")
    assert resultado == "LXFOPVEFRNHR"


def test_descifrado():
    """Comprueba que el descifrado recupera el mensaje."""
    resultado = descifrar_vigenere("LXFOPVEFRNHR", "LEMON")
    assert resultado == "ATTACKATDAWN"


def test_recuperacion_original():
    """Comprueba que cifrar y descifrar recupera el texto original."""
    texto = "Hola, señor. Año 2026: café y Ñ."

    cifrado = cifrar_vigenere(texto, "CLAVE")
    descifrado = descifrar_vigenere(cifrado, "CLAVE")

    assert descifrado == texto


def test_validar_clave():
    """Comprueba que la clave se convierte a mayúsculas."""
    assert validar_clave("clave") == "CLAVE"


@pytest.mark.parametrize(
    "clave",
    ["", "1234", "CLAVE!", "ESPAÑA", "CLÁVE"],
)
def test_claves_invalidas(clave):
    """Comprueba que las claves incorrectas son rechazadas."""
    with pytest.raises(ValueError):
        validar_clave(clave)


def test_preparar_clave():
    """Comprueba los desplazamientos numéricos de la clave."""
    assert preparar_clave("HOLA") == (7, 14, 11, 0)


def test_procesamiento_por_bloques():
    """Comprueba que la posición de la clave continúa entre bloques."""
    bloque1, posicion = procesar_bloque("HOLA, ", "CLAVE", True)

    bloque2, posicion = procesar_bloque("MUNDO!", "CLAVE", True, posicion)

    resultado = bloque1 + bloque2

    assert resultado == cifrar_vigenere("HOLA, MUNDO!", "CLAVE")
    assert posicion == 9


def test_posicion_negativa():
    """Comprueba que se rechaza una posición inicial negativa."""
    with pytest.raises(ValueError):
        procesar_bloque("HOLA", "CLAVE", True, -1)
