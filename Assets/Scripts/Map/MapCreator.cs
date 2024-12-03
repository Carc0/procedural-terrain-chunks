using Assets.Scripts.Chunks;
using Assets.Scripts.Cubes;
using Assets.Scripts.Utils;
using System;
using UnityEngine;

namespace Assets.Scripts.Map
{
    // Clase principal encargada de la creación del mapa mediante el Editor con las siguientes características:
    // Configuración principal de los Chunks y de los paths
    // Configuración adicional como la seed, si debe ir paso a paso creando el mapa
    // Facade para la creación de cubos y chunks 
    [RequireComponent(typeof(CubeSpawner))]
    public class MapCreator : SimpleSingleton<MapCreator>
    {
        [Header("Chunk configuration")]
        [Range(5, 50)]
        [SerializeField] private int chunkSize = 13;
        [Range(1, 50)]
        [SerializeField] private int chunksNumber = 5;

        [Header("Path configuration")]
        [Range(0.0f, 1.0f)]
        [Tooltip("En 0 el camino irá hacia al centro del chunk. En 1 irá a los bordes.")]
        [SerializeField] private float proximityPathToEdge = 0.5f;
        [Range(0.0f, 1.0f)]
        [Tooltip("En 0 el camino será recto. En 1 será irregular.")]
        [SerializeField] private float irregularityPath = 0.5f;

        [Space(10)]
        [Tooltip("Usar una seed negativa hará crear una seed aleatoria.")]
        [SerializeField] private int seed = -1;
        [Range(0.0f, 100.0f)]
        [Tooltip("La probabilidad que se irá sumando por cada chunk creado para que aparezcan nuevos paths.")]
        [SerializeField] private float sumNewPathsProbability = 10.0f;
        [Space(10)]
        [Tooltip("Para ver el comportamiento del path paso a paso. Usar el botón 'Next step' en Play Mode o Editor Mode." +
            "o si 'goStepByStepSeconds' no es 0.0f en PlayMode irá automáticamente.")]
        [SerializeField] private bool goStepByStep = false;
        [Tooltip("En Play Mode, segundos entre paso y paso en la creación del path.")]
        [SerializeField] private float goStepByStepSeconds = 0.01f;

        private CubeSpawner cubeSpawner;
        private ChunkSpawner chunkSpawner;
        private MapChunks mapChunks;

        private float cubeSize;
        
        private float newPathsProbability;

        private bool isChunkStep = false;
        private bool isForkPathStep = false;
        private bool isInitPathStep = false;
        private bool isEdgePathStep = false;


        private void Awake()
        {
            GetReferences();
        }

        private void GetReferences()
        {
            if (cubeSpawner == null) cubeSpawner = GetComponent<CubeSpawner>();
            if (chunkSpawner == null) chunkSpawner = new ChunkSpawner();
            if (mapChunks == null) mapChunks = new MapChunks();
        }

        #region Map
        // Initializa el mapa: Tamaño del cubo, setear la seed e inicializar variables
        private void Initialize()
        {
            // Comprueba el tamaño de los cubos por si hay nuevos
            if (!cubeSpawner.CheckCubeSize()) return;
            cubeSize = cubeSpawner.CubeSize;

            DeleteAllChunks();

            SeedGenerator.SetSeed(seed);
            newPathsProbability = 0.0f;
        }

        // Crea el mapa
        public void CreateMap()
        {
            GetReferences();

            Initialize();

            // Crea el Chunk inicial
            CreateInitialChunk();

            // Si la flag goStepByStep es true, el mapa se irá generando poco a poco, ya sea con un timer o con el botón Next Step
            if (goStepByStep)
            {
                Debug.Log("Going step by step.");
                isChunkStep = true;

                if (goStepByStepSeconds != 0.0f && Application.isPlaying)
                    StartCoroutine(NextStepCoroutine());

                return;
            }

            // Crea el número de Chunks elegido
            for (int i = 1; i < chunksNumber; i++)
            {
                if (!CreateNextChunk()) break;
            }
        }
        #endregion

        #region Step by step
        // Corrutina de espera entre paso y paso a la hora de generar el camino
        private System.Collections.IEnumerator NextStepCoroutine()
        {
            if (!DoNextStep()) yield break;

            yield return new WaitForSeconds(goStepByStepSeconds);

            StartCoroutine(NextStepCoroutine());
        }

        // Control de los pasos a la hora de generar el camino
        // TODO: Esto debería ser una StateMachine
        public bool DoNextStep()
        {
            if (isChunkStep)
            {
                isChunkStep = false;
                isForkPathStep = true;

                return CreateNextChunk();
            }
            else if (isForkPathStep)
            {
                isForkPathStep = mapChunks.CreateNextPathCubeUntilFork();
                isInitPathStep = !isForkPathStep;
            }
            else if (isInitPathStep)
            {
                isInitPathStep = false;
                isEdgePathStep = CreateNextPath();
                isChunkStep = !isEdgePathStep;

                if (!isEdgePathStep) mapChunks.Chunks[^1].Optimize();
            }
            else if (isEdgePathStep)
            {
                //chunksController.Chunks[^1].InstantiateCubesGameObjects();
                //chunksController.Chunks[^1].Optimize();
                isEdgePathStep = mapChunks.CreateNextPathCubeUntilEdge();
                isInitPathStep = !isEdgePathStep;
            }

            return true;
        }

        // Crea el siguiente Chunk
        public bool CreateNextChunk()
        {
            // Comprueba si ha llegado al máximo de Chunks cuando es Step by Step
            if (chunksNumber <= mapChunks.Chunks.Count)
            {
                Debug.Log("Number of chunks reached.");
                return false;
            }

            // Recupera los paths disponibles del anterior Chunk creado
            mapChunks.SetLastChunkAvailablePaths();

            // Intenta definir la siguiente dirección, posición del chunk y del path según los paths disponibles
            if (!mapChunks.TrySetNextChunkValues(chunkSize, cubeSize))
                return false;

            // Por cada chunk aumenta la probabilidad
            newPathsProbability = Mathf.Min(100.0f, sumNewPathsProbability * mapChunks.Chunks.Count);

            Chunk chunk = CreateChunk($"Chunk_{mapChunks.Chunks.Count}", mapChunks.LastChunkPosition);

            // Crea el camino hacia el fork (Donde se propagarán el/los caminos)
            mapChunks.CreatePathUntilFork();

            // Crea una lista con las posiciones disponibles para el siguiente Chunk
            mapChunks.SetNextAvailableChunksDirections(chunkSize);

            if (goStepByStep) return true;

            // Después de creado el Fork, generación del camino o caminos completos hasta su borde
            bool isInitNextPathFinished;
            do
            {
                isInitNextPathFinished = CreateNextPath();
            } while (isInitNextPathFinished);

            chunk.InstantiateCubesGameObjects();
            chunk.Optimize();

            return true;
        }

        // Crea un Path eligiendo una de las salidas disponibles del último Chunk
        public bool CreateNextPath()
        {
            Directions lastChoosenChunkDirection = mapChunks.GetLastChoosenChunkDirection(newPathsProbability);

            if (lastChoosenChunkDirection == Directions.NULL) return false;

            mapChunks.CreatePathUntilEdge(lastChoosenChunkDirection, proximityPathToEdge, irregularityPath, goStepByStep);

            return true;
        }
        #endregion

        #region Cubes
        // Crea el Cube según su tipo. InstantiateGameObject cuando MapCreator esté en Step By Step
        public Cube CreateCube(Transform _chunkTransform, Vector3 _position, CubeTypes _cubeType, Directions _lastCubeDirection = Directions.NULL)
        {
            return cubeSpawner.Create(_chunkTransform, _position, _cubeType, goStepByStep, _lastCubeDirection);
        }

        // Instancia el GameObject del Cube
        public void InstantiateCubeGameObject(Cube _cube)
        {
            cubeSpawner.InstantiateGameObject(_cube);
        }

        // Destruye el gameObject del Cube
        public void DestroyCubeGameObject(Cube _cube)
        {
            cubeSpawner.DestroyGameObject(_cube);
        }
        #endregion

        #region Chunks
        // Crea el Chunk inicial
        private void CreateInitialChunk()
        {
            Chunk chunk = CreateChunk($"Chunk_{mapChunks.Chunks.Count}", Vector3.zero);
            chunk.CreateFirstPath();
            chunk.InstantiateCubesGameObjects();
            chunk.Optimize();
        }

        // Crea un Chunk y la añade a la lista
        private Chunk CreateChunk(string _name, Vector3 _position)
        {
            Chunk chunk = chunkSpawner.Create(_name, _position, chunkSize, cubeSize);
            chunk.Initialize(proximityPathToEdge, irregularityPath, goStepByStep);
            mapChunks.AddChunk(chunk);

            return chunk;
        }

        // Elimina todos los chunks creados anteriormente
        public void DeleteAllChunks()
        {
            mapChunks.DeleteAllChunks();
        }

        // Destruye el gameObject del Chunk
        public void DestroyChunkGameObject(Chunk _chunk)
        {
            chunkSpawner.DestroyGameObject(_chunk);
        }
        #endregion
    }
}