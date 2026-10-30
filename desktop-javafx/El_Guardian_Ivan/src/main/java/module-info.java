module el_guardian_ivan.desktop {
    requires javafx.controls;
    requires javafx.fxml;


    opens el_guardian_ivan.desktop.controller to javafx.fxml;
    exports el_guardian_ivan.desktop;
}