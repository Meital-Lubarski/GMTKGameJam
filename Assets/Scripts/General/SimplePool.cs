using System.Collections.Generic;
using UnityEngine;

namespace General
{
    public class SimplePool<T> : MonoSingleton<SimplePool<T>> where T: MonoBehaviour, IPoolable
    {
        [SerializeField] private T prefab;
        [SerializeField] private int poolSize;
        [SerializeField] private int increaseSize;
        private readonly Stack<T> _available = new Stack<T>();

        protected override void Awake()
        {
            base.Awake();

            IncreasePoolSize(poolSize);
        }

        public T Get()
        {
            var pooledObject = TakeAvailable();

            /*
             * Nothing left to hand out: either the pool was set up empty, or
             * everything it made has been taken. Both are answered the same
             * way, by making more.
             */
            if (pooledObject == null)
            {
                IncreasePoolSize(Mathf.Max(1, increaseSize));

                pooledObject = TakeAvailable();
            }

            if (pooledObject == null)
            {
                Debug.LogError(
                    $"{name} could not hand out a {typeof(T).Name}. Check that " +
                    "its Prefab is set.",
                    this
                );

                return null;
            }

            // C# Knows this object is a MonoBehaviour!
            pooledObject.gameObject.SetActive(true);

            // C# Knows this object implements IPoolable!
            pooledObject.Reset();

            if (_available.Count < 1)
            {
                IncreasePoolSize(increaseSize);
            }

            return pooledObject;
        }

        public void Return(T obj)
        {
            if (obj == null)
            {
                return;
            }

            obj.gameObject.SetActive(false);
            _available.Push(obj);
        }

        /// <summary>
        /// The next one that is still there. A pooled object can be destroyed
        /// out from under the pool - by being parented to something in a scene
        /// that is then unloaded, say - and what is left behind is a hole in
        /// the stack rather than an object. Those are dropped on the way past.
        /// </summary>
        private T TakeAvailable()
        {
            while (_available.Count > 0)
            {
                T candidate = _available.Pop();

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void IncreasePoolSize(int size)
        {
            if (prefab == null)
            {
                return;
            }

            for (int i = 0; i < size; i++)
            {
                var instance =  Instantiate(prefab, this.transform);
                Return(instance);
            }
        }
    }
}
