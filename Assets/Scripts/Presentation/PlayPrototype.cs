using System;
using R3;
using Squapple.Core;
using Squapple.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Squapple.Presentation
{
    public sealed class PlayPrototype : MonoBehaviour
    {
        [Header("Development rules")]
        [SerializeField] private int columns = 8;
        [SerializeField] private int rows = 13;
        [SerializeField] private float durationSeconds = 120;
        [SerializeField] private Font font;
        [Header("Screens")]
        [SerializeField] private GameObject home;
        [SerializeField] private GameObject play;
        [SerializeField] private GameObject pause;
        [SerializeField] private GameObject result;
        [SerializeField] private GameBoardView board;
        [Header("Labels")]
        [SerializeField] private Text timer;
        [SerializeField] private Text score;
        [SerializeField] private Text selectionSum;
        [SerializeField] private Text feedback;
        [SerializeField] private Text resultText;
        [SerializeField] private Text pauseText;
        [Header("Actions")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button homeButton;

        private GameRules _rules;
        private GameSession _session;
        private IDisposable _stateSubscription;
        private double _feedbackUntil;

        private void Awake()
        {
            _rules = new GameRules(columns, rows, durationSeconds);
            board.Initialize(_rules, font);
            board.SelectionChanged += Preview;
            board.SelectionSubmitted += Submit;
            startButton.onClick.AddListener(StartRound);
            pauseButton.onClick.AddListener(PauseRound);
            resumeButton.onClick.AddListener(ResumeRound);
            quitButton.onClick.AddListener(ShowHome);
            newGameButton.onClick.AddListener(StartRound);
            homeButton.onClick.AddListener(ShowHome);
            ShowHome();
        }

        private void Update()
        {
            _session?.Tick();
            if (feedback.text.Length > 0 && Time.realtimeSinceStartupAsDouble >= _feedbackUntil)
                feedback.text = "";
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (pause.activeSelf)
                    ResumeRound();
                else if (_session?.State.CurrentValue.Phase == GamePhase.Playing)
                    PauseRound();
                else if (result.activeSelf)
                    ShowHome();
            }
        }

        public void StartRound()
        {
            ReleaseSession();
            home.SetActive(false);
            result.SetActive(false);
            pause.SetActive(false);
            play.SetActive(true);
            feedback.text = "";
            selectionSum.text = "선택 합: —";
            var cells = GameBoard.Generate(_rules, UnityEngine.Random.Range(0, int.MaxValue)).CopyCells();
            _session = new GameSession(_rules, cells, () => Time.realtimeSinceStartupAsDouble);
            _stateSubscription = _session.State.Subscribe(Render);
            _session.Start();
        }

        public void PauseRound()
        {
            if (_session == null || !_session.Pause())
                return;
            board.CancelSelection();
            pauseText.text = "일시정지\n계속하거나 홈으로 돌아갈 수 있어요.\n그만하면 이번 점수는 남지 않아요.";
            pause.SetActive(true);
            board.gameObject.SetActive(false);
        }

        public void ResumeRound()
        {
            if (_session == null || _session.State.CurrentValue.Phase != GamePhase.Paused)
                return;
            pause.SetActive(false);
            board.gameObject.SetActive(true);
            _session.Resume();
        }

        public void ShowHome()
        {
            ReleaseSession();
            home.SetActive(true);
            play.SetActive(false);
            pause.SetActive(false);
            result.SetActive(false);
            board.gameObject.SetActive(true);
        }

        private void Preview(CellRectangle? rectangle)
        {
            if (!rectangle.HasValue || _session == null)
            {
                selectionSum.text = "선택 합: —";
                return;
            }
            var summary = _session.InspectSelection(rectangle.Value);
            selectionSum.text = $"선택 합: {summary.Sum}";
            board.SetSelectionValid(summary.CanRemove);
        }

        private void Submit(CellRectangle rectangle)
        {
            if (_session == null)
                return;
            var points = _session.TrySelect(rectangle);
            if (_session.State.CurrentValue.Phase == GamePhase.Finished)
                return;
            feedback.text = points > 0 ? $"+{points}점" : "합이 10인 사각형을 골라주세요";
            _feedbackUntil = Time.realtimeSinceStartupAsDouble + 0.8;
        }

        private void Render(GameSessionState state)
        {
            var seconds = Mathf.CeilToInt((float)state.RemainingSeconds);
            timer.text = $"{seconds / 60}:{seconds % 60:00}";
            timer.color = seconds <= 10 ? new Color(0.7f, 0.25f, 0.08f) : Color.black;
            score.text = $"점수 {state.Score}";
            board.Show(state.Cells, state.Phase == GamePhase.Playing);
            if (state.Phase != GamePhase.Finished)
                return;
            pause.SetActive(false);
            result.SetActive(true);
            var reason = state.EndReason switch
            {
                GameEndReason.Cleared => "모든 사과를 지웠어요!",
                GameEndReason.NoMoves => "더 지울 수 있는 사각형이 없어요",
                _ => "시간 종료"
            };
            resultText.text = $"{reason}\n\n이번 점수 {state.Score}점";
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                PauseRound();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                PauseRound();
        }

        private void ReleaseSession()
        {
            board.CancelSelection();
            _stateSubscription?.Dispose();
            _stateSubscription = null;
            _session?.Dispose();
            _session = null;
        }

        private void OnDestroy()
        {
            ReleaseSession();
            board.SelectionChanged -= Preview;
            board.SelectionSubmitted -= Submit;
        }
    }
}
