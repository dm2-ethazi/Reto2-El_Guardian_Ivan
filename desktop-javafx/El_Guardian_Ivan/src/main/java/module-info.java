module el_guardian_ivan.el_guardian_ivan {
    requires javafx.controls;
    requires javafx.fxml;


    opens el_guardian_ivan.el_guardian_ivan to javafx.fxml;
    exports el_guardian_ivan.el_guardian_ivan;
}