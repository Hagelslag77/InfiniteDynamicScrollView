using TMPro;
using UnityEngine;

namespace Hagelslag.InfiniteDynamicScrollView.Examples.SimpleScroll
{

    public class SimpleScrollCell :  VerticalCell<string>
    {
        [SerializeField] private TextMeshProUGUI m_text;

        public override void SetData (string text) => m_text.text = text;

        public override float GetHeight(float width)
        {
            return m_text.GetPreferredValues( width, Mathf.Infinity).y;
        }

    }
}