namespace Hagelslag.InfiniteDynamicScrollView.Internal
{
    internal interface IPlacementStrategy<in TData>
    {
        void CreateCells(float viewRectHeight, float contentWidth, float scrollPosition);
        float Add(TData data, float contentWidth);
        float AddFront(TData data, float contentWidth);
        void Clear();
        bool TryRemoveBottomCells();
        bool TryRemoveTopCells();
        bool TryCreateBottomCells(float contentWidth, float scrollPosition);
        bool TryCreateTopCells(float contentWidth, float scrollPosition);
        float CalculateOffset(float position);
    }
}