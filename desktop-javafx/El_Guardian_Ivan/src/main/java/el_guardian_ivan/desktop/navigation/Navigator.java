package el_guardian_ivan.desktop.navigation;

/** Permite a una pantalla pedir que se muestre otra (por ejemplo, las tarjetas de Inicio). */
@FunctionalInterface
public interface Navigator {
    void navigateTo(View view);
}
