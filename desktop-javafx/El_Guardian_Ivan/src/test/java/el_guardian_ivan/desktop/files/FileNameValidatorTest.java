package el_guardian_ivan.desktop.files;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;

import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNull;

class FileNameValidatorTest {

    @ParameterizedTest
    @ValueSource(strings = {
            "hogwarts.hgw", "con espacios.txt", "acentos ñandú.png", "varios.puntos.tar.gz",
            "sin_extension", ".oculto", "CONSOLA.txt", "nul-no-es-reservado.txt", "COM10.txt"
    })
    void acceptsValidNames(String name) {
        assertNull(FileNameValidator.validate(name));
    }

    @ParameterizedTest
    @ValueSource(strings = {
            "CON", "con.txt", "Nul.hgw", "PRN.png", "aux", "COM1.bin", "lpt9.txt", "CON .txt"
    })
    void rejectsWindowsReservedNames(String name) {
        assertNotNull(FileNameValidator.validate(name));
    }

    @ParameterizedTest
    @ValueSource(strings = {
            "a<b.txt", "a>b.txt", "a:b.txt", "a\"b.txt", "a/b.txt", "a\\b.txt", "a|b.txt", "a?b.txt", "a*b.txt",
            "tab\there.txt", "acaba en punto.", "acaba en espacio ", "", "   ", ".", ".."
    })
    void rejectsInvalidNames(String name) {
        assertNotNull(FileNameValidator.validate(name));
    }

    @Test
    void rejectsTooLongNames() {
        assertNotNull(FileNameValidator.validate("a".repeat(256)));
        assertNull(FileNameValidator.validate("a".repeat(255)));
    }

    @Test
    void rejectsNull() {
        assertNotNull(FileNameValidator.validate(null));
    }
}
