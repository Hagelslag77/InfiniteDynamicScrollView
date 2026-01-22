using UnityEngine;

namespace Hagelslag.InfiniteDynamicScrollView.Shared
{
    internal static class RectTransformExtensions
    {
        /// <summary>
        /// Child is fully above the parent's bottom edge.
        /// </summary>
        internal static bool IsFullyAboveParentsBottom(this RectTransform child, RectTransform parent)
        {
            var (childBottom, _) = GetVerticalEdgesInParentSpace(child, parent);
            return parent.IsFullyAboveParentsBottom(childBottom);
        }

        /// <summary>
        /// Child is fully below the parent's top edge.
        /// </summary>
        internal static bool IsFullyBelowParentsTop(this RectTransform child, RectTransform parent)
        {
            var (_, childTop) = GetVerticalEdgesInParentSpace(child, parent);
            return parent.IsFullyBelowParentsTop(childTop);
        }

        /// <summary>
        /// Child is fully below the parent's bottom edge.
        /// </summary>
        internal static bool IsFullyBelowParentsBottom(this RectTransform child, RectTransform parent)
        {
            var (_, childTop) = GetVerticalEdgesInParentSpace(child, parent);
            return parent.IsFullyBelowParentsBottom(childTop);
        }

        /// <summary>
        /// Child is fully above the parent's top edge.
        /// </summary>
        internal static bool IsFullyAboveParentsTop(this RectTransform child, RectTransform parent)
        {
            var (childBottom, _) = GetVerticalEdgesInParentSpace(child, parent);
            return parent.IsFullyAboveParentsTop(childBottom);
        }

        /// <summary>
        /// Child is fully below the parent's top edge.
        /// </summary>
        internal static bool IsFullyBelowParentsTop(this RectTransform parent, float childTop)
            => childTop < parent.rect.yMax;

        /// <summary>
        /// Child is fully above the parent's bottom edge.
        /// </summary>
        internal static bool IsFullyAboveParentsBottom(this RectTransform parent, float childBottom)
            => childBottom > parent.rect.yMin;

        /// <summary>
        /// Child is fully below the parent's bottom edge.
        /// </summary>
        internal static bool IsFullyBelowParentsBottom(this RectTransform parent, float childTop)
            => childTop < parent.rect.yMin;

        /// <summary>
        /// Child is fully above the parent's top edge.
        /// </summary>
        internal static bool IsFullyAboveParentsTop(this RectTransform parent, float childBottom)
            => childBottom > parent.rect.yMax;

        internal static (float bottom, float top) GetVerticalEdgesInParentSpace(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);

            var bottom = float.PositiveInfinity;
            var top = float.NegativeInfinity;

            for (var i = 0; i < 4; i++)
            {
                var y = parent.InverseTransformPoint(corners[i]).y;
                bottom = Mathf.Min(bottom, y);
                top = Mathf.Max(top, y);
            }

            return (bottom, top);
        }

        internal static float DistanceFromTopToParentTop(this RectTransform child, RectTransform parent)
        {
            var childTopInParent =
                parent.InverseTransformPoint(child.TransformPoint(new Vector3(0, child.rect.height, 0)));

            return parent.rect.height - childTopInParent.y;
        }
    }
}