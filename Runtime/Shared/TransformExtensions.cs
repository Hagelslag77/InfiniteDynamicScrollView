using UnityEngine;

namespace Hagelslag.InfiniteDynamicScrollView.Shared
{
    internal static class TransformExtensions
    {
        internal static void SetLocalPositionX(this Transform source, float value)
            => source.SetLocalPositionByIndex(0, value);

        internal static void SetLocalPositionY(this Transform source, float value)
            => source.SetLocalPositionByIndex(1, value);

        internal static void SetLocalPositionZ(this Transform source, float value)
            => source.SetLocalPositionByIndex(2, value);

        private static void SetLocalPositionByIndex(this Transform source, int index, float value)
        {
            var pos = source.localPosition;
            pos[index] = value;
            source.localPosition = pos;
        }

    }
}