using System;
using System.Collections.Generic;
using Squapple.Core;

namespace Squapple.Gameplay
{
    public readonly struct GameSessionState
    {
        public GameRules Rules { get; }
        public IReadOnlyList<int> Cells { get; }
        public GamePhase Phase { get; }
        public int Score { get; }
        public int RemainingApples { get; }
        public double ElapsedSeconds { get; }
        public double RemainingSeconds => Math.Max(0, Rules.DurationSeconds - ElapsedSeconds);
        public GameEndReason EndReason { get; }

        internal GameSessionState(GameRules rules, IReadOnlyList<int> cells, GamePhase phase,
            int score, int remainingApples, double elapsedSeconds, GameEndReason endReason)
        {
            Rules = rules;
            Cells = cells;
            Phase = phase;
            Score = score;
            RemainingApples = remainingApples;
            ElapsedSeconds = elapsedSeconds;
            EndReason = endReason;
        }
    }
}
