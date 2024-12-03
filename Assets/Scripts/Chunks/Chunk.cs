using Assets.Scripts.Cubes;
using Assets.Scripts.Map;
using Assets.Scripts.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Chunks
{
    // Clase principal para la creación de los paths de un chunk
    public class Chunk
    {
        private readonly int size;
        private readonly float cubeSize;
        private readonly GameObject gameObject;
        private readonly List<List<PathCube>> paths;

        private readonly ChunkCubes chunkCubes;

        private Vector3 centerPosition;
        private Directions lastCubeDirection;
        private Directions lastChunkDirection;

        private int initForkIndex;
        private int forkIndex;

        private bool stopCreatingPath;
        private bool isLastPathFinished;

        // Map Creator Config
        private bool goStepByStep;
        private float maxProbabilityProximity;
        private float minProbabilityProximity;
        private float minProbabilityIrregularity;
        private float maxProbabilityIrregularity;

        public Chunk(string _name, Vector3 _position, int _size, float _cubeSize)
        {
            gameObject = new(_name);
            gameObject.transform.position = _position;
            gameObject.isStatic = true;

            size = _size;
            cubeSize = _cubeSize;

            chunkCubes = new ChunkCubes();
            paths = new();

            stopCreatingPath = isLastPathFinished = false;
        }

        public Vector3 Position { get => gameObject.transform.position; }
        public GameObject GameObject { get => gameObject; }
        public List<List<PathCube>> Paths { get => paths; }
        public bool IsLastPathFinished { get => isLastPathFinished; }

        #region Chunk
        // Inicializa los valores del Map Creator
        public void Initialize(float _proximityPathToEdge, float _irregularityPath, bool _goStepByStep)
        {
            // Proximity
            maxProbabilityProximity = 100.0f * _proximityPathToEdge;
            minProbabilityProximity = 100.0f - maxProbabilityProximity;

            // Irregularity
            minProbabilityIrregularity = 100.0f * _irregularityPath;
            maxProbabilityIrregularity = 100.0f - minProbabilityIrregularity;

            goStepByStep = _goStepByStep;

            centerPosition = Position + new Vector3((size / 2) * cubeSize, cubeSize, (size / 2) * cubeSize);

            InitializeCubes();
        }

        // Inicializa los cubos del chunk dando por hecho que son cuadrados
        private void InitializeCubes()
        {
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    // Crea los cubos "path" que hacen de plataforma de la hierba y camino
                    CreateCube(Position + new Vector3(x * cubeSize, 0.0f, z * cubeSize), CubeTypes.PATH);

                    // Crea los cubos "grass" iniciales que se irán sustituyendo
                    CreateCube(Position + new Vector3(x * cubeSize, cubeSize, z * cubeSize), CubeTypes.GRASS);
                }
            }
        }

        // Instancia los GameObjects de los cubos en caso que no lo haya hecho paso a paso
        public void InstantiateCubesGameObjects()
        {
            if (goStepByStep) return;

            chunkCubes.InstantiateCubesGameObjects();
        }

        // Añade los componentes MeshFilter y MeshRenderer y combina sus Mesh para optimizar
        public void Optimize()
        {
            MeshCombiner.CombineMeshes(GameObject);
        }

        // Destruye el GameObject del Chunk
        public void DestroyGameObject()
        {
            MapCreator.Instance.DestroyChunkGameObject(this);
        }
        #endregion

        #region Initial Path
        // Crea el primer camino en el primer Chunk
        public void CreateFirstPath()
        {
            CreateCenterPathCube();

            CreateStraightPath((Directions)Random.Range(0, 4));
        }

        // Crea el PathCube inicial en el centro del Chunk
        private void CreateCenterPathCube()
        {
            Cube centerCube = chunkCubes.TryGetCubeInPosition(Position + new Vector3((size / 2) * cubeSize, cubeSize, (size / 2) * cubeSize));
            
            if (centerCube == null)
            {
                Debug.LogError($"Chunk {GameObject.name}. Center Cube missing.");
                return;
            }

            Cube newCube = SwitchCube(centerCube, CubeTypes.PATH);

            // Crea un nuevo Path y añade el primer PathCube
            Paths.Add(new());
            Paths[^1].Add((PathCube)newCube);

            isLastPathFinished = false;
        }

        // Crea el camino recto que va desde el centro a uno de los laterales del primer chunk
        private void CreateStraightPath(Directions _cubeDirection)
        {
            Vector3 sumDirection = DirectionFunctions.GetSumDirection(_cubeDirection, cubeSize);
            Vector3 nextCubePosition = sumDirection + Paths[^1][^1].Position;

            // Crea un camino recto hacia la dirección dada al tratarse del Initial Chunk
            Cube cube;
            do
            {
                cube = chunkCubes.TryGetCubeInPosition(nextCubePosition);

                if (cube == null) break;

                cube = SwitchCube(cube, CubeTypes.PATH);

                Paths[^1].Add((PathCube)cube);

                nextCubePosition = sumDirection + Paths[^1][^1].Position;
            } while (cube != null);
        }
        #endregion

        #region Paths
        // Crea el camino de PathCubes hasta la bifurcación (Mitad del Chunk)
        public void CreatePathUntilFork(Vector3 _lastPathPosition, Directions _cubeDirection)
        {
            lastCubeDirection = lastChunkDirection = _cubeDirection;
            Vector3 sumDirection = DirectionFunctions.GetSumDirection(lastCubeDirection, cubeSize);

            // Sustituye el primer cubo que está en el borde por donde empieza el camino y crea la primera lista Path
            Vector3 cubePosition = _lastPathPosition + sumDirection;
            Cube cube = chunkCubes.TryGetCubeInPosition(cubePosition);
            Cube newCube = SwitchCube(cube, CubeTypes.PATH);
            Paths.Add(new());
            Paths[^1].Add((PathCube)newCube);

            // Sustiyuye el segundo cubo, que siempre será hacia la misma dirección ya que no puede crearse en el borde 
            cubePosition = sumDirection + Paths[^1][^1].Position;
            cube = chunkCubes.TryGetCubeInPosition(cubePosition);
            newCube = SwitchCube(cube, CubeTypes.PATH);
            Paths[^1].Add((PathCube)newCube);

            if (goStepByStep)
            {
                Debug.Log("Going step by step until fork path.");
                return;
            }

            while (!stopCreatingPath)
            {
                TryCreateNextPathCube(new());
            }
        }

        // Crea el camino de PathCubes hasta el borde del Chunk
        public void CreatePathUntilEdge(Directions _choosenChunkDirection)
        {
            stopCreatingPath = false;

            // Si el último path ha llegado a su fin y se quiere seguir creando Paths
            if (isLastPathFinished)
            {
                // Utiliza el ForkPathCube (La mitad del Chunk) para empezar a crear el nuevo Path
                forkIndex = initForkIndex;

                PathCube forkPathCube = Paths[0][forkIndex];

                // Añade ese ForkPathCube al último Path que se creará a continuación
                Paths.Add(new());
                Paths[^1].Add(forkPathCube);

                lastCubeDirection = forkPathCube.LastPathCubeDirection;

                isLastPathFinished = false;
            }

            if (goStepByStep)
            {
                Debug.Log("Going step by step until edge.");
                return;
            }

            while (!stopCreatingPath)
            {
                TryCreateNextPathCube(new(), _choosenChunkDirection);
            }
        }

        // Creación de un PathCube teniendo en cuenta:
        // _forbiddenCubeDirections: Direcciones que ha probado y no llegó a su salida
        // _exitCubeDirection: La dirección de salida que busca
        // La proximidad del Path al borde del chunk (MapCreator proximityPathToEdge)
        // La irregularidad del Path (MapCreator irregularityPath)
        // Return true si ha creado el siguiente PathCube
        // Return false si no ha creado el siguiente PathCube
        public bool TryCreateNextPathCube(List<Directions> _forbiddenCubeDirections, Directions _exitCubeDirection = Directions.NULL)
        {
            if (stopCreatingPath) return false;

            // Checkear cuanta distancia ha recorrido ya el path 
            if (HasPathFinished(_exitCubeDirection)) return false;

            // Recupera las direcciones disponibles para crear el siguiente PathCube
            List<Directions> availableCubeDirections = GetAvailableCubeDirections(Paths[^1][^1].Position, lastCubeDirection, _forbiddenCubeDirections);

            // Si no tiene direcciones válidas tiene que retroceder 
            // Deshacer último movimiento y prohibir esa dirección
            if (availableCubeDirections.Count == 0)
            {
                return ResetPathCube(_exitCubeDirection);
            }
            // Si solo hay una dirección disponible, usarla
            else if (availableCubeDirections.Count == 1)
            {
                CreateNextPathCube(availableCubeDirections[0]);

                return true;
            }

            // Recupera un diccionario con las direcciones disponibles y la probabilidad de elegir esa dirección
            Dictionary<Directions, float> directionsProbability = GetDirectionsProbability(_exitCubeDirection, availableCubeDirections);

            // Suma la probabilidad total
            float totalProbability = 0.0f;
            foreach (var direction in directionsProbability)
            {
                totalProbability += direction.Value;
            }

            // Elige una dirección por su peso de probabilidad
            float random = Random.Range(0, totalProbability);
            float randomSum = 0.0f;

            foreach (var direction in directionsProbability)
            {
                randomSum += direction.Value;

                if (random > randomSum) continue;

                CreateNextPathCube(direction.Key);

                return true;
            }

            return false;
        }

        // Recupera un diccionario con las direcciones disponibles y la probabilidad de elegir esa dirección
        // según la proximidad al borde del chunk y según la irregularidad del camino
        private Dictionary<Directions, float> GetDirectionsProbability(Directions _exitCubeDirection, List<Directions> availableCubeDirections)
        {
            Dictionary<Directions, float> directionsProbability = new();

            // Comprobación de las direcciones disponibles para saber cuáles están más cerca del centro
            // y cuáles más alejadas para posteriormente aplicarles un peso de probabilidad diferente
            float minDistanceToCenter = 0.0f;
            int minDistanceToCenterCount = 0;
            float maxDistanceToCenter = 0.0f;
            int maxDistanceToCenterCount = 0;

            // Comprueba si entre las direcciones disponibles está la última dirección del cubo
            int subLastCubeDirection = 0;

            // Recorre todas las direcciones disponibles para saber cuáles están más cerca del centro y cuáles no
            foreach (Directions availableCubeDirection in availableCubeDirections)
            {
                Vector3 checkCubePosition = Paths[^1][^1].Position + DirectionFunctions.GetSumDirection(availableCubeDirection, cubeSize);
                float distanceToCenter = Vector3.Distance(checkCubePosition, centerPosition);

                if (minDistanceToCenter == 0.0f)
                {
                    minDistanceToCenter = maxDistanceToCenter = distanceToCenter;
                    minDistanceToCenterCount = maxDistanceToCenterCount = 1;
                }
                else
                {
                    if (distanceToCenter < minDistanceToCenter)
                    {
                        minDistanceToCenter = distanceToCenter;
                        minDistanceToCenterCount = 1;
                    }
                    else if (distanceToCenter == minDistanceToCenter)
                    {
                        minDistanceToCenterCount++;
                    }

                    if (distanceToCenter > maxDistanceToCenter)
                    {
                        maxDistanceToCenter = distanceToCenter;
                        maxDistanceToCenterCount = 1;
                    }
                    else if (distanceToCenter == maxDistanceToCenter)
                    {
                        maxDistanceToCenterCount++;
                    }
                }

                if (availableCubeDirection == lastCubeDirection)
                    subLastCubeDirection = 1;
            }

            // Asigna probabilidad a cada dirección según estén más cerca o lejos del centro del Chunk
            // y según si tiene que ser recto o un camino irregular
            foreach (Directions availableCubeDirection in availableCubeDirections)
            {
                directionsProbability.Add(availableCubeDirection, 0.0f);

                Vector3 checkCubePosition = Paths[^1][^1].Position + DirectionFunctions.GetSumDirection(availableCubeDirection, cubeSize);

                float distanceToCenter = Vector3.Distance(checkCubePosition, centerPosition);

                // Suma de probabilidad según si está cerca o lejos del centro del Chunk
                if (distanceToCenter == minDistanceToCenter)
                    directionsProbability[availableCubeDirection] = minProbabilityProximity / minDistanceToCenterCount;
                else if (distanceToCenter == maxDistanceToCenter)
                    directionsProbability[availableCubeDirection] = maxProbabilityProximity / maxDistanceToCenterCount;

                if (_exitCubeDirection == availableCubeDirection)
                    directionsProbability[availableCubeDirection] += 100.0f;

                // Suma de probabilidad según tenga que ser recto o un camino irregular
                // Se usa LastCubeDirection para saber cuál fue el anterior PathCube
                if (availableCubeDirection == lastCubeDirection)
                    directionsProbability[availableCubeDirection] += maxProbabilityIrregularity;
                else
                    directionsProbability[availableCubeDirection] += minProbabilityIrregularity / (availableCubeDirections.Count - subLastCubeDirection);
            }

            return directionsProbability;
        }

        // Crea el PathCube sustituyendo el anterior GrassCube
        private void CreateNextPathCube(Directions _lastCubeDirection)
        {
            lastCubeDirection = _lastCubeDirection;

            Vector3 sumDirection = DirectionFunctions.GetSumDirection(lastCubeDirection, cubeSize);

            // First cube on edge
            Vector3 cubePosition = sumDirection + Paths[^1][^1].Position;
            Cube cube = chunkCubes.TryGetCubeInPosition(cubePosition);

            Cube newCube = SwitchCube(cube, CubeTypes.PATH);
            Paths[^1].Add((PathCube)newCube);
        }

        // Resetea el PathCube o el Path en el caso que no pueda continuar creándolo
        private bool ResetPathCube(Directions _exitCubeDirection)
        {
            // Si hay más de un Path creados mover el Fork al siguiente PathCube disponible
            if (Paths.Count > 1)
                return MoveForkNextPathCube();

            // Reset el último PathCube creado poniéndolo de nuevo a GrassCube
            lastCubeDirection = Paths[^1][^1].LastPathCubeDirection;

            SwitchCube(Paths[^1][^1], CubeTypes.GRASS);

            Paths[^1].RemoveAt(Paths[^1].Count - 1);

            // Prohíbe al camino recorrer de nuevo el Path que no funcionó
            List<Directions> forbiddenDirections = new()
                {
                    lastCubeDirection
                };

            if (_exitCubeDirection != Directions.NULL &&
                !forbiddenDirections.Contains(DirectionFunctions.GetOppositeDirection(_exitCubeDirection)))
            {
                forbiddenDirections.Add(DirectionFunctions.GetOppositeDirection(_exitCubeDirection));
            }

            TryCreateNextPathCube(forbiddenDirections, _exitCubeDirection);

            return true;
        }

        // Elimina el Path creado y empieza el Path desde el siguiente Fork
        private bool MoveForkNextPathCube()
        {
            Cube cube;

            // Resetea el camino creado cambiándolo de nuevo a GrassCubes
            for (int i = 1; i < Paths[^1].Count; i++)
            {
                cube = chunkCubes.TryGetCubeInPosition(Paths[^1][i].Position);
                SwitchCube(cube, CubeTypes.GRASS);
            }

            // Elimina el último Path creado
            Paths.RemoveAt(Paths.Count - 1);

            // Si ha llegado al borde no usarlo como nuevo punto de bifurcación
            if (forkIndex >= Paths[0].Count - 2)
            {
                stopCreatingPath = true;
                isLastPathFinished = true;
                return false;
            }

            forkIndex++;
            Debug.Log($"Fork path didn't find a path, moving the fork path.{forkIndex}/{Paths[0].Count - 2}");

            // Crea de nuevo el Path empezando desde el nuevo Fork
            PathCube forkPathCube = Paths[0][forkIndex];
            Paths.Add(new());
            Paths[^1].Add(Paths[0][forkIndex]);

            lastCubeDirection = forkPathCube.LastPathCubeDirection;

            return true;
        }

        // Comprueba si ha terminado el Path en el Fork o en el borde del Chunk
        private bool HasPathFinished(Directions _exitCubeDirection)
        {
            // Si está creando el Path hasta el Fork, recorrer la mitad del chunk
            // y añadir por probabilidad una serie de PathCubes más
            if (Paths[^1].Count >= size / 2.0f && _exitCubeDirection == Directions.NULL)
            {
                float restPossibleCubes = (size / 2.0f) / 100.0f;
                float diffCubes = Paths[^1].Count - (size / 2.0f);

                float stopProbability = diffCubes / restPossibleCubes;

                if (Random.Range(0, 100) > stopProbability) return false;

                Debug.Log("Path created until fork.");
            }
            // Si está creando el Path hasta el borde del chunk (Por eso tiene salida el cubo)
            else if (_exitCubeDirection != Directions.NULL)
            {
                Vector3 sumDirection = DirectionFunctions.GetSumDirection(_exitCubeDirection, cubeSize);
                Vector3 cubePosition = sumDirection + Paths[^1][^1].Position;

                if (!IsCubePositionOnChunkEdge(cubePosition)) return false;

                // Sustituir el último cubo que está en el borde del Chunk
                Cube cube = chunkCubes.TryGetCubeInPosition(cubePosition);
                Cube newCube = SwitchCube(cube, CubeTypes.PATH);
                Paths[^1].Add((PathCube)newCube);

                if (Paths.Count == 1)
                    initForkIndex = forkIndex = Paths[^1].Count / 2;

                isLastPathFinished = true;
                Debug.Log("Path until edge.");
            }

            stopCreatingPath = true;
            return true;
        }
        #endregion

        #region Cubes
        // Crea un Cube. Instancia su gameObject en caso de ir paso a paso
        private Cube CreateCube(Vector3 _position, CubeTypes _cubeType, Directions _lastCubeDirection = Directions.NULL)
        {
            if (_lastCubeDirection == Directions.NULL) _lastCubeDirection = lastCubeDirection;

            Cube cube = MapCreator.Instance.CreateCube(gameObject.transform, _position, _cubeType, _lastCubeDirection);
            chunkCubes.Cubes.Add(cube);

            return cube;
        }

        // Intercambia un Cube por otro. Elimina el gameObject del anterior si fue creado
        private Cube SwitchCube(Cube _cube, CubeTypes _cubeType, Directions _lastCubeDirection = Directions.NULL)
        {
            Cube newCube = CreateCube(_cube.Position, _cubeType, _lastCubeDirection);

            if (goStepByStep) _cube.DestroyGameObject();

            chunkCubes.RemoveCube(_cube);

            return newCube;
        }

        // Devuelve las direcciones disponibles para generar el siguiente "PathCube"
        private List<Directions> GetAvailableCubeDirections(Vector3 _cubePosition, Directions _lastCubeDirection,
            List<Directions> _forbiddenCubeDirections)
        {
            List<Directions> possibleCubeDirections = DirectionFunctions.AllDirections;

            // Elimina la dirección por la que vino el path
            possibleCubeDirections.Remove(DirectionFunctions.GetOppositeDirection(_lastCubeDirection));
            // Elimina la dirección donde está el chunk de origen para evitar bucles
            possibleCubeDirections.Remove(DirectionFunctions.GetOppositeDirection(lastChunkDirection));

            // Elimina las direcciones prohibidas (Al reconstruir el path)
            foreach (Directions _forbiddenCubeDirection in _forbiddenCubeDirections)
            {
                if (possibleCubeDirections.Contains(_forbiddenCubeDirection))
                    possibleCubeDirections.Remove(_forbiddenCubeDirection);
            }

            List<Directions> availableCubeDirections = new();

            foreach (Directions possibleCubeDirection in possibleCubeDirections)
            {
                Vector3 sumDirection = DirectionFunctions.GetSumDirection(possibleCubeDirection, cubeSize);
                Vector3 checkCubePosition = _cubePosition + sumDirection;

                Cube cube = chunkCubes.TryGetCubeInPosition(checkCubePosition);

                if (cube == null) continue;
                // No puede chocar contra otro "PathCube"
                if (cube.Type == CubeTypes.PATH) continue;
                // No puede ser un cubo en el borde del chunk
                if (IsCubePositionOnChunkEdge(checkCubePosition)) continue;
                // No puede tener "PathCube"s alrededor de su siguiente posición
                if (IsCubePositionNearChunkPathCubes(checkCubePosition, possibleCubeDirection)) continue;

                availableCubeDirections.Add(possibleCubeDirection);
            }

            return availableCubeDirections;
        }

        // Comprueba si la posición del cubo está en el borde del chunk
        private bool IsCubePositionOnChunkEdge(Vector3 _cubePosition)
        {
            float sumSize = (size * cubeSize) - cubeSize;
            if (_cubePosition.x == Position.x || _cubePosition.x == Position.x + sumSize ||
                _cubePosition.z == Position.z || _cubePosition.z == Position.z + sumSize)
                return true;

            return false;
        }

        // Comprueba una posición como resultado de una posición inicial a una dirección si hay "PathCube"s
        private bool IsCubePositionNearChunkPathCubes(Vector3 _cubePosition, Directions _cubeDirection)
        {
            List<Directions> possibleCubeDirections = DirectionFunctions.AllDirections;

            // Elimina la dirección por la que vino el path
            possibleCubeDirections.Remove(DirectionFunctions.GetOppositeDirection(_cubeDirection));

            foreach (Directions cubeDirection in possibleCubeDirections)
            {
                Vector3 sumDirection = DirectionFunctions.GetSumDirection(cubeDirection, cubeSize);
                Vector3 checkCubePosition = _cubePosition + sumDirection;

                Cube cube = chunkCubes.TryGetCubeInPosition(checkCubePosition);

                if (cube == null) continue;
                // No puede chocar contra otro "PathCube"
                if (cube.Type == CubeTypes.PATH) return true;
            }

            return false;
        }
        #endregion
    }
}