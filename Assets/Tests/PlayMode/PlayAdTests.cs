#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Squapple.Core;
using Squapple.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Squapple.Presentation.Tests
{
    public sealed class PlayAdTests
    {
        private GameObject _root;
        private PlayPrototype _ui;
        private FakeAds _ads;
        private string _progress;
        private string _frequency;
        private bool _hadProgress;
        private bool _hadFrequency;
        private Random.State _random;
        private T Field<T>(string name) => (T)typeof(PlayPrototype)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_ui);
        private GameSession Session => Field<GameSession>("_session");

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _hadProgress = PlayerPrefs.HasKey(LocalProgress.StorageKey);
            _progress = PlayerPrefs.GetString(LocalProgress.StorageKey);
            _hadFrequency = PlayerPrefs.HasKey(AdFrequency.StorageKey);
            _frequency = PlayerPrefs.GetString(AdFrequency.StorageKey);
            PlayerPrefs.DeleteKey(LocalProgress.StorageKey);
            PlayerPrefs.DeleteKey(AdFrequency.StorageKey);
            _random = Random.state;
            _root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayPrototype.prefab"));
            _ui = _root.GetComponent<PlayPrototype>();
            typeof(PlayPrototype).GetField("enableAds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_ui, true);
            Field<IInterstitialAds>("_ads").Dispose();
            _ads = new FakeAds();
            typeof(PlayPrototype).GetField("_ads", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_ui, _ads);
            _ads.Initialize(() => _root.SendMessage("Suspend"));
            yield return null;
            _root.SendMessage("OnApplicationFocus", true);
            _root.SendMessage("OnApplicationPause", false);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_root);
            yield return null;
            Restore(LocalProgress.StorageKey, _hadProgress, _progress);
            Restore(AdFrequency.StorageKey, _hadFrequency, _frequency);
            PlayerPrefs.Save();
            Random.state = _random;
        }

        private static void Restore(string key, bool existed, string value)
        {
            if (existed) PlayerPrefs.SetString(key, value);
            else PlayerPrefs.DeleteKey(key);
        }

        private void EarnBreak()
        {
            for (var i = 0; i < 5; i++)
                Field<AdFrequency>("_adFrequency").RecordRound(120);
        }

        private void Complete(bool practice = false)
        {
            typeof(PlayPrototype).GetMethod("BeginRound", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_ui, new object[] { new GameRules(2, 1), new[] { 3, 7 }, practice });
            _root.SendMessage("Submit", new CellRectangle(0, 0, 1, 0));
        }

        private void Next() => Field<Button>("newGameButton").onClick.Invoke();

        [UnityTest]
        public IEnumerator DefaultPrefabDoesNotStartAdsOrOfferThemWithASavedEligibleInterval()
        {
            EarnBreak();
            var saved = PlayerPrefs.GetString(AdFrequency.StorageKey);
            Object.Destroy(_root);
            yield return null;
            _root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayPrototype.prefab"));
            _ui = _root.GetComponent<PlayPrototype>();
            Assert.That(Field<bool>("enableAds"), Is.False);
            // Exercise Start even when the editor SDK preview is explicitly enabled.
            typeof(PlayPrototype).GetField("initializeAdsInEditor", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_ui, true);
            yield return null;
            _root.SendMessage("OnApplicationFocus", true);
            _root.SendMessage("OnApplicationPause", false);
            var sdk = Field<IInterstitialAds>("_ads");
            Assert.That(typeof(AdMobInterstitial).GetField("_started", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(sdk), Is.False);
            Assert.That(sdk.IsReady, Is.False);
            Assert.That(sdk.PrivacyOptionsRequired, Is.False);
            Complete();
            Assert.That(Field<Text>("adNotice").text, Is.Empty);
            Assert.That(Field<Button>("newGameButton").GetComponentInChildren<Text>().text, Is.EqualTo("새 판"));
            _ui.OpenSettings();
            Assert.That(Field<Button>("privacyOptionsButton").gameObject.activeSelf, Is.False);
            _ui.CloseSettings();
            Next();
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
            Assert.That(PlayerPrefs.GetString(AdFrequency.StorageKey), Is.EqualTo(saved));
        }

        [UnityTest]
        public IEnumerator EditorSdkCompletesConsentAndLoadsATestAdFromColdStart()
        {
            using var sdk = new AdMobInterstitial(true);
            sdk.Initialize(() => { });
            var deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (!sdk.IsReady && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert.That(sdk.IsReady, Is.True, "Consent and SDK callbacks must run before an ad can load.");
        }

        [Test]
        public void FirstRoundAndPracticeNeverOfferAds()
        {
            Complete();
            Assert.That(Field<Text>("adNotice").text, Is.Empty);
            Next();
            Assert.That(_ads.Shows, Is.Zero);
            EarnBreak();
            var saved = PlayerPrefs.GetString(AdFrequency.StorageKey);
            Complete(true);
            Assert.That(Field<Text>("adNotice").text, Is.Empty);
            Next();
            Assert.That(_ads.Shows, Is.Zero);
            Assert.That(PlayerPrefs.GetString(AdFrequency.StorageKey), Is.EqualTo(saved));
        }

        [UnityTest]
        public IEnumerator AnnouncedAdSavesResultThenStartsOnlyOneRoundAfterClose()
        {
            EarnBreak();
            Complete();
            Assert.That(new LocalProgress().LastRound.record.score, Is.EqualTo(2));
            Assert.That(Field<Text>("adNotice").text, Does.Contain("광고"));
            var completed = Session;
            Next();
            Next();
            _ui.StartRound();
            Assert.That(_ads.Shows, Is.EqualTo(1));
            Assert.That(Session, Is.SameAs(completed));
            _ads.Close();
            yield return null;
            Assert.That(Session, Is.Not.SameAs(completed));
            var next = Session;
            _ads.Close();
            yield return null;
            Assert.That(Session, Is.SameAs(next));
            Assert.That(new AdFrequency().IsEligible, Is.False);
        }

        [Test]
        public void LateLoadDoesNotAddAnAdToAnUnannouncedResult()
        {
            EarnBreak();
            _ads.Ready = false;
            Complete();
            _ads.Ready = true;
            _root.SendMessage("Render", Session.State.CurrentValue);
            Assert.That(Field<Text>("adNotice").text, Is.Empty);
            Next();
            Assert.That(_ads.Shows, Is.Zero);
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
            Assert.That(new AdFrequency().IsEligible, Is.False);
        }

        [Test]
        public void AdThatExpiresAfterTheNoticeDoesNotBlockTheNextRound()
        {
            EarnBreak();
            Complete();
            _ads.Ready = false;
            Next();
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
            Assert.That(_ads.Shows, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CloseInBackgroundWaitsForBothApplicationSignals()
        {
            EarnBreak();
            Complete();
            Next();
            var finished = Session;
            _root.SendMessage("OnApplicationPause", true);
            _root.SendMessage("OnApplicationFocus", false);
            _ads.Close();
            yield return null;
            Assert.That(Session, Is.SameAs(finished));
            _root.SendMessage("OnApplicationPause", false);
            yield return null;
            Assert.That(Session, Is.SameAs(finished));
            _root.SendMessage("OnApplicationFocus", true);
            yield return null;
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
        }

        [UnityTest]
        public IEnumerator DisabledScreenIgnoresALateClose()
        {
            EarnBreak();
            Complete();
            Next();
            var finished = Session;
            _root.SetActive(false);
            _ads.Close();
            _root.SetActive(true);
            yield return null;
            Assert.That(Session, Is.SameAs(finished));
        }

        [UnityTest]
        public IEnumerator PrivacyFormPausesGameAndFailureLeavesManualResumeAvailable()
        {
            _ui.StartRound();
            _ui.OpenSettings();
            Field<Button>("privacyOptionsButton").onClick.Invoke();
            var elapsed = Session.State.CurrentValue.ElapsedSeconds;
            _ui.ResumeRound();
            yield return null;
            Assert.That(Session.State.CurrentValue.ElapsedSeconds, Is.EqualTo(elapsed));
            _ads.FinishPrivacy(false);
            Assert.That(Field<Text>("privacyStatus").text, Does.Contain("다시 시도"));
            _ui.CloseSettings();
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Paused));
            _ui.ResumeRound();
            Assert.That(Session.State.CurrentValue.Phase, Is.EqualTo(GamePhase.Playing));
        }

        private sealed class FakeAds : IInterstitialAds
        {
            public bool Ready = true;
            public bool IsReady => Ready && !IsBusy;
            public bool IsBusy { get; private set; }
            public bool PrivacyOptionsRequired => true;
            public int Shows;
            private Action _closed;
            private Action _beforePrivacy;
            private Action<bool> _privacyDone;
            public void Initialize(Action beforePrivacyForm) => _beforePrivacy = beforePrivacyForm;
            public void Prepare() { }
            public bool TryShow(Action opened, Action closed)
            {
                if (!IsReady) return false;
                Shows++;
                IsBusy = true;
                _closed = closed;
                opened();
                return true;
            }
            public void Close() { IsBusy = false; _closed?.Invoke(); }
            public void ShowPrivacyOptions(Action<bool> completed)
            {
                IsBusy = true;
                _beforePrivacy();
                _privacyDone = completed;
            }
            public void FinishPrivacy(bool success) { IsBusy = false; _privacyDone(success); }
            public void Dispose() { }
        }
    }
}
#endif
