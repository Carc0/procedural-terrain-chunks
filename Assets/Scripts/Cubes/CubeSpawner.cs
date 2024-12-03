using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.Cubes
{
    // Creación y destrucción de los prefabs de cubos
    public class CubeSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject grassCubePrefab;
        [SerializeField] private GameObject pathCubePrefab;

        private float cubeSize;
        public float CubeSize { get => cubeSize; }


        // Crea el Cube según su tipo. InstantiateGameObject cuando MapCreator esté en Step By Step
        public Cube Create(Transform _chunkTransform, Vector3 _position, CubeTypes _cubeType, bool _createGameObject, Directions _lastCubeDirection = Directions.NULL)
        {
            Cube cube;
            switch (_cubeType)
            {
                case CubeTypes.GRASS:
                    cube = new GrassCube(_chunkTransform, _position);
                    break;
                case CubeTypes.PATH:
                    cube = new PathCube(_chunkTransform, _position, _lastCubeDirection);
                    break;
                default:
                    return null;
            }

            if (_createGameObject) InstantiateGameObject(cube);

            return cube;
        }

        // Instancia el GameObject del Cube
        public void InstantiateGameObject(Cube _cube)
        {
            GameObject cubeGameObject;
            switch (_cube.Type)
            {
                case CubeTypes.GRASS:
                    cubeGameObject = Instantiate(grassCubePrefab);
                    break;
                case CubeTypes.PATH:
                    cubeGameObject = Instantiate(pathCubePrefab);
                    break;
                default:
                    return;
            }

            cubeGameObject.transform.position = _cube.Position;
            cubeGameObject.transform.SetParent(_cube.ChunkTransform);
            cubeGameObject.name = $"Cube_{_cube.Type}_{cubeGameObject.transform.localPosition.x}_" +
                $"{cubeGameObject.transform.localPosition.y}_{cubeGameObject.transform.localPosition.z}";

            _cube.GameObject = cubeGameObject;
        }

        // Destruye el gameObject del Cube
        public void DestroyGameObject(Cube _cube)
        {
            DestroyImmediate(_cube.GameObject);
        }

        // Comprueba los cubePrefab y guarda su tamaño
        public bool CheckCubeSize()
        {
            if (grassCubePrefab == null || pathCubePrefab == null)
            {
                Debug.LogError("There aren't cube prefabs.");
                return false;
            }

            if (grassCubePrefab.transform.localScale != pathCubePrefab.transform.localScale)
            {
                Debug.LogError("Cubes haven't the same size.");
                return false;
            }

            if (grassCubePrefab.transform.localScale.x != grassCubePrefab.transform.localScale.z)
            {
                Debug.LogError("Cubes aren't cubes!");
                return false;
            }

            cubeSize = grassCubePrefab.transform.localScale.x;

            return true;
        }
    }
}