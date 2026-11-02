package el_guardian_ivan.desktop.navigation;

/** Lo implementan los controladores que necesitan navegar a otras pantallas. */
public interface NavigationAware {
    /**
     * MainController lo llama justo después de cargar la pantalla.
     *
     * @param navigator quien cambia de pantalla
     */
    void setNavigator(Navigator navigator);
}
