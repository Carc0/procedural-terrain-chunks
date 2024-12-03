using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Cubes
{
    // Los cubos que pertecen a un Chunk
    public class ChunkCubes
    {
        private readonly List<Cube> cubes = new();

        public List<Cube> Cubes { get => cubes; }


        // Añade un nuevo Chunk a la lista
        public void AddCube(Cube _cube)
        {
            cubes.Add(_cube);
        }

        // Elimina un nuevo Chunk de la lista
        public void RemoveCube(Cube _cube)
        {
            cubes.Remove(_cube);
        }

        // Instancia los GameObjects de los cubos
        public void InstantiateCubesGameObjects()
        {
            foreach (Cube cube in cubes)
            {
                cube.InstantiateGameObject();
            }
        }

        // Intenta conseguir un cubo por posición
        public Cube TryGetCubeInPosition(Vector3 _position)
        {
            foreach (Cube cube in cubes)
            {
                if (cube.Position != _position) continue;

                return cube;
            }

            return null;
        }
    }
}