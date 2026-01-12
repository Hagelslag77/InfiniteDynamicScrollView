using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hagelslag.InfiniteDynamicScrollView.Pool
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ObjectPool<TData> : IObjectPool<TData>
    {
        private readonly Func<TData, Transform, VerticalCell<TData>> m_createInstance;
        private readonly Action<VerticalCell<TData>> m_destroyInstance;

        private readonly Stack<VerticalCell<TData>> m_pool = new();

        private bool m_isDisposed;

        public ObjectPool(Func<TData, Transform, VerticalCell<TData>> createInstance,
            Action<VerticalCell<TData>> destroyInstance)
        {
            m_createInstance = createInstance;
            m_destroyInstance = destroyInstance;
        }

        /// <summary>
        /// Rent an object from the pool.
        /// </summary>
        public virtual VerticalCell<TData> Rent(TData data, Transform parent)
        {
            var obj = m_pool.Count > 0 ? m_pool.Pop() : m_createInstance.Invoke(data, parent);
            OnActivate(obj);
            return obj;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        public virtual void Return(VerticalCell<TData> obj)
        {
            OnDeactivate(obj);
            m_pool.Push(obj);
        }

        protected virtual void OnActivate(VerticalCell<TData> obj)
        {
            obj.gameObject.SetActive(true);
        }

        protected virtual void OnDeactivate(VerticalCell<TData> obj)
        {
            obj.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_isDisposed)
                return;

            if (disposing)
            {
                while (m_pool.Count > 0)
                    m_destroyInstance?.Invoke(m_pool.Pop());
            }

            m_isDisposed = true;
        }

    }
}