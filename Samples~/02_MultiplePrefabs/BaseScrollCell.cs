using Hagelslag.InfiniteDynamicScrollView;
using TMPro;
using UnityEngine;

namespace Hagelslag.Samples.InfiniteDynamicScrollView.MultiplePrefabs
{
    public class BaseScrollCell : VerticalCell<CellData>
    {
        [SerializeField] private TextMeshProUGUI m_text;

        public CellData Data { get; private set; }

        public override void SetData(CellData data)
        {
            Data = data;
            m_text.text = Data.Text;
        }

        public override float GetHeight(float width)
        {
            // The HorizontalLayoutGroup has padding of 200 to move the cells to the left or the right.
            // So the width for the text is 200 px smaller
            var preferredWith = Mathf.Max(0f, width - 200f);
            return m_text.GetPreferredValues(preferredWith, Mathf.Infinity).y;
        }
    }
}