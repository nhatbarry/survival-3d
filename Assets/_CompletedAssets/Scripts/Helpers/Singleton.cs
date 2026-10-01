using UnityEngine;

namespace CaptainPinkTurd.Core.DesignPattern.Singleton
{
    public class Singleton<T> : MonoBehaviour where T : Component 
    {
        protected static T instance;

        public static bool HasInstance => instance;
        public static T TryGetInstance() => HasInstance ? instance : null;

        public static T Instance 
        {
            get 
            {
                if (!instance)
                {
                    instance = FindAnyObjectByType<T>();
                    if (!instance) 
                    {
                        Debug.LogError($"No {typeof(T).Name} singleton found, Auto-Generating one.");
                        var go = new GameObject(typeof(T).Name + " Auto-Generated");
                        instance = go.AddComponent<T>();
                    }
                }

                return instance;
            }
        }

        static Singleton()
        {
            SingletonResetRegistry.Register(ResetStaticState);
        }

        // Clears the cached instance (and destroys any leftover GameObject) from a previous
        // Play session. See SingletonResetRegistry.
        private static void ResetStaticState()
        {
            if (instance)
            {
                Destroy(instance.gameObject);
            }
            instance = null;
        }

        /// <summary>
        /// Make sure to call base.Awake() in override if you need awake.
        /// </summary>
        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying) return;

            instance = this as T;
        }
    }
}