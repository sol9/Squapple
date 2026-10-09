using UnityEngine;

namespace Squapple.Presentation
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaView : MonoBehaviour
    {
        private Rect _lastArea;
        private Vector2Int _lastSize;

        private void OnEnable() => Apply();
        private void Update()
        {
            if (_lastArea != Screen.safeArea || _lastSize != new Vector2Int(Screen.width, Screen.height))
                Apply();
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return;
            _lastArea = Screen.safeArea;
            _lastSize = new Vector2Int(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(_lastArea.xMin / Screen.width, _lastArea.yMin / Screen.height);
            rect.anchorMax = new Vector2(_lastArea.xMax / Screen.width, _lastArea.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
