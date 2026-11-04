package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.navigation.NavigationAware;
import el_guardian_ivan.desktop.navigation.Navigator;
import el_guardian_ivan.desktop.navigation.View;
import javafx.fxml.FXML;

/** Pantalla de inicio: dos accesos directos a Cifrar y Descifrar. */
public class HomeController implements NavigationAware {

    /** FXMLLoader crea el controlador al cargar home-view.fxml. */
    public HomeController() {
    }
    /** Quien cambia de pantalla (MainController); lo recibe al cargar esta vista. */
    private Navigator navigator;

    @Override
    public void setNavigator(Navigator navigator) {
        this.navigator = navigator;
    }

    /** Tarjeta "Cifrar archivo": lleva a la pantalla Cifrar, igual que el botón del menú. */
    @FXML
    private void onOpenEncrypt() {
        navigator.navigateTo(View.ENCRYPT);
    }

    /** Tarjeta "Descifrar archivo": lleva a la pantalla Descifrar. */
    @FXML
    private void onOpenDecrypt() {
        navigator.navigateTo(View.DECRYPT);
    }
}
