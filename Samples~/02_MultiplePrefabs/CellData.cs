namespace Hagelslag.Samples.InfiniteDynamicScrollView.MultiplePrefabs
{
    public sealed class CellData
    {
        public enum Alignments
        {
            Left,
            Right
        }

        public string Text { get;  }
        public Alignments Alignment { get; }

        public CellData(string text, Alignments alignment)
        {
            Text = text;
            Alignment = alignment;
        }
    }
}