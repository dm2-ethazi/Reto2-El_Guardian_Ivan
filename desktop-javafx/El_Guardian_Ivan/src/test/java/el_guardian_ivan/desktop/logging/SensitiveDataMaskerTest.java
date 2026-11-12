package el_guardian_ivan.desktop.logging;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

/**
 * Comprueba que los datos sensibles no llegan a los registros.
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
class SensitiveDataMaskerTest {

    /** Usuario de Windows de ejemplo. */
    private final SensitiveDataMasker windows = new SensitiveDataMasker("C:\\Users\\xlexy", "xlexy");

    /** La carpeta del usuario en Windows se cambia por "~". */
    @Test
    void hidesWindowsHomeFolder() {
        assertEquals("No se encuentra ~\\Documents\\nota.txt",
                windows.mask("No se encuentra C:\\Users\\xlexy\\Documents\\nota.txt"));
    }

    /** Se reconoce la carpeta aunque cambien las barras o las mayúsculas. */
    @Test
    void hidesHomeFolderWithOtherSlashesAndCase() {
        assertEquals("ruta ~/Desktop/a.png", windows.mask("ruta c:/users/XLEXY/Desktop/a.png"));
    }

    /** El nombre de usuario se oculta también fuera de su carpeta. */
    @Test
    void hidesUserNameAnywhere() {
        String masked = windows.mask("D:\\Copias\\xlexy\\fotos y usuario Xlexy");
        assertFalse(masked.toLowerCase().contains("xlexy"), masked);
        assertTrue(masked.contains(SensitiveDataMasker.USER));
    }

    /** No se toca la carpeta de otro usuario cuyo nombre empieza igual. */
    @Test
    void doesNotHideOtherUsersFolderWithSimilarName() {
        assertEquals("C:\\Users\\xlexy2\\a.txt",
                new SensitiveDataMasker("C:\\Users\\xlexy", null).mask("C:\\Users\\xlexy2\\a.txt"));
    }

    /** La carpeta del usuario en Linux se cambia por "~". */
    @Test
    void hidesLinuxHomeFolder() {
        SensitiveDataMasker linux = new SensitiveDataMasker("/home/ivan", "ivan");
        assertEquals("abrir ~/docs/x.pdf", linux.mask("abrir /home/ivan/docs/x.pdf"));
    }

    /**
     * Lo que va detrás de "contraseña", "password", "clave"... se cambia por "****".
     *
     * @param text texto que se enmascara
     */
    @ParameterizedTest
    @ValueSource(strings = {"contraseña=Hogwarts123", "password: Hogwarts123", "PASS = Hogwarts123",
            "clave=Hogwarts123", "pasahitza: Hogwarts123", "token=\"Hogwarts 123\""})
    void hidesPasswordValues(String text) {
        String masked = windows.mask(text);
        assertFalse(masked.contains("Hogwarts"), masked);
        assertTrue(masked.contains(SensitiveDataMasker.MASK), masked);
    }

    /** Las claves, sales y nonces en hexadecimal o Base64 se ocultan. */
    @Test
    void hidesLongHexAndBase64Values() {
        String masked = windows.mask("sal 00112233445566778899aabbccddeeff y nonce q83vEjRWeJCrze8SNFZ4kA==");
        assertEquals("sal [oculto] y nonce [oculto]", masked);
    }

    /** Un mensaje normal, sin datos sensibles, no se modifica. */
    @Test
    void keepsNormalMessages() {
        String text = "Operación terminada en 120 ms: fichero .png de 1.5 MB (AesFileCipherEdgeCasesTest)";
        assertEquals(text, windows.mask(text));
    }

    /** Un texto nulo o vacío se devuelve tal cual. */
    @Test
    void acceptsNullAndEmpty() {
        assertNull(windows.mask(null));
        assertEquals("", windows.mask(""));
    }

    /** Funciona aunque no se conozcan la carpeta ni el usuario. */
    @Test
    void worksWithoutUserData() {
        assertEquals("password=****", new SensitiveDataMasker(null, null).mask("password=1234"));
    }
}
