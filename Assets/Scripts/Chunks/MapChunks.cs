using Assets.Scripts.Cubes;
using Assets.Scripts.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Chunks
{
    // Los chunks que corresponden al mapa
    public class MapChunks
    {
        private readonly List<Chunk> chunks = new();
        private readonly Dictionary<Vector3, int> availablePathsPositions = new();

        private Directions lastChunkDirection = Directions.NULL;
        private Vector3 lastChunkPosition = Vector3.zero;
        private Vector3 lastChunkAvailablePathPosition;

        private Directions lastChoosenChunkDirection;
        private List<Directions> availableChunksDirections;

        public List<Chunk> Chunks { get => chunks; }
        public Vector3 LastChunkPosition { get => lastChunkPosition; }


        // Añade un nuevo Chunk a la lista
        public void AddChunk(Chunk _chunk)
        {  
            chunks.Add(_chunk); 
        }

        // Elimina todos los chunks creados anteriormente
        public void DeleteAllChunks()
        {
            foreach (Chunk chunk in chunks)
            {
                chunk.DestroyGameObject();
            }

            chunks.Clear();
            availablePathsPositions.Clear();
        }

        // Comprueba si hay un Chunk en la posición dada
        public Chunk TryGetChunkInPosition(Vector3 _position)
        {
            foreach (Chunk chunk in chunks)
            {
                if (chunk.Position != _position) continue;

                return chunk;
            }

            return null;
        }

        // Crea una lista con las posiciones disponibles para crear el siguiente Chunk
        public void SetNextAvailableChunksDirections(float _chunkSize)
        {
            List<Directions> possibleChunkDirections = DirectionFunctions.AllDirections;

            possibleChunkDirections.Remove(DirectionFunctions.GetOppositeDirection(lastChunkDirection));

            availableChunksDirections = new();

            foreach (Directions possibleChunkDirection in possibleChunkDirections)
            {
                Vector3 sumDirection = DirectionFunctions.GetSumDirection(possibleChunkDirection, _chunkSize);
                Vector3 checkChunkPosition = chunks[^1].Position + sumDirection;

                Chunk chunk = TryGetChunkInPosition(checkChunkPosition);

                if (chunk != null) continue;

                availableChunksDirections.Add(possibleChunkDirection);
            }
        }

        // Introduce en el diccionario los paths creados por el último Chunk
        public void SetLastChunkAvailablePaths()
        {
            List<List<PathCube>> paths = chunks[^1].Paths;
            for (int j = 0; j < paths.Count; j++)
            {
                availablePathsPositions.Add(paths[j][^1].Position, chunks.Count - 1);
            }
        }

        // Intenta definir la siguiente dirección, posición del chunk y del path según los paths disponibles
        public bool TrySetNextChunkValues(float _chunkSize, float _cubeSize)
        {
            bool isChunkInPosition = true;
            do
            {
                if (availablePathsPositions.Count == 0)
                {
                    Debug.Log("There are no more paths available. Possibly the path was closed between chunks.");
                    return false;
                }

                int randomIndex = Random.Range(0, availablePathsPositions.Count);
                lastChunkAvailablePathPosition = availablePathsPositions.ElementAt(randomIndex).Key;
                int chunkIndex = availablePathsPositions.ElementAt(randomIndex).Value;

                availablePathsPositions.Remove(lastChunkAvailablePathPosition);

                float sumSize = (_chunkSize * _cubeSize) - _cubeSize;
                if (lastChunkAvailablePathPosition.z == chunks[chunkIndex].Position.z + sumSize)
                {
                    lastChunkDirection = Directions.TOP;
                    lastChunkPosition = chunks[chunkIndex].Position + new Vector3(0.0f, 0.0f, (_chunkSize * _cubeSize));
                }
                else if (lastChunkAvailablePathPosition.z == chunks[chunkIndex].Position.z)
                {
                    lastChunkDirection = Directions.BOT;
                    lastChunkPosition = chunks[chunkIndex].Position + new Vector3(0.0f, 0.0f, -(_chunkSize * _cubeSize));
                }
                else if (lastChunkAvailablePathPosition.x == chunks[chunkIndex].Position.x + sumSize)
                {
                    lastChunkDirection = Directions.RIGHT;
                    lastChunkPosition = chunks[chunkIndex].Position + new Vector3((_chunkSize * _cubeSize), 0.0f, 0.0f);
                }
                else if (lastChunkAvailablePathPosition.x == chunks[chunkIndex].Position.x)
                {
                    lastChunkDirection = Directions.LEFT;
                    lastChunkPosition = chunks[chunkIndex].Position + new Vector3(-(_chunkSize * _cubeSize), 0.0f, 0.0f);
                }

                if (lastChunkDirection != Directions.NULL)
                    isChunkInPosition = TryGetChunkInPosition(lastChunkPosition) != null;
            } while (isChunkInPosition);

            return true;
        }

        public Directions GetLastChoosenChunkDirection(float _newPathsProbability)
        {
            Chunk chunk = chunks[^1];

            if (availableChunksDirections.Count == 0)
            {
                Debug.Log("No Available Chunks Directions.");
                return Directions.NULL;
            }

            if (chunk.IsLastPathFinished && Random.Range(0.0f, 100.0f) > _newPathsProbability)
                return Directions.NULL;

            int random = Random.Range(0, availableChunksDirections.Count);

            lastChoosenChunkDirection = availableChunksDirections[random];

            availableChunksDirections.Remove(lastChoosenChunkDirection);

            return lastChoosenChunkDirection;
        }

        // Crea para el último chunk el path hasta la bifurcación
        public void CreatePathUntilFork()
        {
            chunks[^1].CreatePathUntilFork(lastChunkAvailablePathPosition, lastChunkDirection);
        }

        // Crea para el último chunk el path hasta el borde
        public void CreatePathUntilEdge(Directions _choosenChunkDirection,
            float _proximityPathToEdge, float _irregularityPath, bool _goStepByStep)
        {
            chunks[^1].CreatePathUntilEdge(lastChoosenChunkDirection);
        }

        // Crea para el último chunk el siguiente PathCube hasta la bifurcación paso a paso
        public bool CreateNextPathCubeUntilFork()
        {
            return chunks[^1].TryCreateNextPathCube(new());
        }

        // Crea para el último chunk el siguiente PathCube hasta el borde paso a paso
        public bool CreateNextPathCubeUntilEdge()
        {
            return chunks[^1].TryCreateNextPathCube(new(), lastChoosenChunkDirection);
        }
    }
}