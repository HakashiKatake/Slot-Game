using SlotGame.Core;

namespace SlotGame.Services
{
    /// <summary>
    /// SOLID Abstraction: Evaluates reel results against payout rules.
    /// Pure logic decoupled from rendering or engine state for unit testability.
    /// </summary>
    public interface IPayoutEvaluator
    {
        PayoutResult Evaluate(
            SymbolType[] paylineSymbols,
            int currentBet,
            bool isFreeSpin,
            PayoutTableSO payoutTable);
    }
}
