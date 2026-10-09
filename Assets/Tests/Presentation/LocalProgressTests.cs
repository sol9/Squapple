using NUnit.Framework;
using Squapple.Core;
using Squapple.Gameplay;
using UnityEngine;

namespace Squapple.Presentation.Tests
{
    public sealed class LocalProgressTests
    {
        private bool _hadSave;
        private string _savedJson;

        [SetUp]
        public void SetUp()
        {
            _hadSave = PlayerPrefs.HasKey(LocalProgress.StorageKey);
            _savedJson = PlayerPrefs.GetString(LocalProgress.StorageKey);
            PlayerPrefs.DeleteKey(LocalProgress.StorageKey);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadSave)
                PlayerPrefs.SetString(LocalProgress.StorageKey, _savedJson);
            else
                PlayerPrefs.DeleteKey(LocalProgress.StorageKey);
            PlayerPrefs.Save();
        }

        private static void Complete(LocalProgress progress, GameRules rules, int[] cells, bool practice = false)
        {
            using var session = new GameSession(rules, cells, () => 0);
            session.Start();
            session.TrySelect(new CellRectangle(0, 0, rules.Width - 1, rules.Height - 1));
            Assert.That(session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Finished));
            Assert.That(progress.RecordFinished(session.State.CurrentValue, cells, practice), Is.True);
        }

        [Test]
        public void FirstLaunchHasNoBestOrReplay()
        {
            var progress = new LocalProgress();
            Assert.That(progress.GetBest(new GameRules()), Is.Zero);
            Assert.That(progress.LastRound, Is.Null);
            Assert.That(progress.CanSave, Is.True);
        }

        [Test]
        public void FinishedRoundAndBestSurviveReloadWithOriginalBoardCopied()
        {
            var rules = new GameRules(2, 1, 123.125);
            var original = new[] { 3, 7 };
            var progress = new LocalProgress();
            Complete(progress, rules, original);
            original[0] = 9;
            var restored = new LocalProgress();
            Assert.That(restored.GetBest(rules), Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { 3, 7 }, restored.LastRound.initialCells);
            Assert.That(restored.LastRound.record.durationSeconds, Is.EqualTo(123.125));
            Assert.That(restored.LastRound.endReason, Is.EqualTo(GameEndReason.Cleared));
            Assert.That(restored.LastRound.practice, Is.False);
            Assert.That(JsonUtility.FromJson<VersionProbe>(PlayerPrefs.GetString(LocalProgress.StorageKey)).version,
                Is.EqualTo(LocalProgress.FormatVersion));
        }

        [System.Serializable]
        private sealed class VersionProbe { public int version; }

        [Test]
        public void LowerScoreCannotReplaceBestButBecomesLastFinishedBoard()
        {
            var rules = new GameRules(4, 1);
            var progress = new LocalProgress();
            Complete(progress, rules, new[] { 1, 2, 3, 4 });
            var elapsed = 0.0;
            var cells = new[] { 3, 7, 4, 6 };
            using var session = new GameSession(rules, cells, () => elapsed);
            session.Start();
            session.TrySelect(new CellRectangle(0, 0, 1, 0));
            elapsed = 120;
            session.Tick();
            progress.RecordFinished(session.State.CurrentValue, cells, false);
            var restored = new LocalProgress();
            Assert.That(restored.GetBest(rules), Is.EqualTo(4));
            Assert.That(restored.LastRound.record.score, Is.EqualTo(2));
            CollectionAssert.AreEqual(cells, restored.LastRound.initialCells);
        }

        [Test]
        public void HigherPracticeScoreDoesNotReplaceGeneralBest()
        {
            var rules = new GameRules(4, 1);
            var progress = new LocalProgress();
            var elapsed = 0.0;
            var cells = new[] { 3, 7, 4, 6 };
            using var session = new GameSession(rules, cells, () => elapsed);
            session.Start();
            session.TrySelect(new CellRectangle(0, 0, 1, 0));
            elapsed = 120;
            session.Tick();
            progress.RecordFinished(session.State.CurrentValue, cells, false);
            Complete(progress, rules, new[] { 1, 2, 3, 4 }, true);
            var restored = new LocalProgress();
            Assert.That(restored.GetBest(rules), Is.EqualTo(2));
            Assert.That(restored.LastRound.record.score, Is.EqualTo(4));
            Assert.That(restored.LastRound.practice, Is.True);
        }

        [Test]
        public void DimensionsDurationAndRuleVersionDoNotMixRecords()
        {
            var progress = new LocalProgress();
            Complete(progress, new GameRules(2, 1, 120), new[] { 3, 7 });
            Complete(progress, new GameRules(1, 2, 120), new[] { 4, 6 });
            Complete(progress, new GameRules(2, 1, 60), new[] { 2, 8 });
            var restored = new LocalProgress();
            Assert.That(restored.GetBest(new GameRules(2, 1, 120)), Is.EqualTo(2));
            Assert.That(restored.GetBest(new GameRules(1, 2, 120)), Is.EqualTo(2));
            Assert.That(restored.GetBest(new GameRules(2, 1, 60)), Is.EqualTo(2));
            Assert.That(restored.GetBest(new GameRules(2, 1, 90)), Is.Zero);
            Assert.That(restored.GetBest(new GameRules(3, 1, 120)), Is.Zero);
            var json = PlayerPrefs.GetString(LocalProgress.StorageKey).Replace(
                $"\"rulesVersion\":{GameRules.Version}", $"\"rulesVersion\":{GameRules.Version + 1}");
            PlayerPrefs.SetString(LocalProgress.StorageKey, json);
            restored = new LocalProgress();
            Assert.That(restored.GetBest(new GameRules(2, 1)), Is.Zero);
            Assert.That(restored.LastRound, Is.Null);
        }

        [Test]
        public void OngoingAndAbandonedRoundsNeverWriteProgress()
        {
            var progress = new LocalProgress();
            var cells = new[] { 3, 7 };
            using var session = new GameSession(new GameRules(2, 1), cells, () => 0);
            session.Start();
            Assert.That(progress.RecordFinished(session.State.CurrentValue, cells, false), Is.False);
            session.Finish();
            Assert.That(progress.RecordFinished(session.State.CurrentValue, cells, false), Is.False);
            Assert.That(PlayerPrefs.HasKey(LocalProgress.StorageKey), Is.False);
        }

        [TestCase("not-json")]
        [TestCase("{}")]
        [TestCase("{\"version\":1,\"bestScores\":null,\"lastRound\":null}")]
        [TestCase("{\"version\":1,\"bestScores\":[null,{\"width\":-1}],\"lastRound\":{}}")]
        public void InvalidDataDoesNotPreventNewGameplayOrSaving(string json)
        {
            PlayerPrefs.SetString(LocalProgress.StorageKey, json);
            var progress = new LocalProgress();
            Assert.That(progress.GetBest(new GameRules()), Is.Zero);
            Assert.That(progress.LastRound, Is.Null);
            Complete(progress, new GameRules(2, 1), new[] { 3, 7 });
            Assert.That(new LocalProgress().GetBest(new GameRules(2, 1)), Is.EqualTo(2));
        }

        [Test]
        public void FutureSaveFormatIsPreservedWithoutBeingOverwritten()
        {
            const string future = "{\"version\":2,\"futureField\":\"keep\"}";
            PlayerPrefs.SetString(LocalProgress.StorageKey, future);
            var progress = new LocalProgress();
            using var session = new GameSession(new GameRules(2, 1), new[] { 3, 7 }, () => 0);
            session.Start();
            session.TrySelect(new CellRectangle(0, 0, 1, 0));
            Assert.That(progress.CanSave, Is.False);
            Assert.That(progress.RecordFinished(session.State.CurrentValue, new[] { 3, 7 }, false), Is.False);
            Assert.That(PlayerPrefs.GetString(LocalProgress.StorageKey), Is.EqualTo(future));
        }
    }
}
