using System;
using SlotGame.Core;

namespace SlotGame.Core
{
    /// <summary>
    /// Pure domain Model representing slot machine game state and business rules.
    /// Free of Unity Engine view dependencies for high testability and clean architecture.
    /// </summary>
    public class SlotGameModel
    {
        private readonly int[] _betAmounts;

        public int Balance { get; private set; }
        public int CurrentBet { get; private set; }
        public int SelectedBetIndex { get; private set; }
        public int LastWin { get; private set; }
        public int TotalWon { get; private set; }
        public int FreeSpinsRemaining { get; private set; }
        public SlotGameState CurrentState { get; private set; }

        public bool IsFreeSpinsActive => FreeSpinsRemaining > 0;

        public event Action<int> OnBalanceChanged;
        public event Action<int> OnBetChanged;
        public event Action<int> OnWinChanged;
        public event Action<int> OnFreeSpinsChanged;
        public event Action<SlotGameState> OnStateChanged;

        public SlotGameModel(int initialBalance, int[] betAmounts, int defaultBetIndex = 0)
        {
            if (betAmounts == null || betAmounts.Length == 0)
            {
                throw new ArgumentException("Bet amounts cannot be null or empty", nameof(betAmounts));
            }

            _betAmounts = betAmounts;
            Balance = initialBalance;
            SelectedBetIndex = Math.Clamp(defaultBetIndex, 0, _betAmounts.Length - 1);
            CurrentBet = _betAmounts[SelectedBetIndex];
            LastWin = 0;
            TotalWon = 0;
            FreeSpinsRemaining = 0;
            CurrentState = SlotGameState.Idle;
        }

        public bool CanSpin()
        {
            if (CurrentState != SlotGameState.Idle && CurrentState != SlotGameState.FreeSpins)
            {
                return false;
            }

            if (IsFreeSpinsActive)
            {
                return true;
            }

            return Balance >= CurrentBet;
        }

        public bool TryDeductBet()
        {
            if (IsFreeSpinsActive)
            {
                // Free spins do not cost credits!
                ConsumeFreeSpin();
                return true;
            }

            if (Balance < CurrentBet)
            {
                return false;
            }

            Balance -= CurrentBet;
            OnBalanceChanged?.Invoke(Balance);
            return true;
        }

        public void AddPayout(int amount)
        {
            if (amount <= 0) return;

            LastWin = amount;
            TotalWon += amount;
            Balance += amount;

            OnWinChanged?.Invoke(LastWin);
            OnBalanceChanged?.Invoke(Balance);
        }

        public void ClearLastWin()
        {
            LastWin = 0;
            OnWinChanged?.Invoke(0);
        }

        public void CycleBet(int delta)
        {
            if (IsFreeSpinsActive || CurrentState != SlotGameState.Idle) return;

            int newIndex = Math.Clamp(SelectedBetIndex + delta, 0, _betAmounts.Length - 1);
            if (newIndex != SelectedBetIndex)
            {
                SelectedBetIndex = newIndex;
                CurrentBet = _betAmounts[SelectedBetIndex];
                OnBetChanged?.Invoke(CurrentBet);
            }
        }

        public void SetMaxBet()
        {
            if (IsFreeSpinsActive || CurrentState != SlotGameState.Idle) return;

            int maxIndex = _betAmounts.Length - 1;
            if (SelectedBetIndex != maxIndex)
            {
                SelectedBetIndex = maxIndex;
                CurrentBet = _betAmounts[SelectedBetIndex];
                OnBetChanged?.Invoke(CurrentBet);
            }
        }

        public void SetBetDirect(int bet)
        {
            if (IsFreeSpinsActive || CurrentState != SlotGameState.Idle) return;

            for (int i = 0; i < _betAmounts.Length; i++)
            {
                if (_betAmounts[i] == bet)
                {
                    SelectedBetIndex = i;
                    CurrentBet = _betAmounts[SelectedBetIndex];
                    OnBetChanged?.Invoke(CurrentBet);
                    return;
                }
            }
        }

        public void AddFreeSpins(int count)
        {
            if (count <= 0) return;
            FreeSpinsRemaining += count;
            OnFreeSpinsChanged?.Invoke(FreeSpinsRemaining);
        }

        private void ConsumeFreeSpin()
        {
            if (FreeSpinsRemaining > 0)
            {
                FreeSpinsRemaining--;
                OnFreeSpinsChanged?.Invoke(FreeSpinsRemaining);
            }
        }

        public void SetState(SlotGameState newState)
        {
            if (CurrentState != newState)
            {
                CurrentState = newState;
                OnStateChanged?.Invoke(CurrentState);
            }
        }

        public void ResetBalance(int amount)
        {
            Balance = amount;
            LastWin = 0;
            TotalWon = 0;
            FreeSpinsRemaining = 0;
            OnBalanceChanged?.Invoke(Balance);
            OnWinChanged?.Invoke(0);
            OnFreeSpinsChanged?.Invoke(0);
        }
    }
}
