using System.Linq;
using Hagelslag.InfiniteDynamicScrollView.Shared;
using UnityEngine;

namespace Hagelslag.InfiniteDynamicScrollView.Internal
{
    internal sealed class BottomToTopStrategy<TData> : IPlacementStrategy<TData>
    {
        private readonly IPlacementContext<TData> m_context;
        private float m_currentContentHeight;

        internal BottomToTopStrategy(IPlacementContext<TData> context)
        {
            m_context = context;
        }

        public void CreateCells(float viewRectHeight, float contentWidth, float scrollPosition)
        {
            for (var i = m_context.Data.Count - 1;
                 i >= 0 && m_currentContentHeight <= viewRectHeight - m_context.TopPadding;
                 i--)
                CreateCell(i, contentWidth,scrollPosition);
        }

        public float Add(TData data, float contentWidth)
        {
            var tmp = m_context.ObjectPool.Rent(data, m_context.RectTransform);
            tmp.SetData(data);
            var cellHeight = tmp.GetHeight(contentWidth);
            m_context.ObjectPool.Return(tmp);

            for (var i = 0; i < m_context.Cells.Count; i++)
            {
                var cell = m_context.Cells[i];
                cell.ReferencePos += cellHeight + m_context.Spacing;
                m_context.Cells[i] = cell;
            }

            return -(cellHeight + m_context.Spacing);
        }

        public float AddFront(TData data, float contentWidth)
        {
            for (var i = 0; i < m_context.Cells.Count; i++)
            {
                var cell = m_context.Cells[i];
                cell.Index += 1;
                m_context.Cells[i] = cell;
            }

            return 0f;
        }

        public void Clear()
        {
            m_currentContentHeight = 0f;
        }

        public bool TryRemoveBottomCells()
        {
            if (m_context.Cells.Count == 0)
                return false;

            var cellData = m_context.Cells[0];

            //don't remove the top cell if it goes below the view port
            if (cellData.Index == 0)
                return false;

            if (!cellData.RectTransform.IsFullyBelowParentsBottom(m_context.RectTransform))
                return false;

            var spacingBelow = cellData.Index == 0 ? 0 : m_context.Spacing;
            m_currentContentHeight -= cellData.RectTransform.rect.yMax + spacingBelow;
            m_context.ObjectPool.Return(cellData.Cell);
            m_context.Cells.RemoveAt(0);

            return true;
        }

        public bool TryRemoveTopCells()
        {
            if (m_context.Cells.Count == 0)
                return false;

            var cellData = m_context.Cells[^1];

            //don't remove the bottom cell if it goes above the view port
            if (cellData.Index == m_context.Data.Count - 1)
                return false;

            if (!cellData.RectTransform.IsFullyAboveParentsTop(m_context.RectTransform))
                return false;

            var spacingAbove = cellData.Index == m_context.Data.Count - 1 ? 0 : m_context.Spacing;
            m_currentContentHeight -= cellData.RectTransform.rect.yMax + spacingAbove;
            m_context.ObjectPool.Return(cellData.Cell);
            m_context.Cells.RemoveAt(m_context.Cells.Count - 1);

            return true;
        }

        public bool TryCreateBottomCells(float contentWidth, float scrollPosition)
        {
            if (m_context.Cells is null || m_context.Cells.Count == 0)
            {
                if (m_context.Data is null || m_context.Data.Count == 0)
                    return false;
                CreateCell(m_context.Data.Count - 1, contentWidth,scrollPosition);
                return true;
            }

            var cellData = m_context.Cells[0];
            if (cellData.Index + 1 >= m_context.Data.Count)
                return false;

            if (cellData.RectTransform.IsFullyAboveParentsBottom(m_context.RectTransform,
                    -m_context.BottomPadding))
            {
                CreateCell(cellData.Index + 1, contentWidth,scrollPosition);
                return true;
            }

            return false;
        }

        public bool TryCreateTopCells(float contentWidth, float scrollPosition)
        {
            var index = m_context.Cells.Count - 1;
            if (index < 0)
                return false;

            var cellData = m_context.Cells[index];

            if (cellData.Index <= 0)
                return false;

            if (cellData.RectTransform.IsFullyBelowParentsTop(m_context.RectTransform, m_context.TopPadding))
            {
                CreateCell(cellData.Index - 1, contentWidth,scrollPosition);
                return true;
            }

            return false;
        }

        public float CalculateOffset(float position)
        {
            var isLatestCellShown = m_context.Cells[0].Index == m_context.Data.Count - 1;
            if (isLatestCellShown && position > 0)
                return -position;

            var isFirstCellShown = m_context.Cells[^1].Index == 0;
            if (!isFirstCellShown)
                return 0f;

            var dist = m_context.Cells[^1].RectTransform.DistanceFromTopToParentTop(m_context.RectTransform)
                       - m_context.TopPadding;

            return dist > 0 ? dist : 0f;
        }


        private void CreateCell(int dataIndex, float contentWidth, float scrollPosition)
        {
            var cell = m_context.ObjectPool.Rent(m_context.Data[dataIndex], m_context.RectTransform);
            cell.SetData(m_context.Data[dataIndex]);

            var rt = cell.GetComponent<RectTransform>();
            var cellHeight = cell.GetHeight(contentWidth);
            var referencePos = GetReferencePosForIndex(dataIndex, cellHeight, rt);

            var cellData = new CellData<TData>
            {
                ReferencePos = referencePos,
                Height = cellHeight,
                Cell = cell,
                Index = dataIndex,
                RectTransform = rt,
            };

            if (m_context.Cells.Count == 0 || dataIndex > m_context.Cells[0].Index)
                m_context.Cells.Insert(0, cellData);
            else
                m_context.Cells.Add(cellData);

            rt.sizeDelta = new Vector2(contentWidth, cellHeight);
            rt.SetLocalPositionY(referencePos + scrollPosition);

            var spacing = dataIndex == 0 || dataIndex == m_context.Data.Count - 1 ? 0 : m_context.Spacing;
            m_currentContentHeight += cellHeight + spacing;
        }

        private float GetReferencePosForIndex(int dataIndex, float newCellHeight, RectTransform child)
        {
            var childPivotY = child.pivot.y;

            if (dataIndex == m_context.Data.Count - 1)
            {
                var parentHeight = m_context.RectTransform.rect.height;
                var parentPivotY = m_context.RectTransform.pivot.y;

                return
                    -parentHeight * parentPivotY
                    + newCellHeight * childPivotY
                    + m_context.BottomPadding;
            }

            var cellDataBelow = m_context.Cells.FirstOrDefault(x => x.Index == dataIndex + 1);
            if (cellDataBelow.Cell is not null)
            {
                return cellDataBelow.ReferencePos
                       + (1.0f - cellDataBelow.RectTransform.pivot.y) * cellDataBelow.Height
                       + newCellHeight * childPivotY
                       + m_context.Spacing;
            }

            var cellDataAbove = m_context.Cells.First(x => x.Index == dataIndex - 1);
            return cellDataAbove.ReferencePos - (cellDataAbove.RectTransform.pivot.y * cellDataAbove.Height)
                                              - m_context.Spacing
                                              - newCellHeight * (1.0f - childPivotY);
        }
    }
}