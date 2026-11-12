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
 *
 * @author Iván Salazar
 * @author Iván Alonso
 */
public class ScalingPane extends Region {

    /** Ancho para el que está diseñada la interfaz: con 1000 x 650 se ve a escala 1. */
    private static final double DESIGN_WIDTH = 1000;
    /** Alto para el que está diseñada la interfaz. */
    private static final double DESIGN_HEIGHT = 650;
    /** Por debajo del 60 % el texto sería demasiado pequeño para leerse bien. */
    private static final double MIN_SCALE = 0.6;
    /** Por encima del 200 % (pantallas muy grandes) no tiene sentido seguir agrandando. */
    private static final double MAX_SCALE = 2.0;

    /** La interfaz que se escala (el menú lateral con la pantalla actual). */
    private final Parent content;
    /** Transformación que agranda o reduce el contenido. */
    private final Scale scaleTransform = new Scale(1, 1, 0, 0);
    /** Escala actual, observable para quien la necesite. */
    private final ReadOnlyDoubleWrapper currentScale = new ReadOnlyDoubleWrapper(1);

    /**
     * Envuelve {@code content} para que se adapte al tamaño de la ventana.
     *
     * @param content interfaz que se escala
     */
    public ScalingPane(Parent content) {
        this.content = content;
        content.getTransforms().add(scaleTransform);
        getChildren().add(content);
    }

    /**
     * Escala aplicada ahora mismo; los diálogos la usan para verse del mismo tamaño que la ventana.
     *
     * @return la propiedad (solo lectura) con la escala
     */
    public ReadOnlyDoubleProperty scaleProperty() {
        return currentScale.getReadOnlyProperty();
    }

    /**
     * Valor actual de la escala: 1 = tamaño de diseño.
     *
     * @return la escala actual
     */
    public double getScale() {
        return currentScale.get();
    }

    /** JavaFX lo llama cada vez que cambia el tamaño de la ventana: recalcula la escala. */
    @Override
    protected void layoutChildren() {
        double width = getWidth();
        double height = getHeight();
        // Se usa el lado que más limita, para que todo quepa sin deformarse
        double factor = Math.min(width / DESIGN_WIDTH, height / DESIGN_HEIGHT);
        factor = Math.max(MIN_SCALE, Math.min(MAX_SCALE, factor));
        scaleTransform.setX(factor);
        scaleTransform.setY(factor);
        currentScale.set(factor);
        // El contenido se coloca a su tamaño "sin escalar" y la transformación lo agranda hasta llenar la ventana
        content.resizeRelocate(0, 0, width / factor, height / factor);
    }

    // El tamaño preferido es el del contenido: así la ventana se abre a la medida del diseño
    @Override
    protected double computePrefWidth(double height) {
        return content.prefWidth(height);
    }

    @Override
    protected double computePrefHeight(double width) {
        return content.prefHeight(width);
    }
}
