"""Tests unitarios de la interfaz de consola del módulo Vigenère."""

from pathlib import Path

import pytest

from src.vigenere import cli, files


@pytest.fixture
def carpetas_temporales(tmp_path, monkeypatch):
    """Redirige los archivos a carpetas temporales creadas por Pytest."""
    # Cada test recibe carpetas nuevas: nunca accedemos a los datos reales.
    desencriptados = tmp_path / "mensajes_desencriptados"
    encriptados = tmp_path / "mensajes_encriptados"
    desencriptados.mkdir()
    encriptados.mkdir()
    # files.py usará estas rutas solo durante el test; después se restauran.
    monkeypatch.setattr(files, "CARPETA_DESENCRIPTADOS", desencriptados)
    monkeypatch.setattr(files, "CARPETA_ENCRIPTADOS", encriptados)
    return desencriptados, encriptados


@pytest.fixture
def simular_entrada(monkeypatch):
    """Simula las respuestas que una persona escribiría con el teclado."""

    def configurar(*respuestas):
        # iter() hace que cada llamada a input() reciba la siguiente respuesta.
        entradas = iter(respuestas)
        # Sustituimos input() por una función que nunca espera al teclado.
        monkeypatch.setattr("builtins.input", lambda _mensaje="": next(entradas))

    return configurar

# --------------------------------------------------------------------------
# 1. ENTRADA DE DATOS Y MENÚ PRINCIPAL
# --------------------------------------------------------------------------

@pytest.mark.parametrize(
    ("entrada", "esperado"),
    [("0", None), (" 0 ", None), ("mensaje", "mensaje")],
)
def test_pedir_dato(simular_entrada, entrada, esperado):
    """El valor 0 sirve para volver; los demás datos se devuelven intactos."""
    simular_entrada(entrada)
    assert cli.pedir_dato("Dato: ") == esperado


def test_mostrar_menu(capsys):
    """El menú principal presenta las opciones de cifrado, descifrado y salida."""
    cli.mostrar_menu()
    # capsys permite leer lo que se ha impreso en pantalla.
    salida = capsys.readouterr().out
    assert "1. Cifrar mensaje" in salida
    assert "2. Descifrar mensaje" in salida
    assert "0. Salir" in salida

# --------------------------------------------------------------------------
# 2. INTRODUCCIÓN Y VALIDACIÓN DE CLAVES
# --------------------------------------------------------------------------
def test_obtener_clave_valida(monkeypatch):
    """La clave válida se normaliza a mayúsculas y se recortan espacios."""
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: "  cLaVe ")
    assert cli.obtener_clave() == "CLAVE"


def test_obtener_clave_reintenta_si_es_invalida(monkeypatch, capsys):
    """Tras una clave incorrecta se informa del error y se vuelve a pedir."""
    # Primera clave inválida, segunda válida: el usuario puede reintentar.
    entradas = iter(["1234", "clave"])
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: next(entradas))
    assert cli.obtener_clave() == "CLAVE"
    assert "Introduce otra clave" in capsys.readouterr().out


def test_obtener_clave_permite_volver(monkeypatch):
    """Introducir 0 al pedir la clave devuelve None para retroceder."""
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: "0")
    assert cli.obtener_clave() is None

# --------------------------------------------------------------------------
# 3. SELECCIÓN DE ARCHIVOS DE ENTRADA
# --------------------------------------------------------------------------
def test_nombre_entrada_valido(carpetas_temporales, simular_entrada):
    """Se encuentra un .txt existente y se añade la extensión si falta."""
    desencriptados, _ = carpetas_temporales
    (desencriptados / "entrada.txt").write_text("HOLA", encoding="utf-8")
    simular_entrada("entrada")
    assert cli.obtener_nombre_entrada(cifrar=True) == "entrada.txt"


def test_nombre_entrada_inexistente_y_luego_valido(
    carpetas_temporales, simular_entrada, capsys
):
    """Ante un archivo inexistente se muestra el error y se reintenta."""
    desencriptados, _ = carpetas_temporales
    (desencriptados / "valido.txt").write_text("HOLA", encoding="utf-8")
    simular_entrada("no_existe", "valido")
    assert cli.obtener_nombre_entrada(cifrar=True) == "valido.txt"
    assert "No existe el archivo" in capsys.readouterr().out


def test_nombre_entrada_vacio_y_luego_volver(
    carpetas_temporales, simular_entrada, capsys
):
    """Un archivo sin bytes se rechaza y después es posible retroceder."""
    desencriptados, _ = carpetas_temporales
    (desencriptados / "vacio.txt").write_bytes(b"")
    simular_entrada("vacio", "0")
    assert cli.obtener_nombre_entrada(cifrar=True) is None
    assert "está vacío" in capsys.readouterr().out

# --------------------------------------------------------------------------
# 4. MENÚ DE GUARDADO Y TRATAMIENTO DE ERRORES
# --------------------------------------------------------------------------

@pytest.mark.parametrize("respuesta", ["n", "N"])
def test_guardado_no_guardar(simular_entrada, respuesta):
    """Elegir n o N no llama a la función que guarda el resultado."""
    simular_entrada(respuesta)

    # pytest.fail marca el test como fallido si se intenta guardar.
    def no_llamar(_nombre):
        pytest.fail("No debería intentar guardar")

    assert cli.preguntar_guardado(no_llamar) == "menu"


def test_guardado_volver(simular_entrada):
    """La opción 0 regresa al paso anterior y no guarda nada."""
    simular_entrada("0")
    assert cli.preguntar_guardado(lambda _nombre: Path("nunca.txt")) == "atras"


def test_guardado_reintenta_nombre_ya_existente(simular_entrada, capsys):
    """Si el destino existe, se solicita otro nombre y se consigue guardar."""
    simular_entrada("s", "existente", "nuevo")
    # Registramos los nombres pedidos para verificar que hubo un reintento.
    intentos = []

    def guardar(nombre):
        intentos.append(nombre)
        if nombre == "existente":
            raise FileExistsError("El archivo ya existe")
        return Path("nuevo.txt")

    assert cli.preguntar_guardado(guardar) == "menu"
    assert intentos == ["existente", "nuevo"]
    assert "Archivo guardado correctamente" in capsys.readouterr().out


@pytest.mark.parametrize(
    ("error", "mensaje"),
    [
        (files.ArchivoSinTextoError("sin texto"), "Error en el contenido"),
        (OSError("disco lleno"), "Error al guardar"),
        (KeyboardInterrupt(), "Operación cancelada"),
    ],
)
def test_guardado_controla_errores(simular_entrada, capsys, error, mensaje):
    """Los errores de contenido, disco y cancelación se controlan sin salir."""
    simular_entrada("s", "salida")

    def guardar(_nombre):
        raise error

    assert cli.preguntar_guardado(guardar) == "menu"
    assert mensaje in capsys.readouterr().out


def test_guardado_opcion_invalida_y_volver(simular_entrada, capsys):
    """Una opción desconocida no bloquea el menú de guardado."""
    simular_entrada("invalida", "s", "0", "0")
    assert cli.preguntar_guardado(lambda _nombre: Path("nunca.txt")) == "atras"
    assert "Opción no válida" in capsys.readouterr().out

# --------------------------------------------------------------------------
# 5. CIFRADO Y DESCIFRADO DE TEXTO MANUAL
# --------------------------------------------------------------------------
def test_texto_manual_cifrar_y_guardar(
    carpetas_temporales, simular_entrada, monkeypatch, capsys
):
    """Se cifra un texto introducido por teclado y se guarda en un .txt."""
    _, encriptados = carpetas_temporales
    simular_entrada("ATTACKATDAWN", "s", "cifrado")
    # Se simula la clave sin usar getpass(), que esperaría al teclado.
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: "LEMON")

    assert cli.ejecutar_texto_manual(cifrar=True) == "menu"
    assert (encriptados / "cifrado.txt").read_text() == "LXFOPVEFRNHR"
    assert "LXFOPVEFRNHR" in capsys.readouterr().out


def test_texto_manual_descifrar_sin_guardar(simular_entrada, monkeypatch, capsys):
    """Se recupera el texto descifrado y se elige no crear ningún archivo."""
    simular_entrada("LXFOPVEFRNHR", "n")
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: "LEMON")
    assert cli.ejecutar_texto_manual(cifrar=False) == "menu"
    assert "ATTACKATDAWN" in capsys.readouterr().out


def test_texto_manual_rechaza_blancos(simular_entrada, monkeypatch, capsys):
    """Un texto que solo contiene espacios se rechaza y se vuelve a pedir."""
    simular_entrada("  ", "HOLA", "n")
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: "CLAVE")
    assert cli.ejecutar_texto_manual(cifrar=True) == "menu"
    assert "El mensaje no puede estar vacío" in capsys.readouterr().out


def test_texto_manual_volver(simular_entrada):
    """La palabra especial :volver regresa a la elección del origen."""
    simular_entrada(":volver")
    assert cli.ejecutar_texto_manual(cifrar=True) == "atras"


def test_texto_manual_volver_desde_clave(simular_entrada, monkeypatch, capsys):
    """Desde la clave se puede volver al mensaje sin cerrar la aplicación."""
    simular_entrada("HOLA", "NUEVO", "n")
    claves = iter(["0", "CLAVE"])
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: next(claves))
    assert cli.ejecutar_texto_manual(cifrar=True) == "menu"
    assert cli.cifrar_vigenere("NUEVO", "CLAVE") in capsys.readouterr().out

# --------------------------------------------------------------------------
# 6. PROCESAMIENTO DE ARCHIVOS PEQUEÑOS Y GRANDES
# --------------------------------------------------------------------------
def test_archivo_pequeno_cifrar_y_guardar(
    carpetas_temporales, simular_entrada, monkeypatch
):
    """Un .txt pequeño se lee, cifra y guarda correctamente."""
    desencriptados, encriptados = carpetas_temporales
    (desencriptados / "original.txt").write_text("ATTACKATDAWN", encoding="utf-8")
    simular_entrada("original", "s", "cifrado")
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: "LEMON")

    assert cli.ejecutar_archivo(cifrar=True) == "menu"
    assert (encriptados / "cifrado.txt").read_text() == "LXFOPVEFRNHR"


def test_archivo_pequeno_descifrar(
    carpetas_temporales, simular_entrada, monkeypatch, capsys
):
    """Un archivo cifrado pequeño se descifra y muestra por consola."""
    _, encriptados = carpetas_temporales
    (encriptados / "cifrado.txt").write_text("LXFOPVEFRNHR", encoding="utf-8")
    simular_entrada("cifrado", "n")
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: "LEMON")
    assert cli.ejecutar_archivo(cifrar=False) == "menu"
    assert "ATTACKATDAWN" in capsys.readouterr().out


def test_archivo_grande_procesado_por_bloques(
    carpetas_temporales, simular_entrada, monkeypatch, capsys
):
    """Al superar el umbral se procesa por bloques y conserva el resultado."""
    desencriptados, encriptados = carpetas_temporales
    texto = "Hola, mundo! " * 8
    (desencriptados / "grande.txt").write_text(texto, encoding="utf-8")
    # Forzamos la ruta de archivos grandes sin escribir megabytes en disco.
    # El algoritmo debe producir lo mismo sin importar dónde corte el bloque.
    monkeypatch.setattr(cli, "TAMANO_MAXIMO_PANTALLA", 10)
    monkeypatch.setattr(files, "TAMANO_BLOQUE", 7)
    simular_entrada("grande", "s", "resultado")
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: "CLAVE")

    assert cli.ejecutar_archivo(cifrar=True) == "menu"
    assert (encriptados / "resultado.txt").read_text() == cli.cifrar_vigenere(
        texto, "CLAVE"
    )
    assert "Procesando archivo por bloques" in capsys.readouterr().out


def test_archivo_corrupto_acepta_volver(
    carpetas_temporales, simular_entrada, monkeypatch, capsys
):
    """Un archivo UTF-8 inválido muestra un error y deja retroceder."""
    desencriptados, _ = carpetas_temporales
    (desencriptados / "corrupto.txt").write_bytes(b"A\xff")
    simular_entrada("corrupto", "0")
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: "CLAVE")

    assert cli.ejecutar_archivo(cifrar=True) == "atras"
    assert "Error al leer el archivo" in capsys.readouterr().out


def test_archivo_volver_desde_seleccion(carpetas_temporales, simular_entrada):
    """La selección de archivo acepta 0 para retroceder."""
    simular_entrada("0")
    assert cli.ejecutar_archivo(cifrar=True) == "atras"

# --------------------------------------------------------------------------
# 7. NAVEGACIÓN ENTRE MENÚS
# --------------------------------------------------------------------------
def test_ejecutar_operacion_opcion_invalida(simular_entrada, capsys):
    """Una opción de tipo de entrada incorrecta se comunica al usuario."""
    simular_entrada("9", "0")
    cli.ejecutar_operacion(cifrar=True)
    assert "Opción no válida" in capsys.readouterr().out


def test_ejecutar_operacion_texto_manual(simular_entrada, monkeypatch, capsys):
    """El flujo del menú lleva al cifrado de texto manual."""
    simular_entrada("1", "HOLA", "n")
    monkeypatch.setattr(cli, "pedir_clave_oculta", lambda: "CLAVE")
    cli.ejecutar_operacion(cifrar=True)
    assert "JZLV" in capsys.readouterr().out


def test_ejecutar_operacion_volver_desde_archivo(
    carpetas_temporales, simular_entrada
):
    """Se puede volver desde archivos hasta la elección inicial."""
    simular_entrada("2", "0", "0")
    cli.ejecutar_operacion(cifrar=True)

# --------------------------------------------------------------------------
# 8. LIMPIEZA OPCIONAL DE TEMPORALES ANTIGUOS
# --------------------------------------------------------------------------
def test_limpieza_temporales_sin_archivos(monkeypatch):
    """Si no hay temporales antiguos no se pide confirmación."""
    monkeypatch.setattr(cli, "detectar_temporales_antiguos", lambda: [])
    cli.ofrecer_limpieza_temporales()


def test_limpieza_temporales_rechazada(tmp_path, monkeypatch, simular_entrada):
    """Si el usuario responde n, el archivo temporal se conserva."""
    archivo = tmp_path / ".vigenere_antiguo.tmp"
    archivo.write_text("temporal", encoding="utf-8")
    # Simulamos que el detector ha encontrado este archivo temporal.
    monkeypatch.setattr(cli, "detectar_temporales_antiguos", lambda: [archivo])
    simular_entrada("n")
    cli.ofrecer_limpieza_temporales()
    assert archivo.exists()


def test_limpieza_temporales_aceptada(tmp_path, monkeypatch, simular_entrada):
    """Si el usuario responde s, el archivo temporal se elimina."""
    archivo = tmp_path / ".vigenere_antiguo.tmp"
    archivo.write_text("temporal", encoding="utf-8")
    # Simulamos que el detector ha encontrado este archivo temporal.
    monkeypatch.setattr(cli, "detectar_temporales_antiguos", lambda: [archivo])
    simular_entrada("s")
    cli.ofrecer_limpieza_temporales()
    assert not archivo.exists()

# --------------------------------------------------------------------------
# 9. EJECUCIÓN DEL PROGRAMA PRINCIPAL
# --------------------------------------------------------------------------
def test_main_sale_correctamente(simular_entrada, monkeypatch, capsys):
    """Elegir 0 en el menú principal termina normalmente."""
    monkeypatch.setattr(cli, "ofrecer_limpieza_temporales", lambda: None)
    simular_entrada("0")
    cli.main()
    assert "Hasta pronto" in capsys.readouterr().out


def test_main_ignora_opcion_invalida(simular_entrada, monkeypatch, capsys):
    """El menú principal informa de una opción inválida y sigue abierto."""
    monkeypatch.setattr(cli, "ofrecer_limpieza_temporales", lambda: None)
    simular_entrada("9", "0")
    cli.main()
    assert "Opción no válida" in capsys.readouterr().out


@pytest.mark.parametrize("error", [KeyboardInterrupt, EOFError])
def test_main_interrupcion_controlada(monkeypatch, capsys, error):
    """Ctrl+C o fin de entrada se controlan sin mostrar una excepción."""
    monkeypatch.setattr(cli, "ofrecer_limpieza_temporales", lambda: None)

    # Simulamos Ctrl+C/EOFError en vez de enviarlo realmente al programa.
    def interrumpir(_mensaje=""):
        raise error

    monkeypatch.setattr("builtins.input", interrumpir)
    cli.main()
    assert "Programa interrumpido" in capsys.readouterr().out

# --------------------------------------------------------------------------
# 10. BARRA DE PROGRESO Y TIEMPO ESTIMADO
# --------------------------------------------------------------------------
def test_indicador_progreso_final(capsys):
    """La actualización final muestra el 100 por ciento y el ETA."""
    progreso = cli.crear_indicador_progreso()
    progreso(0, 100)
    # La actualización final se imprime incluso cuando el tiempo es muy corto.
    progreso(100, 100)
    salida = capsys.readouterr().out
    assert "100.00%" in salida
    assert "ETA" in salida
