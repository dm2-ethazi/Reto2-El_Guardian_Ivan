"""Implementación didáctica de Vigenère sobre letras ASCII A-Z/a-z.

Los espacios, números, tildes y símbolos se conservan y NO avanzan la clave.
No es un cifrado adecuado para proteger datos sensibles reales.
"""


def validar_clave(clave: str) -> str:
    """Comprueba que la clave contenga únicamente letras A-Z."""
    if not clave or not clave.isascii() or not clave.isalpha():
        raise ValueError("La clave debe contener únicamente letras de A a Z.")
    return clave.upper()


def preparar_clave(clave: str) -> tuple[int, ...]:
    """Valida la clave y calcula una sola vez sus desplazamientos (0-25)."""
    clave = validar_clave(clave)
    return tuple(ord(letra) - ord("A") for letra in clave)


def procesar_bloque(
    texto: str,
    clave: str,
    cifrar: bool,
    posicion_clave: int = 0,
    desplazamientos: tuple[int, ...] | None = None,
) -> tuple[str, int]:
    """Cifra/descifra un bloque y devuelve la posición absoluta de la clave.

    Para varios bloques, pasa los ``desplazamientos`` preparados una sola vez
    y la ``posicion_clave`` devuelta por el bloque anterior.

    El parámetro opcional preserva la compatibilidad con las llamadas antiguas.
    """
    if posicion_clave < 0:
        raise ValueError("La posición de la clave no puede ser negativa.")

    if desplazamientos is None:
        desplazamientos = preparar_clave(clave)
    if not desplazamientos:
        raise ValueError("La clave no puede estar vacía.")

    longitud_clave = len(desplazamientos)
    indice = posicion_clave % longitud_clave
    resultado = []

    for caracter in texto:
        # Solo ciframos letras del alfabeto latino básico, conservando el caso.
        if "A" <= caracter <= "Z":
            base = 65
        elif "a" <= caracter <= "z":
            base = 97
        else:
            resultado.append(caracter)
            continue

        desplazamiento = desplazamientos[indice]
        if not cifrar:
            desplazamiento = -desplazamiento

        resultado.append(chr((ord(caracter) - base + desplazamiento) % 26 + base))
        posicion_clave += 1

        # Avanzar circularmente evita calcular el módulo en cada carácter.
        indice += 1
        if indice == longitud_clave:
            indice = 0

    return "".join(resultado), posicion_clave


def procesar_vigenere(texto: str, clave: str, cifrar: bool) -> str:
    """Procesa un mensaje completo mediante Vigenère."""
    desplazamientos = preparar_clave(clave)
    resultado, _ = procesar_bloque(
        texto, clave, cifrar, desplazamientos=desplazamientos
    )
    return resultado


def cifrar_vigenere(texto: str, clave: str) -> str:
    """Cifra un mensaje mediante Vigenère."""
    return procesar_vigenere(texto, clave, True)


def descifrar_vigenere(texto: str, clave: str) -> str:
    """Descifra un mensaje mediante Vigenère."""
    return procesar_vigenere(texto, clave, False)
