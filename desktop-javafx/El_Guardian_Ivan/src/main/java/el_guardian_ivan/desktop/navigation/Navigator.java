package el_guardian_ivan.desktop.navigation;

/**
 * Permite a una pantalla pedir que se muestre otra (por ejemplo, las tarjetas de Inicio).
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
@FunctionalInterface
public interface Navigator {
    /**
     * Muestra la pantalla indicada y marca su botón en el menú lateral.
     *
     * @param view pantalla que se quiere ver
     */
    void navigateTo(View view);
}
