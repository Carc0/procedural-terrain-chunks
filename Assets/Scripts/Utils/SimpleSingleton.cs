using UnityEngine;

namespace Assets.Scripts.Utils
{
    // Singleton básico
    public class SimpleSingleton<T> : MonoBehaviour where T : Component
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();

                    if (instance == null)
                    {
                        var gameObject = new GameObject();
                        instance = gameObject.AddComponent<T>();
                    }
                }

                return instance;
            }
        }
    }
}