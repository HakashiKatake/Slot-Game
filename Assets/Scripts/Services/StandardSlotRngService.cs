using System;
using System.Collections.Generic;
using SlotGame.Core;

namespace SlotGame.Services
{
    /// <summary>
    /// Standard implementation of ISlotRngService using cryptographically/pseudo-random weighted sampling.
    /// Ensures unpredictability and adheres to specified symbol weightings.
    /// </summary>
    public class StandardSlotRngService : ISlotRngService
    {
        private readonly Random _random;

        public StandardSlotRngService(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public SymbolType GenerateRandomSymbol(IReadOnlyList<SymbolDataSO> symbols)
        {
            if (symbols == null || symbols.Count == 0)
            {
                throw new ArgumentException("Symbols list cannot be null or empty.");
            }

            int totalWeight = 0;
            for (int i = 0; i < symbols.Count; i++)
            {
                totalWeight += symbols[i].RngWeight;
            }

            int roll = _random.Next(0, totalWeight);
            int accumulated = 0;

            for (int i = 0; i < symbols.Count; i++)
            {
                accumulated += symbols[i].RngWeight;
                if (roll < accumulated)
                {
                    return symbols[i].SymbolType;
                }
            }

            return symbols[symbols.Count - 1].SymbolType;
        }

        public SymbolType[] GenerateSpinResult(int reelCount, IReadOnlyList<SymbolDataSO> symbols)
        {
            var results = new SymbolType[reelCount];
            for (int i = 0; i < reelCount; i++)
            {
                results[i] = GenerateRandomSymbol(symbols);
            }
            return results;
        }
    }
}
