using System;
using UnityEngine;

namespace Squapple.Presentation
{
    public sealed class AdFrequency
    {
        public const string StorageKey = "Squapple.Ads";
        public const int RoundsBetweenAds = 5;
        public const double SecondsBetweenAds = 600;
        private readonly Func<DateTime> _utcNow;

        [Serializable]
        private sealed class Data
        {
            public int version = 1;
            public int rounds;
            public double seconds;
            public long latestShown;
            public long previousShown;
        }

        private Data _data = new();
        private bool _canSave = true;

        public AdFrequency(Func<DateTime> utcNow = null)
        {
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            var json = PlayerPrefs.GetString(StorageKey, "");
            if (json.Length == 0)
                return;
            try
            {
                var saved = JsonUtility.FromJson<Data>(json);
                if (saved == null || saved.version != 1 || saved.rounds < 0 ||
                    saved.seconds < 0 || double.IsNaN(saved.seconds) || double.IsInfinity(saved.seconds) ||
                    saved.latestShown < 0 || saved.previousShown < 0 || saved.previousShown > saved.latestShown)
                    _canSave = false;
                else
                {
                    _data = saved;
                    _data.rounds = Math.Min(RoundsBetweenAds, _data.rounds);
                    _data.seconds = Math.Min(SecondsBetweenAds, _data.seconds);
                }
            }
            catch (ArgumentException)
            {
                _canSave = false;
            }
        }

        public bool IsEligible => _canSave && _data.rounds >= RoundsBetweenAds &&
            _data.seconds >= SecondsBetweenAds &&
            (_data.previousShown == 0 || _utcNow().Ticks - _data.previousShown >= TimeSpan.TicksPerDay);

        public void RecordRound(double elapsedSeconds)
        {
            if (!_canSave)
                return;
            _data.rounds = Math.Min(RoundsBetweenAds, _data.rounds + 1);
            _data.seconds = Math.Min(SecondsBetweenAds, _data.seconds + elapsedSeconds);
            Save();
        }

        // A missed opportunity is not carried into the next round as ad debt.
        public void ResetInterval()
        {
            if (!_canSave)
                return;
            _data.rounds = 0;
            _data.seconds = 0;
            Save();
        }

        public void RecordShown()
        {
            if (!_canSave)
                return;
            _data.previousShown = _data.latestShown;
            _data.latestShown = _utcNow().Ticks;
            ResetInterval();
        }

        private void Save()
        {
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(_data));
            PlayerPrefs.Save();
        }
    }
}
