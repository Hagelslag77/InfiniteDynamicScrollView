using System.Collections.Generic;
using UnityEngine;

namespace Hagelslag.InfiniteDynamicScrollView.Examples.SimpleScroll
{

    public class MockDataProvider : MonoBehaviour
    {
        private const string Text = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ip";

        [SerializeField] private int m_numMessages = 20;
        [SerializeField] private int m_minChars = 50;
        [SerializeField] private int m_maxChars = 200;

        [SerializeField] private SimpleScrollView m_simpleScrollView;


        private void OnEnable()
        {
            Recreate();
        }

        [ContextMenu( "Recreate")]
        public void Recreate()
        {
            m_simpleScrollView.Clear();
            var messages = new List<string>(m_numMessages);
            for (var i = 0; i < m_numMessages; i++)
            {
                messages.Add($"{i}: {Text[..Random.Range(m_minChars, m_maxChars)]}");
            }

            m_simpleScrollView.Set(messages);
        }
    }
}