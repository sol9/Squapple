using System;

namespace Squapple.Core
{
    public readonly struct CellRectangle
    {
        public int MinX { get; }
        public int MinY { get; }
        public int MaxX { get; }
        public int MaxY { get; }

        // Both endpoints are included, regardless of drag direction.
        public CellRectangle(int startX, int startY, int endX, int endY)
        {
            MinX = Math.Min(startX, endX);
            MinY = Math.Min(startY, endY);
            MaxX = Math.Max(startX, endX);
            MaxY = Math.Max(startY, endY);
        }
    }
}
