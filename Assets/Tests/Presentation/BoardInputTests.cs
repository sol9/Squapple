using NUnit.Framework;
using Squapple.Core;
using Squapple.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Squapple.Presentation.Tests
{
    public sealed class BoardInputTests
    {
        [TestCase(393, 650)]
        [TestCase(320, 460)]
        [TestCase(768, 850)]
        public void BoardFitsAvailableAreaWithSquareCells(int width, int height)
        {
            var area = new Rect(-width / 2f, -height / 2f, width, height);
            var board = BoardLayout.Fit(area, 8, 13);
            Assert.That(board.width, Is.LessThanOrEqualTo(width + 0.0001f));
            Assert.That(board.height, Is.LessThanOrEqualTo(height + 0.0001f));
            Assert.That(board.width / 8, Is.EqualTo(board.height / 13).Within(0.0001));
            Assert.That(board.center, Is.EqualTo(area.center));
        }

        [TestCase(-40, 65, 0, 0)]
        [TestCase(40, -65, 7, 12)]
        [TestCase(-25, 50, 1, 1)]
        public void MapsTopLeftAndInclusiveEdges(float x, float y, int expectedX, int expectedY)
        {
            Assert.That(BoardLayout.TryCell(new Rect(-40, -65, 80, 130), 8, 13,
                new Vector2(x, y), out var cell), Is.True);
            Assert.That(cell, Is.EqualTo(new Vector2Int(expectedX, expectedY)));
        }

        [TestCase(-41, 0)]
        [TestCase(41, 0)]
        [TestCase(0, 66)]
        [TestCase(0, -66)]
        public void OutsideBoardDoesNotBecomeAnEdgeSelection(float x, float y)
        {
            Assert.That(BoardLayout.TryCell(new Rect(-40, -65, 80, 130), 8, 13,
                new Vector2(x, y), out _), Is.False);
        }

        private GameObject _root;
        private GameObject _events;
        private GameBoardView _view;
        private RectTransform _rect;
        private int _submissions;
        private CellRectangle _last;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Board test", typeof(RectTransform), typeof(Image), typeof(GameBoardView));
            _rect = _root.GetComponent<RectTransform>();
            _rect.sizeDelta = new Vector2(80, 130);
            _events = new GameObject("Events", typeof(EventSystem));
            _view = _root.GetComponent<GameBoardView>();
            _view.Initialize(new GameRules(), Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            _view.Show(GameBoard.Generate(new GameRules(), 1).CopyCells(), true);
            _submissions = 0;
            _view.SelectionSubmitted += selection => { _submissions++; _last = selection; };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_events);
        }

        private PointerEventData Pointer(int id, float x, float y) => new(_events.GetComponent<EventSystem>())
        {
            pointerId = id,
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, _rect.TransformPoint(new Vector2(x, y)))
        };

        [Test]
        public void SecondPointerCannotFinishFirstPointersDrag()
        {
            _view.OnPointerDown(Pointer(1, -35, 60));
            _view.OnPointerDown(Pointer(2, 35, -60));
            _view.OnPointerUp(Pointer(2, 35, -60));
            Assert.That(_submissions, Is.Zero);
            _view.OnPointerUp(Pointer(1, -25, 50));
            Assert.That(_submissions, Is.EqualTo(1));
            Assert.That(_last.MinX, Is.Zero);
            Assert.That(_last.MaxX, Is.EqualTo(1));
            Assert.That(_last.MaxY, Is.EqualTo(1));
        }

        [Test]
        public void ReleaseOutsideCancelsWithoutSubmitting()
        {
            _view.OnPointerDown(Pointer(1, -35, 60));
            _view.OnDrag(Pointer(1, 100, -100));
            _view.OnPointerUp(Pointer(1, 100, -100));
            Assert.That(_submissions, Is.Zero);
        }

        [Test]
        public void PausingCancelsAnExistingDrag()
        {
            _view.OnPointerDown(Pointer(1, -35, 60));
            _view.Show(GameBoard.Generate(new GameRules(), 1).CopyCells(), false);
            _view.OnPointerUp(Pointer(1, -25, 50));
            Assert.That(_submissions, Is.Zero);
        }

        [Test]
        public void PressOutsideCannotStartSelection()
        {
            _view.OnPointerDown(Pointer(1, -50, 60));
            _view.OnDrag(Pointer(1, 0, 0));
            _view.OnPointerUp(Pointer(1, 0, 0));
            Assert.That(_submissions, Is.Zero);
        }

        [Test]
        public void CancelledDragDoesNotSubmitOnRelease()
        {
            _view.OnPointerDown(Pointer(1, -35, 60));
            _view.OnCancel(new BaseEventData(_events.GetComponent<EventSystem>()));
            _view.OnPointerUp(Pointer(1, -25, 50));
            Assert.That(_submissions, Is.Zero);
        }

        [Test]
        public void RemovalFeedbackDoesNotBlockNextDragAndIsClearedOnPause()
        {
            var cells = GameBoard.Generate(new GameRules(), 1).CopyCells();
            cells[0] = 0;
            _view.Show(cells, true);
            var tile = _root.transform.Find("Cell 0").GetComponent<Image>();
            Assert.That(tile.color.g, Is.GreaterThan(tile.color.r));
            _view.OnPointerDown(Pointer(1, -25, 60));
            _view.OnPointerUp(Pointer(1, -15, 60));
            Assert.That(_submissions, Is.EqualTo(1));
            _view.Show(cells, false);
            Assert.That(tile.color.a, Is.EqualTo(0.18f).Within(0.001));
            Assert.That(_root.transform.Find("Cell 0/Number").GetComponent<Text>().text, Is.Empty);
        }
    }
}
