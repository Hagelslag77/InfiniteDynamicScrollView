using System;
using Hagelslag.InfiniteDynamicScrollView;
using Hagelslag.InfiniteDynamicScrollView.Pool;
using UnityEngine;

namespace Hagelslag.Samples.InfiniteDynamicScrollView.MultiplePrefabs
{
    public sealed class MultiPrefabObjectPool : IObjectPool<CellData>
    {
        // one pool per prefab type
        private readonly ObjectPool<CellData> m_leftPool;
        private readonly ObjectPool<CellData> m_rightPool;

        public MultiPrefabObjectPool(Func<CellData, Transform, VerticalCell<CellData>> createInstance,
            Action<VerticalCell<CellData>> destroyInstance)
        {
            m_leftPool = new ObjectPool<CellData>(createInstance, destroyInstance);
            m_rightPool = new ObjectPool<CellData>(createInstance, destroyInstance);
        }

        public void Dispose()
        {
            m_leftPool?.Dispose();
            m_rightPool?.Dispose();
        }

        public VerticalCell<CellData> Rent(CellData data, Transform parent)
        {
            return data.Alignment == CellData.Alignments.Left
                ? m_leftPool.Rent(data, parent)
                : m_rightPool.Rent(data, parent);
        }

        public void Return(VerticalCell<CellData> cell)
        {
            var baseScrollCell = cell as BaseScrollCell;

            if (baseScrollCell!.Data.Alignment == CellData.Alignments.Left)
                m_leftPool.Return(cell);
            else
                m_rightPool.Return(cell);
        }
    }
}