namespace SlotGame.Core
{
    public enum SymbolType { Cherry = 0, Bar = 1, Bell = 2, Seven = 3 }

    public enum SlotGameState { Idle, Spinning, Evaluating, FreeSpins }

    public enum WinTier { None, SmallWin, MediumWin, BigWin, Jackpot }

    public enum ReelState { Idle, Anticipation, SpinningFast, Stopping, Bouncing }
}
