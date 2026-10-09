using System;
using System.Collections.Generic;
using Squapple.Core;
using Squapple.Gameplay;
using UnityEngine;

namespace Squapple.Presentation
{
    public sealed class LocalProgress
    {
        public const string StorageKey = "Squapple.Progress";
        public const int FormatVersion = 1;

        [Serializable]
        public sealed class BestScore
        {
            public int rulesVersion;
            public int width;
            public int height;
            public double durationSeconds;
            public int score;

            public bool Matches(GameRules rules) => rulesVersion == GameRules.Version &&
                width == rules.Width && height == rules.Height && durationSeconds == rules.DurationSeconds;
        }

        [Serializable]
        public sealed class SavedRound
        {
            public BestScore record;
            public int[] initialCells;
            public bool practice;
            public GameEndReason endReason;

            public GameRules CreateRules() => new(record.width, record.height, record.durationSeconds);
        }

        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public List<BestScore> bestScores = new();
            public SavedRound lastRound;
        }

        private SaveData _data = new() { version = FormatVersion };
        public bool CanSave { get; private set; } = true;
        public SavedRound LastRound => _data.lastRound;

        public LocalProgress()
        {
            var json = PlayerPrefs.GetString(StorageKey, "");
            if (json.Length == 0)
                return;
            SaveData loaded;
            try
            {
                loaded = JsonUtility.FromJson<SaveData>(json);
            }
            catch (ArgumentException)
            {
                return;
            }
            if (loaded == null)
                return;
            if (loaded.version > FormatVersion)
            {
                CanSave = false;
                return;
            }
            if (loaded.version != FormatVersion)
                return;
            loaded.bestScores ??= new List<BestScore>();
            loaded.bestScores.RemoveAll(record => !ValidRecord(record));
            if (!ValidRound(loaded.lastRound))
                loaded.lastRound = null;
            _data = loaded;
        }

        public int GetBest(GameRules rules)
        {
            var best = 0;
            foreach (var record in _data.bestScores)
                if (record.Matches(rules))
                    best = Math.Max(best, record.score);
            return best;
        }

        public bool RecordFinished(GameSessionState state, IReadOnlyList<int> initialCells, bool practice)
        {
            if (!CanSave || state.Phase != GamePhase.Finished || state.EndReason == GameEndReason.Stopped)
                return false;
            var record = new BestScore
            {
                rulesVersion = GameRules.Version,
                width = state.Rules.Width,
                height = state.Rules.Height,
                durationSeconds = state.Rules.DurationSeconds,
                score = state.Score
            };
            var cells = new int[initialCells.Count];
            for (var i = 0; i < cells.Length; i++)
                cells[i] = initialCells[i];
            var round = new SavedRound { record = record, initialCells = cells,
                practice = practice, endReason = state.EndReason };
            if (!ValidRound(round))
                throw new ArgumentException("A saved round must contain its original generated board.", nameof(initialCells));
            _data.lastRound = round;
            if (!practice && state.Score > GetBest(state.Rules))
            {
                _data.bestScores.RemoveAll(previous => previous.Matches(state.Rules));
                _data.bestScores.Add(record);
            }
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(_data));
            PlayerPrefs.Save();
            return true;
        }

        private static bool ValidRecord(BestScore record) => record != null && record.rulesVersion > 0 &&
            record.width > 0 && record.height > 0 && (long)record.width * record.height >= 2 &&
            (long)record.width * record.height <= int.MaxValue && record.durationSeconds > 0 &&
            !double.IsNaN(record.durationSeconds) && !double.IsInfinity(record.durationSeconds) &&
            record.score >= 0 && record.score <= (long)record.width * record.height;

        private static bool ValidRound(SavedRound round)
        {
            if (round == null || !ValidRecord(round.record) || round.record.rulesVersion != GameRules.Version ||
                round.initialCells == null || round.initialCells.LongLength != (long)round.record.width * round.record.height ||
                (round.endReason != GameEndReason.Cleared && round.endReason != GameEndReason.NoMoves &&
                 round.endReason != GameEndReason.TimeExpired))
                return false;
            var remainder = 0;
            foreach (var cell in round.initialCells)
            {
                if (cell < 1 || cell > 9)
                    return false;
                remainder = (remainder + cell) % GameRules.TargetSum;
            }
            return remainder == 0;
        }
    }
}
