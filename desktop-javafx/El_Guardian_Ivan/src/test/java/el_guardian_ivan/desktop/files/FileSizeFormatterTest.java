package el_guardian_ivan.desktop.files;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.CsvSource;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

/** Pruebas de FileSizeFormatter: unidad correcta en cada salto de 1024 y rechazo de negativos. */
class FileSizeFormatterTest {

    /**
     * Cada tamaño sale con la unidad correcta y coma decimal.
     *
     * @param bytes tamaño en bytes
     * @param expected texto esperado
     */
    @ParameterizedTest
    @CsvSource(delimiter = ';', value = {
            "0; 0 B",
            "1; 1 B",
            "1023; 1023 B",
            "1024; 1,0 KB",
            "1536; 1,5 KB",
            "1048576; 1,0 MB",
            "20971520; 20,0 MB",
            "4509715660; 4,2 GB",
            "1099511627776; 1,0 TB"
    })
    void formatsWithTheRightUnit(long bytes, String expected) {
        assertEquals(expected, FileSizeFormatter.format(bytes));
    }

    /** Un tamaño negativo da error. */
    @Test
    void rejectsNegativeSizes() {
        assertThrows(IllegalArgumentException.class, () -> FileSizeFormatter.format(-1));
    }
}
