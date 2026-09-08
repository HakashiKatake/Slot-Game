namespace SlotGame.Core
{
    /// <summary>
    /// Encapsulates the evaluated result of a spin, including payouts and bonus triggers.
    /// Immutable value container following SOLID principles.
    /// </summary>
    [System.Serializable]
    public class PayoutResult
    {
        public bool IsWin { get; }
        public int PayoutCredits { get; }
        public int Multiplier { get; }
        public WinTier Tier { get; }
        public string WinDescription { get; }
        public bool IsJackpot { get; }
        public bool IsFreeSpinsTriggered { get; }
        public int FreeSpinsAwarded { get; }
        public SymbolType? WinningSymbol { get; }

        public PayoutResult(
            bool isWin,
            int payoutCredits,
            int multiplier,
            WinTier tier,
            string description,
            bool isJackpot = false,
            bool isFreeSpins = false,
            int freeSpinsCount = 0,
            SymbolType? winningSymbol = null)
        {
            IsWin = isWin;
            PayoutCredits = payoutCredits;
            Multiplier = multiplier;
            Tier = tier;
            WinDescription = description;
            IsJackpot = isJackpot;
            IsFreeSpinsTriggered = isFreeSpins;
            FreeSpinsAwarded = freeSpinsCount;
            WinningSymbol = winningSymbol;
        }

        public static PayoutResult NoWin => new PayoutResult(
            false, 0, 0, WinTier.None, "No win. Try again!", false, false, 0, null);
    }
}
