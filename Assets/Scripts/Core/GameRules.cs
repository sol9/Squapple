using System;

namespace Squapple.Core
{
    public sealed class GameRules
    {
        public const int Version = 1;
        public const int TargetSum = 10;
        public const int PointsPerApple = 1;

        public int Width { get; }
        public int Height { get; }
        public int CellCount { get; }
        public double DurationSeconds { get; }

        public GameRules(int width = 8, int height = 13, double durationSeconds = 120)
        {
            if (width < 1)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (durationSeconds <= 0 || double.IsNaN(durationSeconds) || double.IsInfinity(durationSeconds))
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));

            CellCount = checked(width * height);
            if (CellCount < 2)
                throw new ArgumentException("At least two cells are needed for a sum divisible by 10 using digits 1 through 9.");

            Width = width;
            Height = height;
            DurationSeconds = durationSeconds;
        }
    }
}
