using UnityEngine;

namespace Hagelslag.InfiniteDynamicScrollView
{
    public abstract class VerticalCell<TData> : MonoBehaviour
    {
        public abstract void SetData(TData data);
        public abstract float GetHeight(float width);
    }
}