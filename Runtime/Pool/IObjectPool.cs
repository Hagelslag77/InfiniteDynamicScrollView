using System;
using UnityEngine;

namespace Hagelslag.InfiniteDynamicScrollView.Pool
{
    public interface IObjectPool<TData> : IDisposable
    {
        /// <summary>
        /// Rent an object from the pool.
        /// </summary>
        VerticalCell<TData> Rent(TData data, Transform parent);

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        void Return(VerticalCell<TData> obj);
    }
}