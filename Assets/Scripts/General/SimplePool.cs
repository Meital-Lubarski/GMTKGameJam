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

        private void Awake()
        {
            IncreasePoolSize(poolSize);
        }

        public T Get()
        {
            var pooledObject = _available.Pop();
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
            obj.gameObject.SetActive(false);
            _available.Push(obj);
        }

        private void IncreasePoolSize(int size)
        {
            for (int i = 0; i < size; i++)
            {
                var instance =  Instantiate(prefab, this.transform);
                Return(instance);
            }
        }
    }
}