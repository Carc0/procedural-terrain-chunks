using UnityEngine;

namespace Assets.Scripts
{
    // Generador de seeds
    public static class SeedGenerator
    {
        public static void SetSeed(int _seed)
        {
            if (_seed < 0) _seed = Random.Range(0, 9999999);

            Debug.Log("Seed created: " + _seed);

            Random.InitState(_seed);
        }
    }
}