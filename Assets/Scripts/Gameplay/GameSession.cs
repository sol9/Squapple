using System;
using System.Collections.Generic;
using R3;
using Squapple.Core;

namespace Squapple.Gameplay
{
    // Call requests and Tick from one thread. The clock supplies monotonic seconds.
    public sealed class GameSession : IDisposable
    {
        private readonly GameBoard _board;
        private readonly Func<double> _clock;
        private readonly ReactiveProperty<GameSessionState> _state;
        private readonly Subject<SelectionResolved> _selections = new();
        private readonly Subject<GameSessionState> _finished = new();
        private IReadOnlyList<int> _visibleCells;
        private GamePhase _phase = GamePhase.Ready;
        private GameEndReason _endReason;
        private int _score;
        private double _elapsed;
        private double _elapsedAtResume;
        private double _resumeTime;
        private bool _publishing;
        private bool _disposed;

        public ReadOnlyReactiveProperty<GameSessionState> State => _state;
        public Observable<SelectionResolved> Selections => _selections;
        public Observable<GameSessionState> Finished => _finished;

        public GameSession(GameRules rules, IReadOnlyList<int> initialCells, Func<double> clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _board = new GameBoard(rules, initialCells);
            _visibleCells = Array.AsReadOnly(_board.CopyCells());
            _state = new ReactiveProperty<GameSessionState>(CreateState());
        }

        public bool Start()
        {
            if (!CanRequest || _phase != GamePhase.Ready)
                return false;

            _resumeTime = _clock();
            _phase = GamePhase.Playing;
            CheckBoardEnd();
            Publish();
            return true;
        }

        public bool Pause()
        {
            if (!CanRequest || _phase != GamePhase.Playing)
                return false;

            RefreshTime();
            if (_phase == GamePhase.Finished)
            {
                Publish();
                return false;
            }

            _elapsedAtResume = _elapsed;
            _phase = GamePhase.Paused;
            Publish();
            return true;
        }

        public bool Resume()
        {
            if (!CanRequest || _phase != GamePhase.Paused)
                return false;

            _resumeTime = _clock();
            _phase = GamePhase.Playing;
            Publish();
            return true;
        }

        public bool Tick()
        {
            if (!CanRequest || _phase != GamePhase.Playing)
                return false;

            RefreshTime();
            Publish();
            return true;
        }

        public SelectionSummary InspectSelection(CellRectangle rectangle)
        {
            return !_disposed && _phase == GamePhase.Playing ? _board.InspectSelection(rectangle) : default;
        }

        public int TrySelect(CellRectangle rectangle)
        {
            if (!CanRequest || _phase != GamePhase.Playing)
                return 0;

            RefreshTime();
            if (_phase == GamePhase.Finished)
            {
                Publish();
                return 0;
            }

            var selection = _board.InspectSelection(rectangle);
            var points = _board.TryRemove(rectangle);
            if (points > 0)
            {
                _score += points;
                _visibleCells = Array.AsReadOnly(_board.CopyCells());
                CheckBoardEnd();
            }

            Publish(new SelectionResolved(rectangle, selection, _elapsed));
            return points;
        }

        public bool Finish()
        {
            if (!CanRequest || (_phase != GamePhase.Playing && _phase != GamePhase.Paused))
                return false;

            if (_phase == GamePhase.Playing)
                RefreshTime();
            if (_phase != GamePhase.Finished)
                End(GameEndReason.Stopped);
            Publish();
            return true;
        }

        private bool CanRequest => !_disposed && !_publishing;

        private void RefreshTime()
        {
            _elapsed = Math.Min(_board.Rules.DurationSeconds, _elapsedAtResume + _clock() - _resumeTime);
            if (_elapsed >= _board.Rules.DurationSeconds)
                End(GameEndReason.TimeExpired);
        }

        private void CheckBoardEnd()
        {
            if (_board.RemainingApples == 0)
                End(GameEndReason.Cleared);
            else if (!_board.HasMoves())
                End(GameEndReason.NoMoves);
        }

        private void End(GameEndReason reason)
        {
            _phase = GamePhase.Finished;
            _endReason = reason;
        }

        private GameSessionState CreateState()
        {
            return new GameSessionState(_board.Rules, _visibleCells, _phase,
                _score, _board.RemainingApples, _elapsed, _endReason);
        }

        private void Publish(SelectionResolved? selection = null)
        {
            var justFinished = _phase == GamePhase.Finished && _state.CurrentValue.Phase != GamePhase.Finished;
            var state = CreateState();
            _publishing = true;
            try
            {
                // All fields are final before observers run; requests from observers are rejected.
                _state.Value = state;
                if (!_disposed && selection.HasValue)
                    _selections.OnNext(selection.Value);
                if (!_disposed && justFinished)
                    _finished.OnNext(state);
            }
            finally
            {
                _publishing = false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _state.Dispose();
            _selections.Dispose();
            _finished.Dispose();
        }
    }
}
