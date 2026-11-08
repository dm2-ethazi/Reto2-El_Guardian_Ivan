package el_guardian_ivan.desktop.logging;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

/** Comprueba que los datos sensibles no llegan a los registros. */
class SensitiveDataMaskerTest {

    /** Usuario de Windows de ejemplo. */
    private final SensitiveDataMasker windows = new SensitiveDataMasker("C:\\Users\\xlexy", "xlexy");

    @Test
    void hidesWindowsHomeFolder() {
        assertEquals("No se encuentra ~\\Documents\\nota.txt",
                windows.mask("No se encuentra C:\\Users\\xlexy\\Documents\\nota.txt"));
    }

    @Test
    void hidesHomeFolderWithOtherSlashesAndCase() {
        assertEquals("ruta ~/Desktop/a.png", windows.mask("ruta c:/users/XLEXY/Desktop/a.png"));
    }

    @Test
    void hidesUserNameAnywhere() {
        String masked = windows.mask("D:\\Copias\\xlexy\\fotos y usuario Xlexy");
        assertFalse(masked.toLowerCase().contains("xlexy"), masked);
        assertTrue(masked.contains(SensitiveDataMasker.USER));
    }

    @Test
    void doesNotHideOtherUsersFolderWithSimilarName() {
        assertEquals("C:\\Users\\xlexy2\\a.txt",
                new SensitiveDataMasker("C:\\Users\\xlexy", null).mask("C:\\Users\\xlexy2\\a.txt"));
    }

    @Test
    void hidesLinuxHomeFolder() {
        SensitiveDataMasker linux = new SensitiveDataMasker("/home/ivan", "ivan");
        assertEquals("abrir ~/docs/x.pdf", linux.mask("abrir /home/ivan/docs/x.pdf"));
    }

    @ParameterizedTest
    @ValueSource(strings = {"contraseña=Hogwarts123", "password: Hogwarts123", "PASS = Hogwarts123",
            "clave=Hogwarts123", "pasahitza: Hogwarts123", "token=\"Hogwarts 123\""})
    void hidesPasswordValues(String text) {
        String masked = windows.mask(text);
        assertFalse(masked.contains("Hogwarts"), masked);
        assertTrue(masked.contains(SensitiveDataMasker.MASK), masked);
    }

    @Test
    void hidesLongHexAndBase64Values() {
        String masked = windows.mask("sal 00112233445566778899aabbccddeeff y nonce q83vEjRWeJCrze8SNFZ4kA==");
        assertEquals("sal [oculto] y nonce [oculto]", masked);
    }

    @Test
    void keepsNormalMessages() {
        String text = "Operación terminada en 120 ms: fichero .png de 1.5 MB (AesFileCipherEdgeCasesTest)";
        assertEquals(text, windows.mask(text));
    }

    @Test
    void acceptsNullAndEmpty() {
        assertNull(windows.mask(null));
        assertEquals("", windows.mask(""));
    }

    @Test
    void worksWithoutUserData() {
        assertEquals("password=****", new SensitiveDataMasker(null, null).mask("password=1234"));
    }
}
