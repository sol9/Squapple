using UnityEngine;

namespace Squapple.Presentation
{
    public static class BoardLayout
    {
        public static Rect Fit(Rect area, int columns, int rows)
        {
            var pitch = Mathf.Min(area.width / columns, area.height / rows);
            var size = new Vector2(columns * pitch, rows * pitch);
            return new Rect(area.center - size * 0.5f, size);
        }

        // Row zero is the top of the board. Borders belong to the nearest edge cell.
        public static bool TryCell(Rect board, int columns, int rows, Vector2 point, out Vector2Int cell)
        {
            cell = default;
            if (board.width <= 0 || board.height <= 0 || point.x < board.xMin || point.x > board.xMax ||
                point.y < board.yMin || point.y > board.yMax)
                return false;

            cell = new Vector2Int(
                Mathf.Min(columns - 1, Mathf.FloorToInt((point.x - board.xMin) / board.width * columns)),
                Mathf.Min(rows - 1, Mathf.FloorToInt((board.yMax - point.y) / board.height * rows)));
            return true;
        }
    }
}
