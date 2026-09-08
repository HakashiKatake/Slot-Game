using System.Collections.Generic;
using SlotGame.Core;

namespace SlotGame.Services
{
    /// <summary>
    /// SOLID Abstraction: Provides decoupled, testable Random Number Generation for reel outcomes.
    /// Can be swapped for deterministic testing mocks or provably fair backends.
    /// </summary>
    public interface ISlotRngService
    {
        /// <summary>
        /// Selects a single symbol based on weighted probabilities.
        /// </summary>
        SymbolType GenerateRandomSymbol(IReadOnlyList<SymbolDataSO> symbols);

        /// <summary>
        /// Generates target symbols for all reels in a spin.
        /// </summary>
        SymbolType[] GenerateSpinResult(int reelCount, IReadOnlyList<SymbolDataSO> symbols);
    }
}
