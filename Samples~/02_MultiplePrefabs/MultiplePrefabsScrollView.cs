using Hagelslag.InfiniteDynamicScrollView;
using Hagelslag.InfiniteDynamicScrollView.Pool;
using UnityEngine;

namespace Hagelslag.Samples.InfiniteDynamicScrollView.MultiplePrefabs
{
    public sealed class MultiplePrefabsScrollView : VerticalScrollView<CellData>
    {
        [SerializeField] private BaseScrollCell m_leftCellPrefab;
        [SerializeField] private BaseScrollCell m_rightCellPrefab;


        private MultiPrefabObjectPool m_objectPool;

        // here we use a custom object pool to select the correct prefab
        protected override IObjectPool<CellData> ObjectPool => m_objectPool ??= new MultiPrefabObjectPool(InstantiateCell, DestroyCell);

        protected override VerticalCell<CellData> InstantiateCell(CellData data, Transform parent)
        {
            // here we decide which prefab to use
            // this would also the right place to let a dependency injection framework instantiate the cell
            var cellPrefab = data.Alignment == CellData.Alignments.Left ? m_leftCellPrefab : m_rightCellPrefab;
            return Instantiate(cellPrefab, parent);
        }
    }
}