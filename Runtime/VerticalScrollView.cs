using System;
using System.Collections.Generic;
using System.Linq;
using Hagelslag.InfiniteDynamicScrollView.Pool;
using Hagelslag.InfiniteDynamicScrollView.Shared;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hagelslag.InfiniteDynamicScrollView
{
    //TODO Feature: add support for top down
    //TODO Feature: allow other pivots
    public class VerticalScrollView<TData> : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        private struct CellData
        {
            public VerticalCell<TData> Cell;
            public int Index;
            public float Height;
            public float BottomPos;
            public RectTransform RectTransform;
        }

        [Serializable]
        private class Padding
        {
            // ReSharper disable InconsistentNaming
            public float Left;
            public float Right;
            public float Top;
            public float Bottom;
            // ReSharper restore InconsistentNaming
        }

        [Serializable]
        public class ScrollRectEvent : UnityEvent<float> {}

        private enum MovementType
        {
            Unrestricted = ScrollRect.MovementType.Unrestricted,
            Elastic = ScrollRect.MovementType.Elastic,
            Clamped = ScrollRect.MovementType.Clamped
        }

        #region Variables

        [SerializeField] private Padding m_padding;
        [SerializeField] private float m_spacing;

        [SerializeField] private MovementType m_movementType = MovementType.Elastic;
        [SerializeField] private float m_elasticity = 0.1f;
        [SerializeField] private bool m_inertia = true;
        [SerializeField] private float m_decelerationRate = 0.03f;

        [SerializeField] private float m_scrollSensitivity = 1f;

        [SerializeField] private VerticalCell<TData> m_cellPrefab;
        [SerializeField] private Scrollbar.ScrollEvent m_onValueChanged = new();

        private IList<TData> m_data;
        private readonly List<CellData> m_cells = new();

        private float m_currentContentHeight;
        private Rect m_viewRect;
        private float m_contentWidth;
        private bool m_isVisibilityUpdateNeeded;

        private float m_velocity;
        private bool m_isDragging;
        private Vector2 m_lastDragPointerPosition;

        private float m_startPosition;
        private Vector2 m_startPointerPosition;

        private const float Epsilon = 0.001f;

        private RectTransform m_rectTransform;
        private RectTransform RectTransform
            => m_rectTransform ??= GetComponent<RectTransform>();


        private IObjectPool<TData> m_objectPool;
        protected virtual IObjectPool<TData> ObjectPool
            => m_objectPool ??= new ObjectPool<TData>(InstantiateCell, DestroyCell);

        private float scrollPositionInternal;
        private float ScrollPositionInternal
        {
            get => scrollPositionInternal;
            set
            {
                var pref = scrollPositionInternal;
                scrollPositionInternal = value;
                UpdatePosition();
                if(Math.Abs(pref - scrollPositionInternal) > Epsilon)
                    m_onValueChanged.Invoke(scrollPositionInternal);
            }
        }

        #endregion

        #region Public Interface

        public float ScrollPosition
        {
            get => ScrollPositionInternal;
            set
            {
                m_velocity = 0;
                ScrollPositionInternal = value;
            }
        }

        public IEnumerable<VerticalCell<TData>> VisibleCells
            => m_cells.Select(x => x.Cell).Reverse();

        public Scrollbar.ScrollEvent OnValueChanged
        {
            get => m_onValueChanged;
            set => m_onValueChanged = value;
        }

        public void Set(IList<TData> data)
        {
            m_data = data;
            CreateCells();
        }

        public void Add(TData data)
        {
            m_data ??= new List<TData>();
            if (m_data.IsReadOnly)
                m_data = new List<TData>(m_data);

            m_data.Add(data);

            var tmp = ObjectPool.Rent(data, transform);
            tmp.SetData(data);
            var cellHeight = tmp.GetHeight(m_contentWidth);
            ObjectPool.Return(tmp);

            for (var i = 0; i < m_cells.Count; i++)
            {
                var cell = m_cells[i];
                cell.BottomPos += cellHeight + m_spacing;
                m_cells[i] = cell;
            }

            ScrollPositionInternal -= cellHeight + m_spacing;
        }

        public void AddFront(TData data)
        {
            m_data ??= new List<TData>();
            if (m_data.IsReadOnly)
                m_data = new List<TData>(m_data);

            m_data.Insert(0, data);

            for (var i = 0; i < m_cells.Count; i++)
            {
                var cell = m_cells[i];
                cell.Index += 1;
                m_cells[i] = cell;
            }
        }

        public void Clear()
        {
            foreach (var cellData in m_cells)
                ObjectPool.Return(cellData.Cell);

            m_cells.Clear();

            m_currentContentHeight = 0f;
            ScrollPositionInternal = .0f;
            m_velocity = .0f;
            m_isDragging = false;
            m_lastDragPointerPosition = Vector2.zero;
            m_startPosition = .0f;
            m_startPointerPosition = Vector2.zero;
            m_isVisibilityUpdateNeeded = false;
        }

        protected virtual VerticalCell<TData> InstantiateCell(TData data, Transform parent)
        {
            return Instantiate(m_cellPrefab, parent);
        }

        protected virtual void DestroyCell(VerticalCell<TData> cell)
        {
            Destroy(cell.gameObject);
        }

        #endregion

        #region Implementation

        protected override void OnDestroy()
        {
            //TODO: maybe not the best idea to dispose here if it's a custom pool
            ObjectPool.Dispose();

            base.OnDestroy();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            var rect = RectTransform.rect;
            var widthChanged = Math.Abs(rect.width - m_viewRect.width) > Epsilon;
            var heightChanged = Math.Abs(rect.height - m_viewRect.height) > Epsilon;

            m_viewRect = rect;
            m_contentWidth = m_viewRect.width - m_padding.Left - m_padding.Right;

            if (widthChanged)
            {
                Clear();
                CreateCells();
                return;
            }

            if(heightChanged)
                UpdatePosition();
        }

        private void CreateCells()
        {
            if (m_data == null)
                return;

            for (var i = m_data.Count - 1; i >= 0 && m_currentContentHeight <= m_viewRect.height - m_padding.Top; i--)
                CreateCell(i);
        }

        private void CreateCell(int dataIndex)
        {
            var cell = ObjectPool.Rent(m_data[dataIndex], this.transform);
            cell.SetData(m_data[dataIndex]);

            var rt = cell.GetComponent<RectTransform>();
            var cellHeight = cell.GetHeight(m_contentWidth);
            var bottomPos = GetBottomPosForIndex(dataIndex, cellHeight, rt);

            var cellData = new CellData
            {
                BottomPos = bottomPos,
                Height = cellHeight,
                Cell = cell,
                Index = dataIndex,
                RectTransform = rt,
            };

            if (m_cells.Count == 0 || dataIndex > m_cells[0].Index)
                m_cells.Insert(0, cellData);
            else
                m_cells.Add(cellData);

            rt.sizeDelta = new Vector2(m_contentWidth, cellHeight);
            rt.SetLocalPositionY(bottomPos + ScrollPositionInternal);

            var spacing = dataIndex == 0 || dataIndex == m_data.Count - 1 ? 0 : m_spacing;
            m_currentContentHeight += cellHeight + spacing;
        }

        private float GetBottomPosForIndex(int dataIndex, float newCellHeight, RectTransform child)
        {
            if (dataIndex == m_data.Count - 1)
            {
                var parentHeight = RectTransform.rect.height;
                var childHeight = child.rect.height;
                var parentPivotY = RectTransform.pivot.y;
                var childPivotY = child.pivot.y;

                return
                    -parentHeight * parentPivotY
                    + childHeight * childPivotY
                    + m_padding.Bottom;

            }

            var cellDataBelow = m_cells.FirstOrDefault(x => x.Index == dataIndex + 1);
            if (cellDataBelow.Cell is not null)
                return cellDataBelow.BottomPos + cellDataBelow.Height + m_spacing;

            var cellDataAbove = m_cells.First(x => x.Index == dataIndex - 1);
            return cellDataAbove.BottomPos - m_spacing - newCellHeight;
        }

        #endregion

        #region DragHandler
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_velocity = 0f;
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_isDragging = true;
            m_velocity = 0f;
            m_startPosition = ScrollPositionInternal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform) transform, eventData.position,
                eventData.pressEventCamera, out m_startPointerPosition);
            m_lastDragPointerPosition = m_startPointerPosition;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!m_isDragging || eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform) transform, eventData.position,
                    eventData.pressEventCamera, out var currentLocalPoint))
                return;

            var totalDeltaY = (currentLocalPoint.y - m_startPointerPosition.y) * m_scrollSensitivity;
            var unclampedPosition = m_startPosition + totalDeltaY;

            var newPosition = unclampedPosition;
            var offset = CalculateOffset(newPosition);


            if (m_movementType == MovementType.Elastic && !Mathf.Approximately(offset, 0f))
            {
                newPosition += offset - RubberDelta(offset, m_viewRect.height);
            }
            else if (m_movementType == MovementType.Clamped)
            {
                newPosition += offset;
            }

            var deltaTime = Time.unscaledDeltaTime;
            if (deltaTime > 0)
            {
                var frameDeltaY = (currentLocalPoint.y - m_lastDragPointerPosition.y) * m_scrollSensitivity;
                var newVelocity = frameDeltaY / deltaTime;
                m_velocity = Mathf.Lerp(m_velocity, newVelocity, deltaTime * 10f);
            }

            m_lastDragPointerPosition = currentLocalPoint;
            ScrollPositionInternal = newPosition;
        }


        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_isDragging = false;

            if (Mathf.Abs(eventData.delta.y) < Epsilon)
                m_velocity = 0f;
        }

        #endregion

        #region Scrolling

        private float CalculateOffset(float position)
        {
            if (m_movementType == MovementType.Unrestricted || m_data == null || m_data.Count == 0 || m_cells.Count == 0)
                return 0f;

            var isNewestMessageShown = m_cells[0].Index == m_data.Count - 1;
            if (isNewestMessageShown && position > 0)
                return -position;

            var isOldestMessageShown = m_cells[^1].Index == 0;
            if (!isOldestMessageShown)
                return 0f;

            var dist = m_cells[^1].RectTransform.DistanceFromTopToParentTop(RectTransform);
            return dist > 0 ? dist : 0f;
        }


        private static float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - 1 / (Mathf.Abs(overStretching) * 0.55f / viewSize + 1)) * viewSize * Mathf.Sign(overStretching);
        }


        private void Update()
        {
            if (m_isDragging)
                return;

            var deltaTime = Time.unscaledDeltaTime;
            var offset = CalculateOffset(ScrollPositionInternal);

            if(Mathf.Abs(offset) <= Epsilon && Mathf.Abs(m_velocity) <= Epsilon)
                return;

            if (m_movementType == MovementType.Elastic && !Mathf.Approximately(offset, 0f))
            {
                var newPosition = Mathf.SmoothDamp(ScrollPositionInternal, ScrollPositionInternal + offset, ref m_velocity,
                    m_elasticity, Mathf.Infinity, deltaTime);
                ScrollPositionInternal = newPosition;
            }
            else if (m_inertia)
            {
                m_velocity *= Mathf.Pow(m_decelerationRate, deltaTime);
                if (Mathf.Abs(m_velocity) < Epsilon)
                    m_velocity = 0f;

                var newPosition = ScrollPositionInternal + m_velocity * deltaTime;

                if (m_movementType == MovementType.Clamped)
                {
                    var newOffset = CalculateOffset(newPosition);
                    newPosition += newOffset;
                    if (!Mathf.Approximately(newOffset, 0f))
                        m_velocity = 0f;
                }

                ScrollPositionInternal = newPosition;
            }
            else
            {
                m_velocity = 0f;
                if (!Mathf.Approximately(offset, 0f))
                    ScrollPositionInternal += offset;
            }
        }

        private void LateUpdate()
        {
            if(!m_isVisibilityUpdateNeeded)
                return;

            m_isVisibilityUpdateNeeded = UpdateVisibility();
        }

        private void UpdatePosition()
        {
            for (var i = 0; i < m_cells.Count; i++)
                m_cells[i].RectTransform.SetLocalPositionY(m_cells[i].BottomPos + ScrollPositionInternal);

            m_isVisibilityUpdateNeeded = true;
        }

        #endregion

        #region Visibility

        private bool UpdateVisibility()
        {
            //TODO: for the time being only one cell is added/removed to spread the CPU load over more frames. Is this what we want.
            return TryRemoveBottomCells()
                || TryRemoveTopCells()
                || TryCreateBottomCells()
                || TryCreateTopCells();
        }

        private bool TryRemoveBottomCells()
        {
            if (m_cells.Count == 0)
                return false;

            var cellData = m_cells[0];

            //don't remove the top cell if it goes below the view port
            if (cellData.Index == 0)
                return false;

            if (!cellData.RectTransform.IsFullyBelowParentsBottom(RectTransform))
                return false;

            var spacingBelow = cellData.Index == 0 ? 0 : m_spacing;
            m_currentContentHeight -= cellData.RectTransform.rect.yMax + spacingBelow;
            ObjectPool.Return(cellData.Cell);
            m_cells.RemoveAt(0);

            return true;
        }

        private bool TryRemoveTopCells()
        {
            if (m_cells.Count == 0)
                return false;

            var cellData = m_cells[^1];

            //don't remove the bottom cell if it goes above the view port
            if (cellData.Index == m_data.Count - 1)
                return false;

            if(!cellData.RectTransform.IsFullyAboveParentsTop(RectTransform))
                return false;

            var spacingAbove = cellData.Index == m_data.Count - 1 ? 0 : m_spacing;
            m_currentContentHeight -= cellData.RectTransform.rect.yMax + spacingAbove;
            ObjectPool.Return(cellData.Cell);
            m_cells.RemoveAt(m_cells.Count - 1);

            return true;
        }

        private bool TryCreateBottomCells()
        {
            if (m_cells is null || m_cells.Count == 0)
            {
                if(m_data is null || m_data.Count == 0)
                    return false;
                CreateCell(m_data.Count - 1);
                return true;
            }

            var cellData = m_cells[0];
            if (cellData.Index + 1 >= m_data.Count)
                return false;

            if (cellData.RectTransform.IsFullyAboveParentsBottom(RectTransform))
            {
                CreateCell(cellData.Index + 1);
                return true;
            }

            return false;
        }

        private bool TryCreateTopCells()
        {
            var index = m_cells.Count - 1;
            if (index < 0)
                return false;

            var cellData = m_cells[index];

            if (cellData.Index <= 0)
                return false;

            if (cellData.RectTransform.IsFullyBelowParentsTop(RectTransform))
            {
                CreateCell(cellData.Index - 1);
                return true;
            }
            return false;
        }



        #endregion
    }
}