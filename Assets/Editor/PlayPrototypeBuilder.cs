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
        AddSettingsAndFeedback(root, font);
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

    [MenuItem("Tools/Squapple/Add Settings and Feedback")]
    public static void Upgrade()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            if (root.transform.Find("SafeArea/Settings") != null)
                return;
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/IBMPlexSansKR-Regular.ttf");
            AddSettingsAndFeedback(root, font);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void AddSettingsAndFeedback(GameObject root, Font font)
    {
        var safe = root.transform.Find("SafeArea");
        var home = safe.Find("Home");
        var pause = safe.Find("PauseScreen");
        home.Find("Rules").GetComponent<Text>().text =
            "합이 10이 되도록 사각형으로 묶으세요.\n예: 3 + 7 = 10 → 사과 2개, 2점\n사과 한 개당 1점 · 제한 시간 120초";
        pause.Find("Quit/Label").GetComponent<Text>().text = "홈으로";
        var homeSettings = Button("Settings", home, "설정", font, new Vector2(0.5f, 0.26f));
        var pauseSettings = Button("Settings", pause, "설정", font, new Vector2(0.5f, 0.22f));
        var restart = Button("Restart", pause, "새 판", font, new Vector2(0.5f, 0.33f));
        var quit = (RectTransform)pause.Find("Quit");
        quit.anchorMin = quit.anchorMax = new Vector2(0.5f, 0.11f);

        var settings = Modal("Settings", safe);
        Label("Title", settings.transform, "설정", font, 28, new Vector2(0.5f, 0.78f), new Vector2(320, 60));
        var sound = Toggle("Sound", settings.transform, "소리", font, new Vector2(0.5f, 0.62f));
        var vibration = Toggle("Vibration", settings.transform, "진동", font, new Vector2(0.5f, 0.51f));
        Label("Hint", settings.transform, "진동은 제거 성공 시 사용해요.\n기기에 따라 진동 느낌이 달라요.", font, 15,
            new Vector2(0.5f, 0.415f), new Vector2(330, 64));
        var test = Button("TestFeedback", settings.transform, "효과 확인", font, new Vector2(0.5f, 0.30f));
        var close = Button("Close", settings.transform, "돌아가기", font, new Vector2(0.5f, 0.18f));
        settings.SetActive(false);

        var confirmation = Modal("QuitConfirmation", safe);
        var message = Label("Message", confirmation.transform, "이번 판을 그만할까요?\n홈으로 가면 이번 점수는 남지 않아요.", font, 19,
            new Vector2(0.5f, 0.62f), new Vector2(350, 130));
        var cancel = Button("Cancel", confirmation.transform, "취소", font, new Vector2(0.5f, 0.43f));
        var leave = Button("Leave", confirmation.transform, "그만하고 홈으로", font, new Vector2(0.5f, 0.31f));
        confirmation.SetActive(false);

        var source = root.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.volume = 0.45f;
        var feedback = root.AddComponent<PlayerFeedback>();
        var audio = new SerializedObject(feedback);
        Assign(audio, "successSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SelectionSuccess.wav"));
        Assign(audio, "errorSound", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SelectionError.wav"));
        audio.ApplyModifiedPropertiesWithoutUndo();

        var serialized = new SerializedObject(root.GetComponent<PlayPrototype>());
        Assign(serialized, "settings", settings);
        Assign(serialized, "quitConfirmation", confirmation);
        Assign(serialized, "feedbackPlayer", feedback);
        Assign(serialized, "homeSettingsButton", homeSettings);
        Assign(serialized, "pauseSettingsButton", pauseSettings);
        Assign(serialized, "closeSettingsButton", close);
        Assign(serialized, "testFeedbackButton", test);
        Assign(serialized, "confirmQuitButton", leave);
        Assign(serialized, "cancelQuitButton", cancel);
        Assign(serialized, "soundToggle", sound);
        Assign(serialized, "vibrationToggle", vibration);
        Assign(serialized, "restartButton", restart);
        Assign(serialized, "quitConfirmationText", message);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Toggle Toggle(string name, Transform parent, string value, Font font, Vector2 anchor)
    {
        var node = Node(name, parent);
        Place((RectTransform)node.transform, anchor, new Vector2(280, 52), Vector2.zero);
        node.AddComponent<Image>().color = Color.clear;
        var box = Node("Box", node.transform);
        Place((RectTransform)box.transform, new Vector2(0.5f, 0.5f), new Vector2(30, 30), new Vector2(-120, 0));
        var background = box.AddComponent<Image>();
        background.color = new Color(0.78f, 0.84f, 0.92f);
        var check = Node("Check", box.transform);
        Place((RectTransform)check.transform, new Vector2(0.5f, 0.5f), new Vector2(18, 18), Vector2.zero);
        var mark = check.AddComponent<Image>();
        mark.color = new Color(0.15f, 0.35f, 0.75f);
        var toggle = node.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = mark;
        Label("Label", node.transform, value, font, 20, new Vector2(0.5f, 0.5f), new Vector2(230, 52), new Vector2(18, 0));
        return toggle;
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
        panel.AddComponent<Image>().color = new Color(0.97f, 0.97f, 0.97f);
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
