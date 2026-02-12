using System;
using System.Collections.Generic;
using System.Linq;
using Hagelslag.InfiniteDynamicScrollView.Internal;
using Hagelslag.InfiniteDynamicScrollView.Pool;
using Hagelslag.InfiniteDynamicScrollView.Shared;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hagelslag.InfiniteDynamicScrollView
{
    //TODO Feature: add support for top down
    public class VerticalScrollView<TData> : UIBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IPointerDownHandler,
        IPlacementContext<TData>
    {
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
        public class ScrollRectEvent : UnityEvent<float>
        {
        }

        private enum MovementType
        {
            Unrestricted = ScrollRect.MovementType.Unrestricted,
            Elastic = ScrollRect.MovementType.Elastic,
            Clamped = ScrollRect.MovementType.Clamped
        }

        private enum PlacementType
        {
            BottomToTop,
            TopToBottom
        }

        #region Variables

        [SerializeField] private Padding m_padding;
        [SerializeField] private float m_spacing;

        [SerializeField] private PlacementType m_placement = PlacementType.BottomToTop;
        [SerializeField] private MovementType m_movementType = MovementType.Elastic;
        [SerializeField] private float m_elasticity = 0.1f;
        [SerializeField] private bool m_inertia = true;
        [SerializeField] private float m_decelerationRate = 0.03f;

        [SerializeField] private float m_scrollSensitivity = 1f;

        [SerializeField] private VerticalCell<TData> m_cellPrefab;
        [SerializeField] private Scrollbar.ScrollEvent m_onValueChanged = new();

        private List<TData> m_data;
        private readonly List<CellData<TData>> m_cells = new();

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

        private IPlacementStrategy<TData> m_placementStrategy;

        private IPlacementStrategy<TData> PlacementStrategy
            => m_placementStrategy
                ??= m_placement == PlacementType.BottomToTop
                    ? new BottomToTopStrategy<TData>(this)
                    : new TopToBottomStrategy<TData>(this);

        private float scrollPosition;

        public float ScrollPosition
        {
            get => scrollPosition;
            private set
            {
                var pref = scrollPosition;
                scrollPosition = value;
                UpdatePosition();
                if (Math.Abs(pref - scrollPosition) > Epsilon)
                    m_onValueChanged.Invoke(scrollPosition);
            }
        }

        #endregion

        #region Public Interface

        public IReadOnlyList<TData> Data => m_data?.AsReadOnly();

        public IEnumerable<VerticalCell<TData>> VisibleCells
            => m_placement == PlacementType.TopToBottom
                ? m_cells.Select(x => x.Cell)
                : m_cells.Select(x => x.Cell).Reverse();

        public Scrollbar.ScrollEvent OnValueChanged
        {
            get => m_onValueChanged;
            set => m_onValueChanged = value;
        }

        public void Set(IList<TData> data)
        {
            m_data = data.ToList();
            CreateCells();
        }

        public void Add(TData data)
        {
            m_data ??= new List<TData>();
            m_data.Add(data);

            ScrollPosition += PlacementStrategy.Add(data, m_contentWidth);
        }

        public void AddFront(TData data)
        {
            m_data ??= new List<TData>();
            m_data.Insert(0, data);

            ScrollPosition += PlacementStrategy.AddFront(data,m_contentWidth);
        }

        public void Clear()
        {
            foreach (var cellData in m_cells)
                ObjectPool.Return(cellData.Cell);

            PlacementStrategy?.Clear();

            m_cells.Clear();
            m_data?.Clear();

            ScrollPosition = .0f;
            m_velocity = .0f;
            m_isDragging = false;
            m_lastDragPointerPosition = Vector2.zero;
            m_startPosition = .0f;
            m_startPointerPosition = Vector2.zero;
            m_isVisibilityUpdateNeeded = false;
        }

        public void ScrollToCell(int dataIndex)
        {
            if (m_data == null || dataIndex < 0 || dataIndex >= m_data.Count)
                throw new IndexOutOfRangeException("Index out of range.");

            m_velocity = 0f;
            m_isDragging = false;

            var first = m_data.Take(dataIndex + 1).ToList();
            var second = m_data.Skip(dataIndex + 1).ToList();

            Clear();
            Set(first);

            foreach (var data in second)
                Add(data);
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

            if (heightChanged)
                UpdatePosition();
        }

        private void CreateCells()
        {
            if (m_data == null)
                return;

            PlacementStrategy.CreateCells(m_viewRect.height, m_contentWidth, ScrollPosition);
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
            m_startPosition = ScrollPosition;
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
            ScrollPosition = newPosition;
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
            if (m_movementType == MovementType.Unrestricted
                || m_data == null
                || m_data.Count == 0
                || m_cells.Count == 0)
                return 0f;

            return PlacementStrategy.CalculateOffset(position);
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
            var offset = CalculateOffset(ScrollPosition);

            if (Mathf.Abs(offset) <= Epsilon && Mathf.Abs(m_velocity) <= Epsilon)
                return;

            if (m_movementType == MovementType.Elastic && !Mathf.Approximately(offset, 0f))
            {
                var newPosition = Mathf.SmoothDamp(ScrollPosition, ScrollPosition + offset,
                    ref m_velocity,
                    m_elasticity, Mathf.Infinity, deltaTime);
                ScrollPosition = newPosition;
            }
            else if (m_inertia)
            {
                m_velocity *= Mathf.Pow(m_decelerationRate, deltaTime);
                if (Mathf.Abs(m_velocity) < Epsilon)
                    m_velocity = 0f;

                var newPosition = ScrollPosition + m_velocity * deltaTime;

                if (m_movementType == MovementType.Clamped)
                {
                    var newOffset = CalculateOffset(newPosition);
                    newPosition += newOffset;
                    if (!Mathf.Approximately(newOffset, 0f))
                        m_velocity = 0f;
                }

                ScrollPosition = newPosition;
            }
            else
            {
                m_velocity = 0f;
                if (!Mathf.Approximately(offset, 0f))
                    ScrollPosition += offset;
            }
        }

        private void LateUpdate()
        {
            if (!m_isVisibilityUpdateNeeded)
                return;

            m_isVisibilityUpdateNeeded = UpdateVisibility();
        }

        private void UpdatePosition()
        {
            for (var i = 0; i < m_cells.Count; i++)
                m_cells[i].RectTransform.SetLocalPositionY(m_cells[i].ReferencePos + ScrollPosition);

            m_isVisibilityUpdateNeeded = true;
        }

        #endregion

        #region Visibility

        private bool UpdateVisibility()
        {
            //TODO: for the time being only one cell is added/removed to spread the CPU load over more frames. Is this what we want.
            return PlacementStrategy.TryRemoveBottomCells()
                   || PlacementStrategy.TryRemoveTopCells()
                   || PlacementStrategy.TryCreateBottomCells(m_contentWidth, ScrollPosition)
                   || PlacementStrategy.TryCreateTopCells(m_contentWidth, ScrollPosition);
        }

        #endregion

        #region IPlacementContext Implementation

        RectTransform IPlacementContext<TData>.RectTransform => RectTransform;

        float IPlacementContext<TData>.TopPadding => m_padding.Top;

        float IPlacementContext<TData>.BottomPadding => m_padding.Bottom;

        float IPlacementContext<TData>.Spacing => m_spacing;

        IObjectPool<TData> IPlacementContext<TData>.ObjectPool => ObjectPool;
        List<CellData<TData>> IPlacementContext<TData>.Cells => m_cells;

        #endregion
    }
}