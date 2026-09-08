using System;
using System.Collections.Generic;
using SlotGame.Core;

namespace SlotGame.Services
{
    /// <summary>
    /// Weighted random number generator for reel outcomes.
    /// Symbols with higher RngWeight appear more frequently.
    /// </summary>
    public class SlotRngService
    {
        private readonly Random _random = new Random();

        public SymbolType[] GenerateSpinResult(int reelCount, IReadOnlyList<SymbolDataSO> symbols)
        {
            var results = new SymbolType[reelCount];
            for (int i = 0; i < reelCount; i++)
                results[i] = PickWeighted(symbols);
            return results;
        }

        private SymbolType PickWeighted(IReadOnlyList<SymbolDataSO> symbols)
        {
            int total = 0;
            for (int i = 0; i < symbols.Count; i++) total += symbols[i].RngWeight;

            int roll = _random.Next(0, total);
            int accumulated = 0;
            for (int i = 0; i < symbols.Count; i++)
            {
                accumulated += symbols[i].RngWeight;
                if (roll < accumulated) return symbols[i].SymbolType;
            }
            return symbols[symbols.Count - 1].SymbolType;
        }
    }
}
