using System.Collections.Generic;
using UnityEngine;

namespace Hagelslag.InfiniteDynamicScrollView.Examples.MultiplePrefabs
{

    public class MockDataProvider : MonoBehaviour
    {
        private const string Text = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ip";

        [SerializeField] private int m_numMessages = 20;
        [SerializeField] private int m_minChars = 50;
        [SerializeField] private int m_maxChars = 200;

        [SerializeField] private MultiplePrefabsScrollView m_simpleScrollView;


        private void OnEnable()
        {
            Recreate();
        }

        [ContextMenu( "Recreate")]
        public void Recreate()
        {
            m_simpleScrollView.Clear();
            var messages = new List<CellData>(m_numMessages);
            for (var i = 0; i < m_numMessages; i++)
            {
                var alignment = Random.Range(0, 2) == 0 ? CellData.Alignments.Left : CellData.Alignments.Right;
                var text = $"{alignment}: {Text[..Random.Range(m_minChars, m_maxChars)]}";
                var data = new CellData(text, alignment);
                messages.Add(data);
            }

            m_simpleScrollView.Set(messages);
        }
    }
}