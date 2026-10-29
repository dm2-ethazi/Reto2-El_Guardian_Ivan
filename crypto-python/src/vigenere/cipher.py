def validar_clave(clave: str) -> str:
    """Comprueba que la clave contenga únicamente letras A-Z."""

    if not clave or not clave.isascii() or not clave.isalpha():
        raise ValueError("La clave debe contener únicamente letras de A a Z.")

    return clave.upper()


def procesar_bloque(
    texto: str,
    clave: str,
    cifrar: bool,
    posicion_clave: int = 0,
) -> tuple[str, int]:
    """
    Cifra o descifra un bloque de texto mediante Vigenère.

    Devuelve:
    - El texto procesado.
    - La posición de la clave para el siguiente bloque.
    """

    # Validamos la clave y la posición inicial.
    clave = validar_clave(clave)

    if posicion_clave < 0:
        raise ValueError("La posición de la clave no puede ser negativa.")

    resultado = []

    # Recorremos los caracteres del bloque.
    for caracter in texto:

        # Solo procesamos las letras ASCII A-Z y a-z.
        if "A" <= caracter <= "Z" or "a" <= caracter <= "z":

            # Seleccionamos la letra correspondiente de la clave.
            indice = posicion_clave % len(clave)

            # Calculamos el desplazamiento entre 0 y 25.
            desplazamiento = ord(clave[indice]) - ord("A")

            # Para descifrar, restamos el desplazamiento.
            if not cifrar:
                desplazamiento = -desplazamiento

            # Conservamos mayúsculas y minúsculas.
            base = ord("A") if caracter.isupper() else ord("a")

            # Aplicamos el algoritmo Vigenère.
            nuevo_caracter = chr(
                (ord(caracter) - base + desplazamiento) % 26 + base
            )

            resultado.append(nuevo_caracter)

            # Avanzamos la clave solamente con letras A-Z.
            posicion_clave += 1

        else:
            # Espacios, números, ñ y tildes no se modifican.
            resultado.append(caracter)

    # Devolvemos el bloque y la posición para continuar.
    return "".join(resultado), posicion_clave


def procesar_vigenere(texto: str, clave: str, cifrar: bool) -> str:
    """Procesa un mensaje completo mediante Vigenère."""

    resultado, _ = procesar_bloque(texto, clave, cifrar)
    return resultado


def cifrar_vigenere(texto: str, clave: str) -> str:
    """Cifra un mensaje mediante Vigenère."""

    return procesar_vigenere(texto, clave, True)


def descifrar_vigenere(texto: str, clave: str) -> str:
    """Descifra un mensaje mediante Vigenère."""

    return procesar_vigenere(texto, clave, False)
