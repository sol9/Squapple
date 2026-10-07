using Squapple.Core;

namespace Squapple.Gameplay
{
    public readonly struct SelectionResolved
    {
        public CellRectangle Rectangle { get; }
        public SelectionSummary Summary { get; }
        public double ElapsedSeconds { get; }

        internal SelectionResolved(CellRectangle rectangle, SelectionSummary summary, double elapsedSeconds)
        {
            Rectangle = rectangle;
            Summary = summary;
            ElapsedSeconds = elapsedSeconds;
        }
    }
}
