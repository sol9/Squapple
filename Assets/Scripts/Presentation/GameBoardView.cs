using System;
using System.Collections.Generic;
using Squapple.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Squapple.Presentation
{
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class GameBoardView : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler,
        IDragHandler, IPointerUpHandler, ICancelHandler
    {
        public event Action<CellRectangle?> SelectionChanged;
        public event Action<CellRectangle> SelectionSubmitted;

        private RectTransform _rect;
        private GameRules _rules;
        private Text[] _numbers;
        private Image[] _tiles;
        private Image _selection;
        private IReadOnlyList<int> _cells;
        private int? _pointer;
        private Vector2Int _start;
        private CellRectangle _selected;
        private bool _interactable;
        private Rect _board;
        private double[] _flashUntil;
        private static readonly Color TileColor = new(0.86f, 0.89f, 0.92f);
        private static readonly Color EmptyColor = new(0.86f, 0.89f, 0.92f, 0.18f);

        public void Initialize(GameRules rules, Font font)
        {
            CancelSelection();
            _rules = rules;
            _rect = (RectTransform)transform;
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            _numbers = new Text[rules.CellCount];
            _tiles = new Image[rules.CellCount];
            _flashUntil = new double[rules.CellCount];
            for (var i = 0; i < rules.CellCount; i++)
            {
                var tile = new GameObject($"Cell {i}", typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(transform, false);
                _tiles[i] = tile.GetComponent<Image>();
                _tiles[i].raycastTarget = false;
                var label = new GameObject("Number", typeof(RectTransform), typeof(Text));
                label.transform.SetParent(tile.transform, false);
                var labelRect = (RectTransform)label.transform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.sizeDelta = Vector2.zero;
                _numbers[i] = label.GetComponent<Text>();
                _numbers[i].font = font;
                _numbers[i].alignment = TextAnchor.MiddleCenter;
                _numbers[i].color = new Color(0.12f, 0.14f, 0.18f);
                _numbers[i].raycastTarget = false;
            }
            var selection = new GameObject("Selection", typeof(RectTransform), typeof(Image), typeof(Outline));
            selection.transform.SetParent(transform, false);
            _selection = selection.GetComponent<Image>();
            _selection.raycastTarget = false;
            var outline = selection.GetComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.35f, 0.75f);
            outline.effectDistance = new Vector2(2, -2);
            selection.SetActive(false);
            Layout();
        }

        public void Show(IReadOnlyList<int> cells, bool interactable)
        {
            _interactable = interactable;
            if (!interactable)
            {
                CancelSelection();
                ClearFlashes();
            }
            if (ReferenceEquals(_cells, cells))
                return;
            for (var i = 0; i < cells.Count; i++)
            {
                if (interactable && _cells != null && _cells[i] != 0 && cells[i] == 0)
                    _flashUntil[i] = Time.realtimeSinceStartupAsDouble + 0.15;
                _numbers[i].text = cells[i] == 0 ? "" : cells[i].ToString();
                _tiles[i].color = _flashUntil[i] > 0 ? new Color(0.15f, 0.5f, 0.3f, 0.4f) :
                    cells[i] == 0 ? EmptyColor : TileColor;
            }
            _cells = cells;
        }

        private void Update()
        {
            if (_flashUntil == null)
                return;
            for (var i = 0; i < _flashUntil.Length; i++)
            {
                if (_flashUntil[i] == 0 || Time.realtimeSinceStartupAsDouble < _flashUntil[i])
                    continue;
                _flashUntil[i] = 0;
                _tiles[i].color = _cells[i] == 0 ? EmptyColor : TileColor;
            }
        }

        private void ClearFlashes()
        {
            if (_flashUntil == null)
                return;
            for (var i = 0; i < _flashUntil.Length; i++)
            {
                _flashUntil[i] = 0;
                _tiles[i].color = _cells != null && _cells[i] == 0 ? EmptyColor : TileColor;
            }
        }

        public void SetSelectionValid(bool valid)
        {
            _selection.color = valid ? new Color(0.15f, 0.5f, 0.3f, 0.2f) : new Color(0.15f, 0.35f, 0.75f, 0.15f);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_interactable || _pointer.HasValue || eventData.button != PointerEventData.InputButton.Left ||
                !TryPoint(eventData, out var point) || !BoardLayout.TryCell(_board, _rules.Width, _rules.Height, point, out _start))
                return;
            _pointer = eventData.pointerId;
            UpdateSelection(_start);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_pointer != eventData.pointerId || !TryPoint(eventData, out var point))
                return;
            point.x = Mathf.Clamp(point.x, _board.xMin, _board.xMax);
            point.y = Mathf.Clamp(point.y, _board.yMin, _board.yMax);
            if (BoardLayout.TryCell(_board, _rules.Width, _rules.Height, point, out var cell))
                UpdateSelection(cell);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_pointer != eventData.pointerId)
                return;
            var point = Vector2.zero;
            var submit = _interactable && TryPoint(eventData, out point) &&
                BoardLayout.TryCell(_board, _rules.Width, _rules.Height, point, out _);
            // Refresh the endpoint even when no Drag event was delivered for the final movement.
            if (submit)
            {
                BoardLayout.TryCell(_board, _rules.Width, _rules.Height, point, out var cell);
                _selected = new CellRectangle(_start.x, _start.y, cell.x, cell.y);
            }
            var rectangle = _selected;
            CancelSelection();
            if (submit)
                SelectionSubmitted?.Invoke(rectangle);
        }

        public void OnCancel(BaseEventData eventData) => CancelSelection();
        private void OnDisable()
        {
            CancelSelection();
            ClearFlashes();
        }
        private void OnRectTransformDimensionsChange() => Layout();

        public void CancelSelection()
        {
            var hadPointer = _pointer.HasValue;
            _pointer = null;
            if (_selection != null)
                _selection.gameObject.SetActive(false);
            if (hadPointer)
                SelectionChanged?.Invoke(null);
        }

        private bool TryPoint(PointerEventData data, out Vector2 point) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, data.position, data.pressEventCamera, out point);

        private void UpdateSelection(Vector2Int end)
        {
            _selected = new CellRectangle(_start.x, _start.y, end.x, end.y);
            var pitch = _board.width / _rules.Width;
            var min = new Vector2(_board.xMin + _selected.MinX * pitch, _board.yMax - (_selected.MaxY + 1) * pitch);
            var size = new Vector2(_selected.MaxX - _selected.MinX + 1, _selected.MaxY - _selected.MinY + 1) * pitch;
            Place(_selection.rectTransform, new Rect(min, size));
            _selection.gameObject.SetActive(true);
            SelectionChanged?.Invoke(_selected);
        }

        private void Layout()
        {
            if (_rules == null || _numbers == null)
                return;
            CancelSelection();
            _board = BoardLayout.Fit(_rect.rect, _rules.Width, _rules.Height);
            var pitch = _board.width / _rules.Width;
            for (var i = 0; i < _numbers.Length; i++)
            {
                var x = i % _rules.Width;
                var y = i / _rules.Width;
                Place(_tiles[i].rectTransform, new Rect(_board.xMin + x * pitch + 2,
                    _board.yMax - (y + 1) * pitch + 2, Mathf.Max(0, pitch - 4), Mathf.Max(0, pitch - 4)));
                _numbers[i].fontSize = Mathf.Max(12, Mathf.RoundToInt(pitch * 0.48f));
            }
        }

        private void Place(RectTransform target, Rect bounds)
        {
            target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = bounds.center - _rect.rect.center;
            target.sizeDelta = bounds.size;
        }
    }
}
