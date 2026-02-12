using UnityEngine;

namespace Hagelslag.InfiniteDynamicScrollView.Internal
{
    internal struct CellData<TData>
    {
        // ReSharper disable InconsistentNaming
        public VerticalCell<TData> Cell;
        public int Index;
        public float Height;
        public float ReferencePos;
        public RectTransform RectTransform;
        // ReSharper restore InconsistentNaming
    }
}