# Menú centrado y muralla

El menú se centra dentro del área segura en ambas orientaciones, ajustándose a muescas y ventanas pequeñas. Se eliminaron los componentes ButtonAnimation del menú y la duración de las transiciones de color es cero. Los siete botones usan BotonMenu.png, con borde de nueve segmentos para evitar deformar los herrajes. El idioma seleccionado conserva una indicación de color estática.

Recursos generados con ImageGen integrado:
- Assets/Sprites/Juego/BotonMenu.png: madera ciruela oscura, marco de hierro azulado, remaches discretos, pixel art sin texto, preparado a 240x56 y con transparencia.
- Assets/Sprites/Juego/Muralla.png: almenas profundas, contrafuertes escalonados, aspilleras, sillares de pizarra y estandartes, preparado a 960x160 con transparencia. Se conserva la altura de la base al sustituir los tres tramos.

Preparación técnica: recorte de márgenes transparentes y ajuste con vecino más próximo.
Pruebas: MainMenuTests y GameLayoutTests; resultados en Temp/MenuWallTests.xml.
Validación completada en copia temporal: 23 pruebas superadas, 0 fallos.
