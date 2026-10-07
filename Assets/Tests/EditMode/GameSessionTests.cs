using System;
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using Squapple.Core;
using Squapple.Gameplay;

namespace Squapple.Core.Tests
{
    public class GameSessionTests
    {
        private double _now;

        [SetUp]
        public void SetUp() => _now = 100;

        private GameSession CreateSession(int[] cells = null, double duration = 120)
        {
            cells ??= new[] { 5, 5, 4, 3, 7, 6 };
            return new GameSession(new GameRules(3, cells.Length / 3, duration), cells, () => _now);
        }

        [Test]
        public void NewSessionPublishesReadyStateAndAcceptsStartOnlyOnce()
        {
            using var session = CreateSession();
            var phases = new List<GamePhase>();
            using var observer = session.State.Subscribe(state => phases.Add(state.Phase));
            Assert.That(session.State.CurrentValue.Score, Is.Zero);
            Assert.That(session.State.CurrentValue.RemainingSeconds, Is.EqualTo(120));
            Assert.That(session.Pause(), Is.False);
            Assert.That(session.Resume(), Is.False);
            Assert.That(session.Finish(), Is.False);
            Assert.That(session.Start(), Is.True);
            Assert.That(session.Start(), Is.False);
            Assert.That(phases, Is.EqualTo(new[] { GamePhase.Ready, GamePhase.Playing }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StartingAnUnplayableBoardFinishesOnce(bool empty)
        {
            using var session = CreateSession(empty ? new[] { 0, 0, 0 } : new[] { 9, 9, 9 });
            var ends = new List<GameSessionState>();
            using var observer = session.Finished.Subscribe(ends.Add);
            Assert.That(session.Start(), Is.True);
            Assert.That(session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Finished));
            Assert.That(session.State.CurrentValue.EndReason, Is.EqualTo(empty ? GameEndReason.Cleared : GameEndReason.NoMoves));
            Assert.That(session.Tick(), Is.False);
            Assert.That(session.Finish(), Is.False);
            Assert.That(ends.Count, Is.EqualTo(1));
        }

        [Test]
        public void SelectionAndPreviewAreAllowedOnlyWhilePlaying()
        {
            using var session = CreateSession();
            var rectangle = new CellRectangle(0, 0, 1, 0);
            Assert.That(session.TrySelect(rectangle), Is.Zero);
            Assert.That(session.InspectSelection(rectangle).CanRemove, Is.False);
            session.Start();
            Assert.That(session.InspectSelection(rectangle).CanRemove, Is.True);
            session.Pause();
            Assert.That(session.TrySelect(rectangle), Is.Zero);
            Assert.That(session.InspectSelection(rectangle).CanRemove, Is.False);
            session.Resume();
            Assert.That(session.TrySelect(rectangle), Is.EqualTo(2));
            session.Finish();
            Assert.That(session.TrySelect(new CellRectangle(0, 1, 1, 1)), Is.Zero);
            Assert.That(session.State.CurrentValue.Score, Is.EqualTo(2));
        }

        [Test]
        public void ElapsedTimeUsesClockAndExcludesEveryPause()
        {
            using var session = CreateSession();
            session.Start();
            _now = 120;
            session.Pause();
            _now = 1000;
            Assert.That(session.Tick(), Is.False);
            Assert.That(session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(20));
            session.Resume();
            _now = 1030;
            session.Pause();
            Assert.That(session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(50));
            _now = 2000;
            session.Resume();
            _now = 2010;
            session.Tick();
            Assert.That(session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(60));
            Assert.That(session.State.CurrentValue.RemainingSeconds, Is.EqualTo(60));
        }

        [Test]
        public void SkippingTicksStillUsesActualElapsedTimeForSelection()
        {
            using var session = CreateSession();
            session.Start();
            _now = 117;
            Assert.That(session.TrySelect(new CellRectangle(0, 0, 1, 0)), Is.EqualTo(2));
            Assert.That(session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(17));
        }

        [TestCase(219.999, true)]
        [TestCase(220, false)]
        [TestCase(300, false)]
        public void DeadlineIsCheckedBeforeSelectionWithoutNeedingATick(double time, bool accepted)
        {
            using var session = CreateSession();
            var selections = new List<SelectionResolved>();
            var ends = new List<GameSessionState>();
            using var selectionObserver = session.Selections.Subscribe(selections.Add);
            using var endObserver = session.Finished.Subscribe(ends.Add);
            session.Start();
            _now = time;
            Assert.That(session.TrySelect(new CellRectangle(0, 0, 1, 0)), Is.EqualTo(accepted ? 2 : 0));
            Assert.That(session.State.CurrentValue.Score, Is.EqualTo(accepted ? 2 : 0));
            Assert.That(selections.Count, Is.EqualTo(accepted ? 1 : 0));
            Assert.That(ends.Count, Is.EqualTo(accepted ? 0 : 1));
            if (!accepted)
            {
                Assert.That(session.State.CurrentValue.EndReason, Is.EqualTo(GameEndReason.TimeExpired));
                Assert.That(session.State.CurrentValue.RemainingSeconds, Is.Zero);
                Assert.That(session.State.CurrentValue.Cells, Is.EqualTo(new[] { 5, 5, 4, 3, 7, 6 }));
            }
        }

        [Test]
        public void DeadlineWinsEvenWhenPauseIsTheFirstRequestAfterExpiry()
        {
            using var session = CreateSession();
            var endCount = 0;
            using var observer = session.Finished.Subscribe(_ => endCount++);
            session.Start();
            _now = 220;
            Assert.That(session.Pause(), Is.False);
            Assert.That(session.Resume(), Is.False);
            Assert.That(session.Tick(), Is.False);
            Assert.That(session.State.CurrentValue.EndReason, Is.EqualTo(GameEndReason.TimeExpired));
            Assert.That(endCount, Is.EqualTo(1));
        }

        [Test]
        public void TickExpiresOnceAndNeverAcceptsFurtherInput()
        {
            using var session = CreateSession();
            var endCount = 0;
            using var observer = session.Finished.Subscribe(_ => endCount++);
            session.Start();
            _now = 250;
            Assert.That(session.Tick(), Is.True);
            Assert.That(session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(120));
            Assert.That(session.State.CurrentValue.RemainingSeconds, Is.Zero);
            Assert.That(session.State.CurrentValue.EndReason, Is.EqualTo(GameEndReason.TimeExpired));
            Assert.That(session.Tick(), Is.False);
            Assert.That(session.TrySelect(new CellRectangle(0, 0, 1, 0)), Is.Zero);
            Assert.That(endCount, Is.EqualTo(1));
        }

        [Test]
        public void ManualFinishAfterExpiryKeepsTimeExpiredReason()
        {
            using var session = CreateSession();
            session.Start();
            _now = 220;
            Assert.That(session.Finish(), Is.True);
            Assert.That(session.State.CurrentValue.EndReason, Is.EqualTo(GameEndReason.TimeExpired));
            Assert.That(session.State.CurrentValue.Score, Is.Zero);
        }

        [Test]
        public void FailedSelectionReportsFailureWithoutChangingBoardOrScore()
        {
            using var session = CreateSession();
            SelectionResolved? selection = null;
            using var observer = session.Selections.Subscribe(value => selection = value);
            session.Start();
            var cells = session.State.CurrentValue.Cells;
            Assert.That(session.TrySelect(new CellRectangle(0, 0, 0, 0)), Is.Zero);
            Assert.That(selection.HasValue, Is.True);
            Assert.That(selection.Value.Summary.Sum, Is.EqualTo(5));
            Assert.That(selection.Value.Summary.CanRemove, Is.False);
            Assert.That(session.State.CurrentValue.Cells, Is.SameAs(cells));
            Assert.That(session.State.CurrentValue.Score, Is.Zero);
        }

        [Test]
        public void RemovingLastMovePublishesFinalBoardScoreAndReasonTogether()
        {
            using var session = CreateSession(new[] { 5, 5, 4 });
            var states = new List<GameSessionState>();
            using var observer = session.State.Subscribe(states.Add);
            session.Start();
            Assert.That(session.TrySelect(new CellRectangle(0, 0, 1, 0)), Is.EqualTo(2));
            Assert.That(states.Count, Is.EqualTo(3));
            var state = states[2];
            Assert.That(state.Score, Is.EqualTo(2));
            Assert.That(state.Cells, Is.EqualTo(new[] { 0, 0, 4 }));
            Assert.That(state.RemainingApples, Is.EqualTo(1));
            Assert.That(state.Phase, Is.EqualTo(GamePhase.Finished));
            Assert.That(state.EndReason, Is.EqualTo(GameEndReason.NoMoves));
        }

        [Test]
        public void ClearingBoardNotifiesStateThenSelectionThenFinished()
        {
            using var session = CreateSession(new[] { 5, 0, 5 });
            var order = new List<string>();
            using var stateObserver = session.State.Subscribe(state =>
            {
                if (state.Score > 0)
                    order.Add($"state:{state.Score}:{state.Phase}");
            });
            using var selectionObserver = session.Selections.Subscribe(value =>
            {
                Assert.That(session.State.CurrentValue.Score, Is.EqualTo(2));
                order.Add($"selection:{value.Summary.Points}");
            });
            using var endObserver = session.Finished.Subscribe(state =>
            {
                Assert.That(state.EndReason, Is.EqualTo(GameEndReason.Cleared));
                order.Add($"finished:{state.Score}");
            });
            session.Start();
            _now = 103;
            Assert.That(session.TrySelect(new CellRectangle(2, 0, 0, 0)), Is.EqualTo(2));
            Assert.That(order, Is.EqualTo(new[] { "state:2:Finished", "selection:2", "finished:2" }));
            Assert.That(session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(3));
        }

        [Test]
        public void ResubscribingReplaysCurrentStateButNotPastEvents()
        {
            using var session = CreateSession(new[] { 5, 0, 5 });
            session.Start();
            session.TrySelect(new CellRectangle(0, 0, 2, 0));
            var states = new List<GameSessionState>();
            var selectionCount = 0;
            var endCount = 0;
            using var stateObserver = session.State.Subscribe(states.Add);
            using var selectionObserver = session.Selections.Subscribe(_ => selectionCount++);
            using var endObserver = session.Finished.Subscribe(_ => endCount++);
            Assert.That(states.Count, Is.EqualTo(1));
            Assert.That(states[0].Score, Is.EqualTo(2));
            Assert.That(states[0].Phase, Is.EqualTo(GamePhase.Finished));
            Assert.That(selectionCount, Is.Zero);
            Assert.That(endCount, Is.Zero);
        }

        [Test]
        public void StateCellsAreReadOnlyAndOldSnapshotsRemainUnchanged()
        {
            var input = new[] { 5, 5, 4, 3, 7, 6 };
            using var session = CreateSession(input);
            var initial = session.State.CurrentValue;
            input[0] = 9;
            Assert.That(() => ((IList<int>)initial.Cells)[0] = 9, Throws.TypeOf<NotSupportedException>());
            session.Start();
            session.TrySelect(new CellRectangle(0, 0, 1, 0));
            Assert.That(initial.Cells[0], Is.EqualTo(5));
            Assert.That(initial.Score, Is.Zero);
            Assert.That(session.State.CurrentValue.Cells[0], Is.Zero);
        }

        [Test]
        public void TimeAndPreviewUpdatesReuseBoardSnapshot()
        {
            using var session = CreateSession();
            var initialCells = session.State.CurrentValue.Cells;
            session.Start();
            _now = 115;
            session.Tick();
            session.InspectSelection(new CellRectangle(0, 0, 1, 0));
            Assert.That(session.State.CurrentValue.Cells, Is.SameAs(initialCells));
            Assert.That(session.State.CurrentValue.RemainingSeconds, Is.EqualTo(105));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RequestsFromStateObserversCannotChangeResultsBasedOnSubscriptionOrder(bool reverseOrder)
        {
            using var session = CreateSession();
            var observedScores = new List<int>();
            Action<GameSessionState> requester = state =>
            {
                if (state.Score != 2)
                    return;
                Assert.That(session.TrySelect(new CellRectangle(0, 1, 1, 1)), Is.Zero);
                Assert.That(session.Pause(), Is.False);
                Assert.That(session.Finish(), Is.False);
                Assert.That(session.Tick(), Is.False);
            };
            Action<GameSessionState> observer = state => observedScores.Add(state.Score);
            using var first = session.State.Subscribe(reverseOrder ? observer : requester);
            using var second = session.State.Subscribe(reverseOrder ? requester : observer);
            session.Start();
            Assert.That(session.TrySelect(new CellRectangle(0, 0, 1, 0)), Is.EqualTo(2));
            Assert.That(session.State.CurrentValue.Score, Is.EqualTo(2));
            Assert.That(session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
            Assert.That(observedScores, Is.EqualTo(new[] { 0, 0, 2 }));
            Assert.That(session.TrySelect(new CellRectangle(0, 1, 1, 1)), Is.EqualTo(2));
            Assert.That(session.State.CurrentValue.Score, Is.EqualTo(4));
        }

        [Test]
        public void RequestsFromSelectionObserversAreAlsoRejected()
        {
            using var session = CreateSession();
            var selectionCount = 0;
            using var observer = session.Selections.Subscribe(_ =>
            {
                selectionCount++;
                Assert.That(session.TrySelect(new CellRectangle(0, 1, 1, 1)), Is.Zero);
            });
            session.Start();
            session.TrySelect(new CellRectangle(0, 0, 1, 0));
            Assert.That(selectionCount, Is.EqualTo(1));
            Assert.That(session.State.CurrentValue.Score, Is.EqualTo(2));
        }

        [Test]
        public void ManualFinishFromPausePreservesElapsedTimeAndNotifiesOnce()
        {
            using var session = CreateSession();
            var endCount = 0;
            using var observer = session.Finished.Subscribe(_ => endCount++);
            session.Start();
            _now = 110;
            session.Pause();
            _now = 1000;
            Assert.That(session.Finish(), Is.True);
            Assert.That(session.Finish(), Is.False);
            Assert.That(session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(10));
            Assert.That(session.State.CurrentValue.EndReason, Is.EqualTo(GameEndReason.Stopped));
            Assert.That(endCount, Is.EqualTo(1));
        }

        [Test]
        public void DisposalRejectsFurtherRequestsAndKeepsNewSessionIndependent()
        {
            var oldSession = CreateSession();
            oldSession.Start();
            oldSession.Dispose();
            oldSession.Dispose();
            using var session = CreateSession();
            session.Start();
            _now = 150;
            Assert.That(oldSession.Tick(), Is.False);
            Assert.That(oldSession.TrySelect(new CellRectangle(0, 0, 1, 0)), Is.Zero);
            Assert.That(oldSession.Pause(), Is.False);
            Assert.That(oldSession.Resume(), Is.False);
            Assert.That(oldSession.Finish(), Is.False);
            session.Tick();
            Assert.That(session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(50));
            Assert.That(session.State.CurrentValue.Score, Is.Zero);
        }

        [Test]
        public void DisposalDuringStateNotificationDoesNotPublishToDisposedSubjects()
        {
            using var session = CreateSession(new[] { 5, 0, 5 });
            using var observer = session.State.Subscribe(state =>
            {
                if (state.Phase == GamePhase.Finished)
                    session.Dispose();
            });
            session.Start();
            Assert.That(() => session.TrySelect(new CellRectangle(0, 0, 2, 0)), Throws.Nothing);
        }
    }
}
