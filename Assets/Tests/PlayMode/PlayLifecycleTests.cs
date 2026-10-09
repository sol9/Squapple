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
    public sealed class PlayLifecycleTests
    {
        private const string SoundKey = "Squapple.Settings.Sound";
        private GameObject _root;
        private GameObject _events;
        private PlayPrototype _ui;
        private Random.State _randomState;
        private bool _hadSoundSetting;
        private int _soundSetting;
        private bool _hadProgress;
        private string _progressJson;

        private GameSession Session => Field<GameSession>("_session");
        private T Field<T>(string name) => (T)typeof(PlayPrototype)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_ui);
        private void Pause(bool paused) => _root.SendMessage("OnApplicationPause", paused);
        private void Focus(bool focused) => _root.SendMessage("OnApplicationFocus", focused);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _randomState = Random.state;
            Random.InitState(42);
            _hadSoundSetting = PlayerPrefs.HasKey(SoundKey);
            _soundSetting = PlayerPrefs.GetInt(SoundKey);
            PlayerPrefs.SetInt(SoundKey, 1);
            _hadProgress = PlayerPrefs.HasKey(LocalProgress.StorageKey);
            _progressJson = PlayerPrefs.GetString(LocalProgress.StorageKey);
            PlayerPrefs.DeleteKey(LocalProgress.StorageKey);
            _events = new GameObject("Lifecycle test events", typeof(EventSystem));
            CreateScreen();
            yield return null;
            Focus(true);
            Pause(false);
        }

        private void CreateScreen()
        {
            _root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/PlayPrototype.prefab"));
            _ui = _root.GetComponent<PlayPrototype>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_events);
            if (_hadSoundSetting)
                PlayerPrefs.SetInt(SoundKey, _soundSetting);
            else
                PlayerPrefs.DeleteKey(SoundKey);
            if (_hadProgress)
                PlayerPrefs.SetString(LocalProgress.StorageKey, _progressJson);
            else
                PlayerPrefs.DeleteKey(LocalProgress.StorageKey);
            PlayerPrefs.Save();
            Random.state = _randomState;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SuspendCancelsDragAndFreezesTimeUntilManualResume()
        {
            _ui.StartRound();
            Canvas.ForceUpdateCanvases();
            var board = Field<GameBoardView>("board");
            var pointer = new PointerEventData(_events.GetComponent<EventSystem>())
            {
                pointerId = 1,
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, board.transform.position)
            };
            board.OnPointerDown(pointer);
            Assert.That(Field<Text>("selectionSum").text, Is.Not.EqualTo("선택 합: —"));
            Field<Text>("feedback").text = "+2점";
            Pause(true);
            var elapsed = Session.State.CurrentValue.ElapsedSeconds;
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Paused));
            Assert.That(Field<Text>("selectionSum").text, Is.EqualTo("선택 합: —"));
            Assert.That(Field<Text>("feedback").text, Is.Empty);
            Assert.That(Field<GameObject>("pause").activeSelf, Is.True);
            yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(Session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(elapsed));
            Pause(false);
            Focus(true);
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Paused));
            _ui.ResumeRound();
            board.OnPointerUp(pointer);
            Assert.That(Session.State.CurrentValue.Score, Is.Zero);
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
            Assert.That(board.gameObject.activeSelf, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void BothApplicationSignalsMustRecoverBeforeStartingOrResuming(bool focusFirst)
        {
            _ui.StartRound();
            var session = Session;
            Pause(true);
            Focus(false);
            if (focusFirst)
                Focus(true);
            else
                Pause(false);
            _ui.ResumeRound();
            _ui.StartRound();
            Assert.That(Session, Is.SameAs(session));
            Assert.That(session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Paused));
            if (focusFirst)
                Pause(false);
            else
                Focus(true);
            Assert.That(session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Paused));
            _ui.ResumeRound();
            Assert.That(session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
        }

        [Test]
        public void HomeSettingsSoundStopsAndCannotRestartWhileApplicationIsSuspended()
        {
            _ui.OpenSettings();
            var button = Field<Button>("testFeedbackButton");
            var source = Field<PlayerFeedback>("feedbackPlayer").GetComponent<AudioSource>();
            button.onClick.Invoke();
            Assert.That(source.isPlaying, Is.True);
            Focus(false);
            Assert.That(source.isPlaying, Is.False);
            button.onClick.Invoke();
            Assert.That(source.isPlaying, Is.False);
            Focus(true);
            button.onClick.Invoke();
            Assert.That(source.isPlaying, Is.True);
            Pause(true);
            Assert.That(source.isPlaying, Is.False);
            Assert.That(Session, Is.Null);
            Assert.That(Field<GameObject>("settings").activeSelf, Is.True);
        }

        [Test]
        public void SettingsAndConfirmationPreventResumeAfterApplicationReturns()
        {
            _ui.StartRound();
            _ui.OpenSettings();
            Focus(false);
            Pause(true);
            Pause(false);
            Focus(true);
            _ui.ResumeRound();
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Paused));
            Assert.That(Field<GameObject>("settings").activeSelf, Is.True);
            _ui.CloseSettings();
            Field<Button>("restartButton").onClick.Invoke();
            _ui.ResumeRound();
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Paused));
            Field<Button>("cancelQuitButton").onClick.Invoke();
            _ui.ResumeRound();
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
        }

        [UnityTest]
        public IEnumerator DisablingControllerPausesSessionAndRequiresManualResume()
        {
            _ui.StartRound();
            Field<Button>("testFeedbackButton").onClick.Invoke();
            _ui.enabled = false;
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Paused));
            Assert.That(Field<PlayerFeedback>("feedbackPlayer").GetComponent<AudioSource>().isPlaying, Is.False);
            var elapsed = Session.State.CurrentValue.ElapsedSeconds;
            yield return new WaitForSecondsRealtime(0.05f);
            _ui.enabled = true;
            yield return null;
            Assert.That(Session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(elapsed));
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Paused));
            _ui.ResumeRound();
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
        }

        [Test]
        public void ThirtyRestartsDisposeOldSessionsWithoutGrowingBoardOrChangingNewScore()
        {
            _ui.StartRound();
            var board = Field<GameBoardView>("board");
            var childCount = board.transform.childCount;
            for (var i = 0; i < 30; i++)
            {
                var old = Session;
                _ui.StartRound();
                Assert.That(old.Tick(), Is.False);
                Assert.That(old.TrySelect(new CellRectangle(0, 0, 1, 0)), Is.Zero);
                Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
                Assert.That(Session.State.CurrentValue.Score, Is.Zero);
                Assert.That(Field<Text>("score").text, Is.EqualTo("점수 0"));
                Assert.That(board.transform.childCount, Is.EqualTo(childCount));
            }
        }

        [UnityTest]
        public IEnumerator RepeatedScreenAndApplicationSuspensionKeepsOneResumableSession()
        {
            _ui.StartRound();
            var session = Session;
            for (var i = 0; i < 10; i++)
            {
                Pause(true);
                Focus(false);
                _root.SetActive(false);
                var elapsed = session.State.CurrentValue.ElapsedSeconds;
                yield return null;
                _root.SetActive(true);
                Focus(true);
                _ui.ResumeRound();
                Assert.That(session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Paused));
                Pause(false);
                Assert.That(session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(elapsed));
                _ui.ResumeRound();
                Assert.That(Session, Is.SameAs(session));
                Assert.That(session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CommittedSelectionSurvivesSuspendButRecreatedScreenStartsAtHome()
        {
            _ui.StartRound();
            var state = Session.State.CurrentValue;
            var found = false;
            for (var y = 0; y < state.Rules.Height && !found; y++)
            for (var x = 0; x < state.Rules.Width && !found; x++)
            for (var bottom = y; bottom < state.Rules.Height && !found; bottom++)
            for (var right = x; right < state.Rules.Width && !found; right++)
            {
                var rectangle = new CellRectangle(x, y, right, bottom);
                if (!Session.InspectSelection(rectangle).CanRemove)
                    continue;
                _root.SendMessage("Submit", rectangle);
                found = true;
            }
            Assert.That(found, Is.True, "The fixed test seed must have a valid move.");
            var committed = Session.State.CurrentValue;
            Assert.That(committed.Score, Is.GreaterThan(0));
            Pause(true);
            Pause(false);
            _ui.ResumeRound();
            Assert.That(Session.State.CurrentValue.Score, Is.EqualTo(committed.Score));
            CollectionAssert.AreEqual(committed.Cells, Session.State.CurrentValue.Cells);
            var old = Session;
            Object.Destroy(_root);
            yield return null;
            Assert.That(old.Tick(), Is.False);
            CreateScreen();
            yield return null;
            Assert.That(Session, Is.Null);
            Assert.That(Field<GameObject>("home").activeSelf, Is.True);
            Assert.That(Field<GameObject>("play").activeSelf, Is.False);
        }
    }
}
#endif
