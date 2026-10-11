using System;
using NUnit.Framework;
using UnityEngine;

namespace Squapple.Presentation.Tests
{
    public sealed class AdFrequencyTests
    {
        private bool _hadSave;
        private string _saved;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _hadSave = PlayerPrefs.HasKey(AdFrequency.StorageKey);
            _saved = PlayerPrefs.GetString(AdFrequency.StorageKey);
            PlayerPrefs.DeleteKey(AdFrequency.StorageKey);
            _now = new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadSave)
                PlayerPrefs.SetString(AdFrequency.StorageKey, _saved);
            else
                PlayerPrefs.DeleteKey(AdFrequency.StorageKey);
            PlayerPrefs.Save();
        }

        private AdFrequency Load() => new(() => _now);
        private static void EarnBreak(AdFrequency frequency)
        {
            for (var i = 0; i < 5; i++)
                frequency.RecordRound(120);
        }

        [Test]
        public void BothCompletedRoundsAndActiveTimeAreRequired()
        {
            var frequency = Load();
            Assert.That(frequency.IsEligible, Is.False);
            for (var i = 0; i < 10; i++)
                frequency.RecordRound(1);
            Assert.That(frequency.IsEligible, Is.False);
            frequency.RecordRound(589);
            Assert.That(frequency.IsEligible, Is.False);
            frequency.RecordRound(1);
            Assert.That(frequency.IsEligible, Is.True);
        }

        [Test]
        public void ElapsedTimeAloneCannotEarnAnAd()
        {
            var frequency = Load();
            frequency.RecordRound(600);
            Assert.That(frequency.IsEligible, Is.False);
            for (var i = 0; i < 4; i++)
                frequency.RecordRound(0);
            Assert.That(frequency.IsEligible, Is.True);
        }

        [Test]
        public void ProgressSurvivesReloadAndSkippedBreakRestartsTheInterval()
        {
            EarnBreak(Load());
            Assert.That(Load().IsEligible, Is.True);
            Load().ResetInterval();
            Assert.That(Load().IsEligible, Is.False);
            EarnBreak(Load());
            Assert.That(Load().IsEligible, Is.True);
        }

        [Test]
        public void TwoAdsAreAllowedPerRollingDayEvenAcrossReloadAndMidnight()
        {
            var frequency = Load();
            EarnBreak(frequency);
            frequency.RecordShown();
            _now = _now.AddMinutes(10);
            EarnBreak(frequency);
            Assert.That(frequency.IsEligible, Is.True);
            frequency.RecordShown();
            EarnBreak(frequency);
            _now = _now.AddHours(12);
            Assert.That(Load().IsEligible, Is.False);
            _now = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
            Assert.That(Load().IsEligible, Is.True);
            Load().RecordShown();
            EarnBreak(Load());
            Assert.That(Load().IsEligible, Is.False);
            _now = _now.AddMinutes(10);
            Assert.That(Load().IsEligible, Is.True);
        }

        [Test]
        public void MovingClockBackDoesNotResetTheCap()
        {
            var frequency = Load();
            frequency.RecordShown();
            frequency.RecordShown();
            EarnBreak(frequency);
            _now = _now.AddDays(-1);
            Assert.That(Load().IsEligible, Is.False);
        }

        [TestCase("broken json")]
        [TestCase("{\"version\":2,\"rounds\":5,\"seconds\":600}")]
        [TestCase("{\"version\":1,\"rounds\":-1,\"seconds\":600}")]
        public void UnreadableOrNewerDataDisablesAdsWithoutOverwritingIt(string saved)
        {
            PlayerPrefs.SetString(AdFrequency.StorageKey, saved);
            var frequency = Load();
            EarnBreak(frequency);
            frequency.ResetInterval();
            frequency.RecordShown();
            Assert.That(frequency.IsEligible, Is.False);
            Assert.That(PlayerPrefs.GetString(AdFrequency.StorageKey), Is.EqualTo(saved));
        }
    }
}
