#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Squapple.Core;
using Squapple.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Squapple.Presentation.Tests
{
    public sealed class PlayRecordTests
    {
        private GameObject _root;
        private GameObject _events;
        private PlayPrototype _ui;
        private bool _hadProgress;
        private string _progressJson;
        private Random.State _randomState;

        private T Field<T>(string name) => (T)typeof(PlayPrototype)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_ui);
        private GameSession Session => Field<GameSession>("_session");
        private void Begin(GameRules rules, int[] cells) => typeof(PlayPrototype)
            .GetMethod("BeginRound", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_ui, new object[] { rules, cells, false });

        private void CreateScreen()
        {
            _root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayPrototype.prefab"));
            _ui = _root.GetComponent<PlayPrototype>();
            _root.SendMessage("OnApplicationFocus", true);
            _root.SendMessage("OnApplicationPause", false);
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _hadProgress = PlayerPrefs.HasKey(LocalProgress.StorageKey);
            _progressJson = PlayerPrefs.GetString(LocalProgress.StorageKey);
            PlayerPrefs.DeleteKey(LocalProgress.StorageKey);
            _randomState = Random.state;
            _events = new GameObject("Record test events", typeof(EventSystem));
            CreateScreen();
            yield return null;
            _root.SendMessage("OnApplicationFocus", true);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_events);
            if (_hadProgress)
                PlayerPrefs.SetString(LocalProgress.StorageKey, _progressJson);
            else
                PlayerPrefs.DeleteKey(LocalProgress.StorageKey);
            PlayerPrefs.Save();
            Random.state = _randomState;
            yield return null;
        }

        [Test]
        public void FirstLaunchDisablesReplayAndAnAbandonedRoundIsNotSaved()
        {
            Assert.That(Field<Button>("lastRoundButton").interactable, Is.False);
            Assert.That(Field<Text>("homeBest").text, Does.Contain("최고 기록 0점"));
            _ui.RetryRound();
            Assert.That(Session, Is.Null);
            _ui.StartRound();
            _ui.ShowHome();
            Assert.That(PlayerPrefs.HasKey(LocalProgress.StorageKey), Is.False);
            Assert.That(Field<Button>("lastRoundButton").interactable, Is.False);
        }

        [Test]
        public void ResultIsSavedBeforeItsActionsAndPracticeRestoresOriginalBoard()
        {
            var rules = new GameRules(2, 1, 75);
            Begin(rules, new[] { 3, 7 });
            _root.SendMessage("Submit", new CellRectangle(0, 0, 1, 0));
            Assert.That(Field<GameObject>("result").activeSelf, Is.True);
            Assert.That(new LocalProgress().GetBest(rules), Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { 3, 7 }, new LocalProgress().LastRound.initialCells);
            Assert.That(Field<Text>("resultText").text, Does.Contain("최고 기록 2점"));
            Field<Button>("retryButton").onClick.Invoke();
            CollectionAssert.AreEqual(new[] { 3, 7 }, Session.State.CurrentValue.Cells);
            Assert.That(Session.State.CurrentValue.Rules.DurationSeconds, Is.EqualTo(75));
            Assert.That(Session.State.CurrentValue.Score, Is.Zero);
            Assert.That(Field<Text>("selectionSum").text, Is.EqualTo("연습 · 선택 합: —"));
        }

        [UnityTest]
        public IEnumerator ABetterPracticeResultCannotImproveGeneralBest()
        {
            var rules = new GameRules(4, 1, 0.3);
            Begin(rules, new[] { 3, 7, 4, 6 });
            _root.SendMessage("Submit", new CellRectangle(0, 0, 1, 0));
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Finished));
            Assert.That(new LocalProgress().GetBest(rules), Is.EqualTo(2));
            _ui.RetryRound();
            _root.SendMessage("Submit", new CellRectangle(0, 0, 1, 0));
            _root.SendMessage("Submit", new CellRectangle(2, 0, 3, 0));
            Assert.That(Session.State.CurrentValue.Score, Is.EqualTo(4));
            Assert.That(new LocalProgress().GetBest(rules), Is.EqualTo(2));
            Assert.That(new LocalProgress().LastRound.practice, Is.True);
            Assert.That(Field<Text>("resultText").text, Does.Contain("연습 점수 4점"));
            Assert.That(Field<Text>("resultText").text, Does.Contain("일반 최고 기록 2점"));
        }

        [UnityTest]
        public IEnumerator RecreatedScreenCanReplayLastFinishedBoardAndThenStartDefaultRules()
        {
            var rules = new GameRules(2, 1, 60);
            Begin(rules, new[] { 4, 6 });
            _root.SendMessage("Submit", new CellRectangle(0, 0, 1, 0));
            Object.Destroy(_root);
            yield return null;
            CreateScreen();
            yield return null;
            _root.SendMessage("OnApplicationFocus", true);
            Assert.That(Session, Is.Null);
            Assert.That(Field<Button>("lastRoundButton").interactable, Is.True);
            Field<Button>("lastRoundButton").onClick.Invoke();
            CollectionAssert.AreEqual(new[] { 4, 6 }, Session.State.CurrentValue.Cells);
            Assert.That(Session.State.CurrentValue.Rules.Width, Is.EqualTo(2));
            Assert.That(Session.State.CurrentValue.Rules.DurationSeconds, Is.EqualTo(60));
            _ui.StartRound();
            yield return null;
            Assert.That(Session.State.CurrentValue.Rules.Width, Is.EqualTo(8));
            Assert.That(Session.State.CurrentValue.Rules.Height, Is.EqualTo(13));
            Assert.That(Session.State.CurrentValue.Rules.DurationSeconds, Is.EqualTo(120));
            Assert.That(Field<GameBoardView>("board").transform.childCount, Is.EqualTo(105));
            Assert.That(Field<Text>("selectionSum").text, Is.EqualTo("선택 합: —"));
        }

        [Test]
        public void RepeatedRenderingOfFinishedStateDoesNotWriteASecondResult()
        {
            Begin(new GameRules(2, 1), new[] { 3, 7 });
            _root.SendMessage("Submit", new CellRectangle(0, 0, 1, 0));
            const string sentinel = "already-saved-result";
            PlayerPrefs.SetString(LocalProgress.StorageKey, sentinel);
            _root.SendMessage("Render", Session.State.CurrentValue);
            Assert.That(PlayerPrefs.GetString(LocalProgress.StorageKey), Is.EqualTo(sentinel));
        }

        [Test]
        public void ApplicationSuspensionRejectsReplayWithoutReplacingCurrentRound()
        {
            Begin(new GameRules(2, 1), new[] { 3, 7 });
            _root.SendMessage("Submit", new CellRectangle(0, 0, 1, 0));
            _ui.StartRound();
            var session = Session;
            _root.SendMessage("OnApplicationPause", true);
            _ui.RetryRound();
            Assert.That(Session, Is.SameAs(session));
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Paused));
        }
    }
}
#endif
