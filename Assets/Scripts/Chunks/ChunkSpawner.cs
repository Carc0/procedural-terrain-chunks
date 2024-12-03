using UnityEngine;

namespace Assets.Scripts.Chunks
{
    // Creación y destrucción de los chunks
    public class ChunkSpawner
    {
        // Crea el Chunk
        public Chunk Create(string _name, Vector3 _position, int _size, float _cubeSize)
        {
            Chunk chunk = new(_name, _position, _size, _cubeSize);
            return chunk;
        }

        // Destruye el gameObject del Chunk
        public void DestroyGameObject(Chunk _chunk)
        {
            Object.DestroyImmediate(_chunk.GameObject);
        }
    }
}