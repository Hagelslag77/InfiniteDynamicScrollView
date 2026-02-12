using System.Collections.Generic;
using Hagelslag.InfiniteDynamicScrollView.Pool;
using UnityEngine;

namespace Hagelslag.InfiniteDynamicScrollView.Internal
{
    internal interface IPlacementContext<TData>
    {
        List<CellData<TData>> Cells { get; }
        IReadOnlyList<TData> Data { get; }

        RectTransform RectTransform { get; }
        float TopPadding { get; }
        float BottomPadding { get; }
        float Spacing { get; }
        IObjectPool<TData> ObjectPool { get; }
    }
}