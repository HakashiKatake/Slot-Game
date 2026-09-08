namespace SlotGame.Core
{
    /// <summary>
    /// Lifecycle states for the slot machine state machine.
    /// </summary>
    public enum SlotGameState
    {
        Idle,
        Spinning,
        ReelStopping,
        Evaluating,
        ShowingWin,
        FreeSpins
    }

    /// <summary>
    /// Category of win awarded to the player.
    /// </summary>
    public enum WinTier
    {
        None,
        SmallWin,
        MediumWin,
        BigWin,
        Jackpot
    }
}
