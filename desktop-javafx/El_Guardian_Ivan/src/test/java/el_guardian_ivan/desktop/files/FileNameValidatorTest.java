package el_guardian_ivan.desktop.files;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;

import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNull;

/**
 * Pruebas de FileNameValidator: nombres válidos, reservados de Windows, caracteres prohibidos,
 * longitud y puntos o espacios al final.
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
class FileNameValidatorTest {

    /**
     * Los nombres normales se aceptan.
     *
     * @param name nombre del fichero
     */
    @ParameterizedTest
    @ValueSource(strings = {
            "hogwarts.hgw", "con espacios.txt", "acentos ñandú.png", "varios.puntos.tar.gz",
            "sin_extension", ".oculto", "CONSOLA.txt", "nul-no-es-reservado.txt", "COM10.txt"
    })
    void acceptsValidNames(String name) {
        assertNull(FileNameValidator.validate(name));
    }

    /**
     * Los nombres reservados de Windows (CON, NUL, COM1...) se rechazan.
     *
     * @param name nombre del fichero
     */
    @ParameterizedTest
    @ValueSource(strings = {
            "CON", "con.txt", "Nul.hgw", "PRN.png", "aux", "COM1.bin", "lpt9.txt", "CON .txt"
    })
    void rejectsWindowsReservedNames(String name) {
        assertNotNull(FileNameValidator.validate(name));
    }

    /**
     * Los nombres vacíos, ".", "..", con caracteres prohibidos o terminados en punto o espacio se rechazan.
     *
     * @param name nombre del fichero
     */
    @ParameterizedTest
    @ValueSource(strings = {
            "a<b.txt", "a>b.txt", "a:b.txt", "a\"b.txt", "a/b.txt", "a\\b.txt", "a|b.txt", "a?b.txt", "a*b.txt",
            "tab\there.txt", "acaba en punto.", "acaba en espacio ", "", "   ", ".", ".."
    })
    void rejectsInvalidNames(String name) {
        assertNotNull(FileNameValidator.validate(name));
    }

    /** Los nombres de más de 255 caracteres se rechazan. */
    @Test
    void rejectsTooLongNames() {
        assertNotNull(FileNameValidator.validate("a".repeat(256)));
        assertNull(FileNameValidator.validate("a".repeat(255)));
    }

    /** Un nombre nulo se rechaza. */
    @Test
    void rejectsNull() {
        assertNotNull(FileNameValidator.validate(null));
    }
}
