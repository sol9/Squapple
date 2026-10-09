using Squapple.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class PlayPrototypeBuilder
{
    private const string PrefabPath = "Assets/Prefabs/PlayPrototype.prefab";

    [MenuItem("Tools/Squapple/Create Play Prototype")]
    public static void Create()
    {
        if (GameObject.Find("PlayPrototype") != null)
            throw new System.InvalidOperationException("PlayPrototype already exists in the open scene.");
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/IBMPlexSansKR-Regular.ttf");
        var root = Node("PlayPrototype", null);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(393, 852);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0;
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<Image>().color = new Color(0.97f, 0.97f, 0.97f);
        var controller = root.AddComponent<PlayPrototype>();
        var safe = Node("SafeArea", root.transform);
        safe.AddComponent<SafeAreaView>();
        var home = Panel("Home", safe.transform);
        Label("Title", home.transform, "사각사과", font, 32, new Vector2(0.5f, 0.7f), new Vector2(340, 60));
        Label("Rules", home.transform, "합이 10이 되도록 사각형으로 묶으세요.\n사과 한 개당 1점 · 제한 시간 120초", font, 18, new Vector2(0.5f, 0.56f), new Vector2(350, 100));
        var start = Button("Start", home.transform, "시작", font, new Vector2(0.5f, 0.39f));
        var play = Panel("Play", safe.transform);
        var timer = Label("Timer", play.transform, "2:00", font, 26, new Vector2(0.5f, 1), new Vector2(110, 52), new Vector2(0, -38));
        var score = Label("Score", play.transform, "점수 0", font, 18, new Vector2(1, 1), new Vector2(110, 52), new Vector2(-65, -38));
        var pauseButton = Button("Pause", play.transform, "정지", font, new Vector2(0, 1), new Vector2(50, -38), new Vector2(76, 48));
        var sum = Label("SelectionSum", play.transform, "선택 합: —", font, 19, new Vector2(0.5f, 1), new Vector2(320, 40), new Vector2(0, -87));
        var feedback = Label("Feedback", play.transform, "", font, 14, new Vector2(0.5f, 1), new Vector2(350, 30), new Vector2(0, -120));
        var boardObject = Node("Board", play.transform);
        var boardRect = (RectTransform)boardObject.transform;
        boardRect.offsetMin = new Vector2(12, 18);
        boardRect.offsetMax = new Vector2(-12, -145);
        boardObject.AddComponent<Image>().color = new Color(1, 1, 1, 0);
        var board = boardObject.AddComponent<GameBoardView>();
        var pause = Modal("PauseScreen", safe.transform);
        var pauseText = Label("PauseText", pause.transform, "일시정지", font, 18, new Vector2(0.5f, 0.62f), new Vector2(340, 130));
        var resume = Button("Resume", pause.transform, "계속하기", font, new Vector2(0.5f, 0.44f));
        var quit = Button("Quit", pause.transform, "그만하고 홈으로", font, new Vector2(0.5f, 0.33f));
        var result = Modal("Result", safe.transform);
        var resultText = Label("ResultText", result.transform, "이번 점수", font, 24, new Vector2(0.5f, 0.62f), new Vector2(350, 160));
        var newGame = Button("NewGame", result.transform, "새 판", font, new Vector2(0.5f, 0.42f));
        var homeButton = Button("Home", result.transform, "홈으로", font, new Vector2(0.5f, 0.31f));

        var serialized = new SerializedObject(controller);
        Assign(serialized, "font", font);
        Assign(serialized, "home", home);
        Assign(serialized, "play", play);
        Assign(serialized, "pause", pause);
        Assign(serialized, "result", result);
        Assign(serialized, "board", board);
        Assign(serialized, "timer", timer);
        Assign(serialized, "score", score);
        Assign(serialized, "selectionSum", sum);
        Assign(serialized, "feedback", feedback);
        Assign(serialized, "resultText", resultText);
        Assign(serialized, "pauseText", pauseText);
        Assign(serialized, "startButton", start);
        Assign(serialized, "pauseButton", pauseButton);
        Assign(serialized, "resumeButton", resume);
        Assign(serialized, "quitButton", quit);
        Assign(serialized, "newGameButton", newGame);
        Assign(serialized, "homeButton", homeButton);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        play.SetActive(false);
        pause.SetActive(false);
        result.SetActive(false);
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        Undo.RegisterCreatedObjectUndo(root, "Create Play Prototype");
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create UI EventSystem");
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
    }

    private static void Assign(SerializedObject target, string field, Object value) =>
        target.FindProperty(field).objectReferenceValue = value;

    private static GameObject Node(string name, Transform parent)
    {
        var node = new GameObject(name, typeof(RectTransform));
        if (parent != null)
            node.transform.SetParent(parent, false);
        var rect = (RectTransform)node.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        return node;
    }

    private static GameObject Panel(string name, Transform parent) => Node(name, parent);

    private static GameObject Modal(string name, Transform parent)
    {
        var panel = Node(name, parent);
        panel.AddComponent<Image>().color = new Color(0.97f, 0.97f, 0.97f, 0.98f);
        return panel;
    }

    private static Text Label(string name, Transform parent, string value, Font font, int size,
        Vector2 anchor, Vector2 dimensions, Vector2 offset = default)
    {
        var node = Node(name, parent);
        Place((RectTransform)node.transform, anchor, dimensions, offset);
        var label = node.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.text = value;
        label.color = new Color(0.12f, 0.14f, 0.18f);
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static Button Button(string name, Transform parent, string value, Font font, Vector2 anchor,
        Vector2 offset = default, Vector2 dimensions = default)
    {
        if (dimensions == default)
            dimensions = new Vector2(280, 56);
        var node = Node(name, parent);
        Place((RectTransform)node.transform, anchor, dimensions, offset);
        node.AddComponent<Image>().color = new Color(0.78f, 0.84f, 0.92f);
        var button = node.AddComponent<Button>();
        button.targetGraphic = node.GetComponent<Image>();
        var label = Label("Label", node.transform, value, font, 19, new Vector2(0.5f, 0.5f), dimensions);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = 19;
        return button;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = offset;
    }
}
