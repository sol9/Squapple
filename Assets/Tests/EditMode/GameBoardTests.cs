using System;
using System.Linq;
using NUnit.Framework;

namespace Squapple.Core.Tests
{
    public class GameBoardTests
    {
        [Test]
        public void DefaultRulesMatchReleaseBaseline()
        {
            var rules = new GameRules();
            Assert.That(rules.Width, Is.EqualTo(8));
            Assert.That(rules.Height, Is.EqualTo(13));
            Assert.That(rules.DurationSeconds, Is.EqualTo(120));
            Assert.That(GameRules.TargetSum, Is.EqualTo(10));
            Assert.That(GameRules.PointsPerApple, Is.EqualTo(1));
        }

        [TestCase(0, 2)]
        [TestCase(2, 0)]
        [TestCase(-1, 2)]
        [TestCase(2, -1)]
        [TestCase(1, 1)]
        public void InvalidDimensionsAreRejected(int width, int height)
        {
            Assert.That(() => new GameRules(width, height), Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidDurationIsRejected(double duration)
        {
            Assert.That(() => new GameRules(2, 1, duration), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(2, 1)]
        [TestCase(1, 2)]
        [TestCase(3, 2)]
        [TestCase(8, 13)]
        [TestCase(13, 8)]
        public void GeneratedBoardsKeepDigitAndTotalConstraints(int width, int height)
        {
            var rules = new GameRules(width, height);
            for (var seed = -128; seed < 128; seed++)
            {
                var board = GameBoard.Generate(rules, seed);
                var cells = board.CopyCells();
                Assert.That(cells.Length, Is.EqualTo(width * height));
                Assert.That(cells.All(value => value >= 1 && value <= 9), Is.True, $"Seed {seed}");
                Assert.That(cells.Sum() % 10, Is.Zero, $"Seed {seed}");
                Assert.That(board.RemainingApples, Is.EqualTo(cells.Length));
            }
        }

        [Test]
        public void SameSeedReproducesBoardWithinThisRuntime()
        {
            var rules = new GameRules();
            Assert.That(GameBoard.Generate(rules, 42).CopyCells(), Is.EqualTo(GameBoard.Generate(rules, 42).CopyCells()));
        }

        [Test]
        public void NonSquareBoardUsesRowMajorCoordinates()
        {
            var board = new GameBoard(new GameRules(3, 2), new[] { 1, 2, 3, 4, 5, 6 });
            Assert.That(board[2, 0], Is.EqualTo(3));
            Assert.That(board[0, 1], Is.EqualTo(4));
            Assert.That(board[2, 1], Is.EqualTo(6));
            Assert.That(() => board[3, 0], Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(0, 0, 1, 0)]
        [TestCase(1, 0, 0, 0)]
        public void SuccessfulSelectionAwardsOnlyRemovedApplesAndPreservesPositions(int x1, int y1, int x2, int y2)
        {
            var board = new GameBoard(new GameRules(3, 2), new[] { 5, 5, 4, 3, 7, 6 });
            var rectangle = new CellRectangle(x1, y1, x2, y2);
            var preview = board.InspectSelection(rectangle);
            Assert.That(preview.Sum, Is.EqualTo(10));
            Assert.That(preview.AppleCount, Is.EqualTo(2));
            Assert.That(board.TryRemove(rectangle), Is.EqualTo(2));
            Assert.That(board.CopyCells(), Is.EqualTo(new[] { 0, 0, 4, 3, 7, 6 }));
            Assert.That(board.RemainingApples, Is.EqualTo(4));
            Assert.That(board.TryRemove(rectangle), Is.Zero);
            Assert.That(board.RemainingApples, Is.EqualTo(4));
        }

        [Test]
        public void ReverseRectangleIncludesBothAxesAndEmptyCellsDoNotScore()
        {
            var board = new GameBoard(new GameRules(2, 2), new[] { 5, 0, 0, 5 });
            Assert.That(board.TryRemove(new CellRectangle(1, 1, 0, 0)), Is.EqualTo(2));
            Assert.That(board.RemainingApples, Is.Zero);
            Assert.That(board.HasMoves(), Is.False);
        }

        [TestCase(0, 0, 0, 0)]
        [TestCase(0, 0, 2, 0)]
        [TestCase(-1, 0, 1, 0)]
        [TestCase(0, 0, 3, 0)]
        [TestCase(0, -1, 1, 0)]
        [TestCase(0, 0, 1, 1)]
        public void InvalidSelectionsLeaveBoardUnchanged(int x1, int y1, int x2, int y2)
        {
            var cells = new[] { 5, 5, 4 };
            var board = new GameBoard(new GameRules(3, 1), cells);
            Assert.That(board.TryRemove(new CellRectangle(x1, y1, x2, y2)), Is.Zero);
            Assert.That(board.CopyCells(), Is.EqualTo(cells));
            Assert.That(board.RemainingApples, Is.EqualTo(3));
        }

        [Test]
        public void EmptySelectionDoesNotRemoveOrScore()
        {
            var board = new GameBoard(new GameRules(2, 1), new[] { 0, 0 });
            var selection = board.InspectSelection(new CellRectangle(0, 0, 1, 0));
            Assert.That(selection.CanRemove, Is.False);
            Assert.That(selection.AppleCount, Is.Zero);
            Assert.That(board.TryRemove(new CellRectangle(0, 0, 1, 0)), Is.Zero);
            Assert.That(board.HasMoves(), Is.False);
        }

        [Test]
        public void BoardOwnsCopiesOfInputAndExportedCells()
        {
            var cells = new[] { 5, 5 };
            var board = new GameBoard(new GameRules(2, 1), cells);
            cells[0] = 9;
            var exported = board.CopyCells();
            exported[1] = 9;
            Assert.That(board.CopyCells(), Is.EqualTo(new[] { 5, 5 }));
        }

        [Test]
        public void MalformedSnapshotsAreRejected()
        {
            var rules = new GameRules(2, 1);
            Assert.That(() => new GameBoard(rules, new[] { 5 }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new GameBoard(rules, new[] { 0, 10 }), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new GameBoard(rules, new[] { -1, 5 }), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NoMovesDoesNotMeanBoardIsEmpty()
        {
            var board = new GameBoard(new GameRules(3, 1), new[] { 5, 5, 4 });
            Assert.That(board.HasMoves(), Is.True);
            board.TryRemove(new CellRectangle(0, 0, 1, 0));
            Assert.That(board.HasMoves(), Is.False);
            Assert.That(board.RemainingApples, Is.EqualTo(1));
        }

        [Test]
        public void MoveSearchFindsRectangleSpanningBothRowsAndColumns()
        {
            var board = new GameBoard(new GameRules(2, 2), new[] { 1, 2, 3, 4 });
            Assert.That(board.HasMoves(), Is.True);
            Assert.That(board.InspectSelection(new CellRectangle(0, 0, 1, 1)).Points, Is.EqualTo(4));
            Assert.That(board.CopyCells(), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void MoveSearchFindsVerticalSelectionAcrossAnEmptyCell()
        {
            var board = new GameBoard(new GameRules(2, 3), new[] { 5, 9, 0, 9, 5, 9 });
            Assert.That(board.HasMoves(), Is.True);
            Assert.That(board.TryRemove(new CellRectangle(0, 2, 0, 0)), Is.EqualTo(2));
            Assert.That(board.HasMoves(), Is.False);
        }

        [Test]
        public void MoveSearchIncludesLastRowAndRightEdge()
        {
            var board = new GameBoard(new GameRules(3, 2), new[] { 9, 9, 9, 9, 5, 5 });
            Assert.That(board.HasMoves(), Is.True);
            Assert.That(board.TryRemove(new CellRectangle(2, 1, 1, 1)), Is.EqualTo(2));
            Assert.That(board.HasMoves(), Is.False);
        }

        [Test]
        public void MoveSearchMatchesIndependentBruteForceIncludingHoles()
        {
            var random = new Random(824);
            var rules = new GameRules(4, 3);
            for (var sample = 0; sample < 300; sample++)
            {
                var cells = Enumerable.Range(0, rules.CellCount).Select(_ => random.Next(0, 10)).ToArray();
                var board = new GameBoard(rules, cells);
                Assert.That(board.HasMoves(), Is.EqualTo(BruteForceHasMoves(cells, rules.Width, rules.Height)), $"Sample {sample}");
            }
        }

        [Test]
        public void InitialSnapshotAndSameActionsReproduceBoardAndPoints()
        {
            var rules = new GameRules(3, 2);
            var initial = new[] { 5, 5, 4, 3, 7, 6 };
            var first = new GameBoard(rules, initial);
            var replay = new GameBoard(rules, initial);
            var actions = new[]
            {
                new CellRectangle(0, 0, 1, 0),
                new CellRectangle(0, 1, 1, 1),
                new CellRectangle(2, 0, 2, 1)
            };
            var points = 0;
            foreach (var action in actions)
            {
                var awarded = first.TryRemove(action);
                Assert.That(replay.TryRemove(action), Is.EqualTo(awarded));
                Assert.That(replay.CopyCells(), Is.EqualTo(first.CopyCells()));
                points += awarded;
            }
            Assert.That(points, Is.EqualTo(6));
            Assert.That(first.RemainingApples, Is.Zero);
            Assert.That(first.HasMoves(), Is.False);
            Assert.That(initial, Is.EqualTo(new[] { 5, 5, 4, 3, 7, 6 }));
        }

        private static bool BruteForceHasMoves(int[] cells, int width, int height)
        {
            for (var top = 0; top < height; top++)
            for (var left = 0; left < width; left++)
            for (var bottom = top; bottom < height; bottom++)
            for (var right = left; right < width; right++)
            {
                var sum = 0;
                for (var y = top; y <= bottom; y++)
                for (var x = left; x <= right; x++)
                    sum += cells[y * width + x];
                if (sum == 10)
                    return true;
            }
            return false;
        }
    }
}
