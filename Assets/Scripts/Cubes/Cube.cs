using Assets.Scripts.Map;
using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.Cubes
{
    // Clase principal de los cubos
    public abstract class Cube
    {
        private readonly Transform chunkTransform;
        private readonly Vector3 position;

        protected CubeTypes type;
        private GameObject gameObject;

        public Cube(Transform _chunkTransform, Vector3 _position)
        {
            chunkTransform = _chunkTransform;
            position = _position;
        }

        public Vector3 Position => position;
        public CubeTypes Type => type;
        public GameObject GameObject { get => gameObject; set => gameObject = value; }
        public Transform ChunkTransform => chunkTransform;

        public void InstantiateGameObject()
        {
            MapCreator.Instance.InstantiateCubeGameObject(this);
        }

        public void DestroyGameObject()
        {
            MapCreator.Instance.DestroyCubeGameObject(this);
        }
    }
}