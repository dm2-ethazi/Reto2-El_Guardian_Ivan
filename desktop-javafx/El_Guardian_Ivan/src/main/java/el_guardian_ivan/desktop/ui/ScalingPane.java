package el_guardian_ivan.desktop.ui;

import javafx.beans.property.ReadOnlyDoubleProperty;
import javafx.beans.property.ReadOnlyDoubleWrapper;
import javafx.scene.Parent;
import javafx.scene.layout.Region;
import javafx.scene.transform.Scale;

/**
 * Escala toda la interfaz (textos, iconos, márgenes...) en proporción al tamaño de la ventana,
 * tomando como referencia el tamaño de diseño. En pantallas pequeñas la reduce para que no se
 * corte el texto, sin bajar de MIN_SCALE para que siga siendo legible; si aun así no cabe,
 * cada pantalla tiene su barra de desplazamiento.
 */
public class ScalingPane extends Region {

    /** Tamaño para el que está diseñada la interfaz: con él se ve a escala 1. */
    private static final double DESIGN_WIDTH = 1000;
    private static final double DESIGN_HEIGHT = 650;
    private static final double MIN_SCALE = 0.6;
    private static final double MAX_SCALE = 2.0;

    private final Parent content;
    private final Scale scale = new Scale(1, 1, 0, 0);
    private final ReadOnlyDoubleWrapper currentScale = new ReadOnlyDoubleWrapper(1);

    public ScalingPane(Parent content) {
        this.content = content;
        content.getTransforms().add(scale);
        getChildren().add(content);
    }

    /** Escala aplicada ahora mismo; los diálogos la usan para verse del mismo tamaño que la ventana. */
    public ReadOnlyDoubleProperty scaleProperty() {
        return currentScale.getReadOnlyProperty();
    }

    public double getScale() {
        return currentScale.get();
    }

    @Override
    protected void layoutChildren() {
        double width = getWidth();
        double height = getHeight();
        double factor = Math.min(width / DESIGN_WIDTH, height / DESIGN_HEIGHT);
        factor = Math.max(MIN_SCALE, Math.min(MAX_SCALE, factor));
        scale.setX(factor);
        scale.setY(factor);
        currentScale.set(factor);
        // El contenido se coloca a su tamaño "sin escalar" y la transformación lo agranda hasta llenar la ventana
        content.resizeRelocate(0, 0, width / factor, height / factor);
    }

    @Override
    protected double computePrefWidth(double height) {
        return content.prefWidth(height);
    }

    @Override
    protected double computePrefHeight(double width) {
        return content.prefHeight(width);
    }
}
