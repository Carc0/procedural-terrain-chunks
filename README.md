# Generación procedural de Terreno con Caminos en Chunks

TODO: Portada
<img src="">

# Resumen

El mapa generado por el MapCreator está compuesto de Chunks que, a su vez, estos están compuestos de Cubes. Estos Cubes pueden ser de dos tipos (GrassCube y PathCube). 

Los Paths estarán compuesto por PathCubes. Un Chunk puede tener varios Path generados y divididos de la siguiente manera:

1. Se genera un Path hasta el Fork. El fork es un PathCube marcado como punto de bifurcación de los siguientes Paths. Este fork está asegurado de generarse más allá de la mitad del chunk.
2. Se continua generando el primer Path hasta un Edge (Borde de un Chunk). Este Edge está determinado por lo siguiente:
	2.1. No puede ser el mismo Edge del que partió el Path.
	2.2. No puede ser un Edge que está cerrado entre otros Chunks.
	2.3. No puede chocarse con otros Paths (Incluido él mismo).
3. Una vez generado este primer Path, en caso de que exista la probabilidad (dependiendo del número de Chunks generados) creará más Paths. Estos Paths partirán del Fork e intentarán llegar a otro Edge disponible. Si no son capaces, recorrerán el primer Path, cambiando su origen e intentando llegar a su dirección de salida.
4. En caso de que un Path no pueda llegar a su dirección de salida, intentará recalcularse volviendo sobre sus pasos e impidiendo que recorra el mismo camino que no le sirvió para llegar a su destino anteriormente. En caso que no sea capaz se descartará.

El primer Chunk se genera siempre de la misma manera, partiendo de su centro y yendo en línea recta a uno de sus Edge.

Como añadido y forma de testear el MapCreator, se implementó una funcionalidad que permite ir paso a paso (Go Step By Step) e ir comprobando como se genera los Chunks y sus respectivos Paths. A continuación se explica como se usa.

TODO: CreaciónMapa
<img src="">

# Como usarlo

La escena Map_Scene ya está configurada para su uso, siendo el MapCreator el elemento más importante y donde está la configuración para probar

El sistema está montado de tal manera que funcione en EditorMode como en PlayMode, a excepción de la funcionalidad Go Step By Step automática, que solo funciona en PlayMode.

TODO: Configuración
<img src="">

- Chunk Size: Tamaño del Chunk limitado en 5 hasta 50. Establecí la base en mínimo en 5 para que los Paths pudieran generarse en ese espacio tan pequeño. A su vez, límite en 50 el máximo debido a la carga y tiempo de espera que supondría hacer Chunks tan grandes.
- Chunks Number: Número de Chunks que habrá en el mapa. Límitado en 1 (El inicial) hasta 50, debido a la misma razón de tiempos de espera.

- Proximity Path To Edge: Peso que tendrán los Paths en generarse cerca del centro o cerca de los Edge. Un valor de 0, hará que los Paths se generen más cerca del centro. Un valor de 1, hará que se generen más cerca de los Edge.
- Irregularity Path: Peso que tendrán los Paths en generarse de manera irregular o de manera recta. Un valor de 0, hará que los Paths se generen más rectos. Un valor de 1, hará que los PAths se generen más irregulares.

Seed: Número de la seed cargada. En caso de usar un número negativo (-1 default) hará que aleatoriamente se genere un seed. En caso que se quiera generar una seed específica tan solo poner un número positivo. En la Consola, el primer mensaje mostrará la seed generada.
Sum New Paths Probability: Probabilidad que se irá sumando por cada Chunk generado para la generación de Paths adicionales.

Go Step By Step: Para comprobar la generación del mapa paso a paso. En EditorMode, será necesario usar el botón Do Next Step cada vez que se quiera avanzar poco a poco en su generación. En GameMode se hará automáticamente.
Go Step By Step Seconds: Si Go Step By Step es true y se está en GameMode, tiempo que tardará en hacer un Do Next Step hasta terminar el Path automáticamente.

Create map: Genera el mapa. Dependiendo de Go Step By Step lo hará por completo o poco a poco.
Do next step: Si Go Step By Step es true y se está en EditorMode, avanzará un paso en la generación del mapa.
Delete map: Reinicia el mapa.

# TODO

- Hacer un StateMachine para la generación Go Step By Step y saber en que momento de su creación está constantemente.
- Interfaz dentro de PlayMode, con pantalla de carga asíncrona para saber cuanto queda de espera según el número de Chunks.
- Elección de mostrar Logs o no.