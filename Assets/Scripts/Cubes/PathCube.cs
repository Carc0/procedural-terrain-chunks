using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.Cubes
{
    // Los cubos utilizados en la creación de los caminos
    // LastPathCubeDirection indica en que dirección se encuentra el anterior CubePath creado
    public class PathCube : Cube
    {
        protected readonly Directions lastPathCubeDirection;

        public PathCube(Transform _chunkTransform, Vector3 _position, Directions _lastPathCubeDirection) : base(_chunkTransform, _position)
        {
            type = CubeTypes.PATH;
            lastPathCubeDirection = _lastPathCubeDirection;
        }

        public Directions LastPathCubeDirection { get => lastPathCubeDirection; }
    }
}