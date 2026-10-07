using System;
using System.Collections.Generic;

namespace Squapple.Core
{
    public sealed class GameBoard
    {
        private readonly int[] _cells;

        public GameRules Rules { get; }
        public int RemainingApples { get; private set; }

        // Row-major cells. Zero represents an empty cell; inputs are copied.
        public GameBoard(GameRules rules, IReadOnlyList<int> cells)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));

            if (cells == null)
                throw new ArgumentNullException(nameof(cells));
            if (cells.Count != rules.CellCount)
                throw new ArgumentException("Cell count must match the board dimensions.", nameof(cells));

            _cells = new int[cells.Count];
            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i] < 0 || cells[i] >= GameRules.TargetSum)
                    throw new ArgumentOutOfRangeException(nameof(cells), "Cells must contain 0 through 9.");

                _cells[i] = cells[i];
                if (cells[i] != 0)
                    RemainingApples++;
            }
        }

        public int this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= Rules.Width)
                    throw new ArgumentOutOfRangeException(nameof(x));
                if (y < 0 || y >= Rules.Height)
                    throw new ArgumentOutOfRangeException(nameof(y));

                return _cells[y * Rules.Width + x];
            }
        }

        public static GameBoard Generate(GameRules rules, int seed)
        {
            if (rules == null)
                throw new ArgumentNullException(nameof(rules));

            var random = new Random(seed);
            var cells = new int[rules.CellCount];
            var remainder = 0;
            for (var i = 0; i < cells.Length - 2; i++)
            {
                cells[i] = random.Next(1, GameRules.TargetSum);
                remainder = (remainder + cells[i]) % GameRules.TargetSum;
            }

            // Exclude the penultimate digit that would force the final digit to be 10.
            var excluded = GameRules.TargetSum - remainder;
            var penultimate = random.Next(1, excluded == GameRules.TargetSum ? 10 : 9);
            if (penultimate >= excluded)
                penultimate++;

            cells[^2] = penultimate;
            cells[^1] = GameRules.TargetSum - (remainder + penultimate) % GameRules.TargetSum;

            return new GameBoard(rules, cells);
        }

        public int[] CopyCells() => (int[])_cells.Clone();

        public SelectionSummary InspectSelection(CellRectangle rectangle)
        {
            if (rectangle.MinX < 0 || rectangle.MaxX >= Rules.Width)
                return default;
            if (rectangle.MinY < 0 || rectangle.MaxY >= Rules.Height)
                return default;

            long sum = 0;
            var count = 0;
            for (var y = rectangle.MinY; y <= rectangle.MaxY; y++)
            {
                for (var x = rectangle.MinX; x <= rectangle.MaxX; x++)
                {
                    var value = _cells[y * Rules.Width + x];
                    sum += value;
                    if (value != 0)
                        count++;
                }
            }


            return new SelectionSummary(sum, count);
        }

        // Returns the awarded points. Invalid selections leave the board unchanged.
        public int TryRemove(CellRectangle rectangle)
        {
            var selection = InspectSelection(rectangle);
            if (!selection.CanRemove)
                return 0;

            for (var y = rectangle.MinY; y <= rectangle.MaxY; y++)
            {
                for (var x = rectangle.MinX; x <= rectangle.MaxX; x++)
                {
                    _cells[y * Rules.Width + x] = 0;
                }
            }


            RemainingApples -= selection.AppleCount;
            return selection.Points;
        }

        public bool HasMoves()
        {
            if (RemainingApples == 0)
                return false;

            var columnSums = new long[Rules.Width];
            for (var top = 0; top < Rules.Height; top++)
            {
                Array.Clear(columnSums, 0, columnSums.Length);
                for (var bottom = top; bottom < Rules.Height; bottom++)
                {
                    for (var x = 0; x < Rules.Width; x++)
                    {
                        columnSums[x] += _cells[bottom * Rules.Width + x];
                    }

                    for (var left = 0; left < Rules.Width; left++)
                    {
                        long sum = 0;
                        for (var right = left; right < Rules.Width; right++)
                        {
                            sum += columnSums[right];
                            if (sum == GameRules.TargetSum)
                                return true;
                            if (sum > GameRules.TargetSum)
                                break;
                        }
                    }
                }
            }

            return false;
        }
    }
}
