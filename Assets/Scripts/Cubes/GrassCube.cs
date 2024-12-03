using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.Cubes
{
    // Cubo utilizado para la hierba
    public class GrassCube : Cube
    {
        public GrassCube(Transform _chunkTransform, Vector3 _position) : base(_chunkTransform, _position)
        {
            type = CubeTypes.GRASS;
        }
    }
}