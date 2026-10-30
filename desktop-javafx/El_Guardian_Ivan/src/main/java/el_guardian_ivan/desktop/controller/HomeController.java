package el_guardian_ivan.desktop.controller;

import el_guardian_ivan.desktop.navigation.NavigationAware;
import el_guardian_ivan.desktop.navigation.Navigator;
import el_guardian_ivan.desktop.navigation.View;
import javafx.fxml.FXML;

/** Pantalla de inicio: dos accesos directos a Cifrar y Descifrar. */
public class HomeController implements NavigationAware {
    private Navigator navigator;

    @Override
    public void setNavigator(Navigator navigator) {
        this.navigator = navigator;
    }

    @FXML
    private void onOpenEncrypt() {
        navigator.navigateTo(View.ENCRYPT);
    }

    @FXML
    private void onOpenDecrypt() {
        navigator.navigateTo(View.DECRYPT);
    }
}
