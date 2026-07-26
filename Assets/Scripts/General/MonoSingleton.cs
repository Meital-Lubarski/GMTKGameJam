using UnityEngine;

namespace General
{
    /// <summary>
    /// A generic Singleton class for MonoBehaviours.
    /// Example usage: public class GameManager : MonoSingleton&lt;GameManager&gt;
    ///
    /// One placed in a scene claims the singleton in its Awake and is made to
    /// outlive that scene. This has to be done when it wakes rather than when
    /// it is first asked for: a scene object that is only ever found by
    /// FindFirstObjectByType is never told to survive, and dies with the scene
    /// it was placed in - which looks exactly like a singleton that was never
    /// persistent at all.
    /// </summary>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindFirstObjectByType<T>();
                if (_instance != null)
                    return _instance;

                var singletonObject = new GameObject(typeof(T).Name);
                _instance = singletonObject.AddComponent<T>();
                DontDestroyOnLoad(singletonObject); // Don't destroy the object when loading a new scene

                /*
                 * An empty one is better than nothing to call, but it has none
                 * of the settings the authored one carries, so it is said out
                 * loud rather than quietly standing in for the real thing.
                 */
                Debug.LogWarning(
                    $"No {typeof(T).Name} was found, so an empty one was " +
                    "created. Anything set up on the real one in the scene " +
                    "is missing from it."
                );

                return _instance;
            }
        }

        /// <summary>
        /// Claims the singleton and makes it outlive its scene. A second one
        /// takes itself away rather than fighting the first over who answers
        /// to <see cref="Instance"/>.
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning(
                    $"A second {typeof(T).Name} was loaded and has been " +
                    "removed. Keep it in one scene only.",
                    this
                );

                Destroy(gameObject);
                return;
            }

            _instance = this as T;

            /*
             * Only root objects can be kept: a child rides along with whatever
             * root it hangs from, and asking for it directly only earns a
             * warning from Unity.
             */
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        // Ensure no other instances can be created by having the constructor as protected
        protected MonoSingleton() { }
    }
}
