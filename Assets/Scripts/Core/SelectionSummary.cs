namespace Squapple.Core
{
    public readonly struct SelectionSummary
    {
        public long Sum { get; }
        public int AppleCount { get; }

        public bool CanRemove => Sum == GameRules.TargetSum;
        public int Points => CanRemove ? AppleCount * GameRules.PointsPerApple : 0;

        internal SelectionSummary(long sum, int appleCount)
        {
            Sum = sum;
            AppleCount = appleCount;
        }
    }
}
