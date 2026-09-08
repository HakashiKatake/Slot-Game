using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Core;
using SlotGame.Services;
using SlotGame.Reels;
using SlotGame.UI;
using SlotGame.Audio;

namespace SlotGame.Controllers
{
    /// <summary>
    /// Master Presenter (Model-View-Presenter Architecture).
    /// Binds domain Model, Views (Cabinet and HUD), Audio, and algorithmic Services.
    /// Manages spin flow, payout evaluation, and automated Free Spins sequences.
    /// </summary>
    public class SlotMachinePresenter : MonoBehaviour
    {
        [Header("Configurations")]
        [SerializeField] private SlotGameConfigSO gameConfig;
        [SerializeField] private PayoutTableSO payoutTable;
        [SerializeField] private List<SymbolDataSO> symbols = new List<SymbolDataSO>();

        [Header("Views")]
        [SerializeField] private ReelsManager reelsManager;
        [SerializeField] private SlotMachineView machineView;
        [SerializeField] private SlotUIView uiView;

        [Header("Services")]
        [SerializeField] private SlotAudioService audioService;

        private SlotGameModel _model;
        private ISlotRngService _rngService;
        private IPayoutEvaluator _evaluator;
        private Coroutine _autoSpinCoroutine;

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            // Fallbacks if not assigned in Inspector
            if (gameConfig == null)
            {
                gameConfig = ScriptableObject.CreateInstance<SlotGameConfigSO>();
                gameConfig.InitializeDefaults();
            }
            if (payoutTable == null)
            {
                payoutTable = ScriptableObject.CreateInstance<PayoutTableSO>();
                payoutTable.InitializeDefaults();
            }

            // Instantiate domain model and services (Dependency Injection)
            _model = new SlotGameModel(
                gameConfig.StartingBalance,
                gameConfig.BetAmounts,
                gameConfig.DefaultBetIndex);

            _rngService = new StandardSlotRngService();
            _evaluator = new ClassicPayoutEvaluator();

            // Initialize views
            if (reelsManager != null)
            {
                reelsManager.Initialize(symbols, gameConfig);
                reelsManager.OnIndividualReelStopped += HandleIndividualReelStopped;
                reelsManager.OnAllReelsStopped += HandleAllReelsStopped;
            }

            // Bind UI Events
            if (uiView != null)
            {
                uiView.OnSpinClicked += HandleSpinRequest;
                uiView.OnBetPlusClicked += HandleBetPlus;
                uiView.OnBetMinusClicked += HandleBetMinus;
                uiView.OnMaxBetClicked += HandleMaxBet;
                uiView.OnSoundToggled += HandleSoundToggle;
                uiView.OnResetCreditsClicked += HandleResetCredits;
            }

            // Bind Physical Cabinet Events
            if (machineView != null)
            {
                machineView.OnLeverClicked += HandleSpinRequest;
            }

            // Bind Model Events to UI View
            if (uiView != null)
            {
                _model.OnBalanceChanged += uiView.UpdateBalance;
                _model.OnBetChanged += uiView.UpdateBet;
                _model.OnWinChanged += w => uiView.UpdateWin(w);
                _model.OnFreeSpinsChanged += uiView.UpdateFreeSpins;

                // Set initial HUD values
                uiView.UpdateBalance(_model.Balance);
                uiView.UpdateBet(_model.CurrentBet);
                uiView.UpdateWin(0, false);
                uiView.UpdateFreeSpins(_model.FreeSpinsRemaining);
                uiView.SetStatusMessage("PULL LEVER OR CLICK SPIN!");
                uiView.SetControlsInteractable(true, true);
                if (audioService != null)
                {
                    uiView.UpdateSoundButton(audioService.IsMuted);
                }
            }
        }

        private void OnDestroy()
        {
            if (reelsManager != null)
            {
                reelsManager.OnIndividualReelStopped -= HandleIndividualReelStopped;
                reelsManager.OnAllReelsStopped -= HandleAllReelsStopped;
            }
            if (uiView != null)
            {
                uiView.OnSpinClicked -= HandleSpinRequest;
                uiView.OnBetPlusClicked -= HandleBetPlus;
                uiView.OnBetMinusClicked -= HandleBetMinus;
                uiView.OnMaxBetClicked -= HandleMaxBet;
                uiView.OnSoundToggled -= HandleSoundToggle;
                uiView.OnResetCreditsClicked -= HandleResetCredits;
            }
            if (machineView != null)
            {
                machineView.OnLeverClicked -= HandleSpinRequest;
            }
        }

        private void HandleSpinRequest()
        {
            if (!_model.CanSpin())
            {
                if (_model.Balance < _model.CurrentBet && !_model.IsFreeSpinsActive)
                {
                    if (uiView != null)
                    {
                        uiView.SetStatusMessage("OUT OF CREDITS! CLICK RELOAD TO PLAY.");
                    }
                    if (audioService != null) audioService.PlayButtonClick();
                }
                return;
            }

            // 1. Audio & Lever feedback
            if (audioService != null)
            {
                audioService.PlayLeverPull();
                audioService.PlaySpinLoop();
            }
            if (machineView != null)
            {
                machineView.TriggerLeverPull();
            }

            // 2. Model updates
            _model.SetState(SlotGameState.Spinning);
            _model.TryDeductBet();

            // 3. View updates
            if (uiView != null)
            {
                uiView.UpdateWin(0, false);
                uiView.SetControlsInteractable(false, false);
                uiView.SetStatusMessage(_model.IsFreeSpinsActive
                    ? $"FREE SPIN ({_model.FreeSpinsRemaining + 1} REMAINING) - 2x MULTIPLIER!"
                    : "SPINNING... GOOD LUCK!");
            }

            // 4. Generate RNG result & spin reels
            var targets = _rngService.GenerateSpinResult(3, symbols);
            if (reelsManager != null)
            {
                reelsManager.SpinAll(targets);
            }
        }

        private void HandleIndividualReelStopped(int reelIndex, SymbolType landedSymbol)
        {
            if (audioService != null)
            {
                audioService.PlayReelStop(reelIndex);
            }
        }

        private void HandleAllReelsStopped(SymbolType[] finalOutcome)
        {
            if (audioService != null)
            {
                audioService.StopSpinLoop();
            }

            _model.SetState(SlotGameState.Evaluating);

            // Evaluate payout on payline
            var result = _evaluator.Evaluate(
                finalOutcome,
                _model.CurrentBet,
                _model.IsFreeSpinsActive,
                payoutTable);

            if (result.IsWin)
            {
                _model.AddPayout(result.PayoutCredits);

                if (uiView != null)
                {
                    uiView.SetStatusMessage(result.WinDescription);
                }

                // Highlight winning line
                Color highlightColor = GetSymbolColor(result.WinningSymbol);
                if (reelsManager != null)
                {
                    reelsManager.HighlightWinningLine(highlightColor, 1.8f);
                }

                if (audioService != null)
                {
                    audioService.PlayWin(result.Tier);
                }

                // Show celebratory popup for Jackpots or Free Spins triggers
                if (result.IsJackpot && uiView != null)
                {
                    uiView.ShowWinPopup("JACKPOT!", result.PayoutCredits, result.WinDescription);
                }
                else if (result.IsFreeSpinsTriggered && uiView != null)
                {
                    _model.AddFreeSpins(result.FreeSpinsAwarded);
                    uiView.ShowWinPopup("BELL BONUS TRIGGERED!", result.PayoutCredits,
                        $"3 BELLS! YOU WON {result.FreeSpinsAwarded} FREE SPINS WITH 2X PAYOUTS!");
                }
                else if (result.Tier == WinTier.BigWin && uiView != null)
                {
                    uiView.ShowWinPopup("BIG WIN!", result.PayoutCredits, result.WinDescription);
                }
            }
            else
            {
                if (uiView != null)
                {
                    uiView.SetStatusMessage(_model.IsFreeSpinsActive
                        ? "NO WIN ON FREE SPIN. ROLLING AGAIN..."
                        : "NO WIN. TRY AGAIN!");
                }
            }

            // Post-spin resolution
            if (_model.IsFreeSpinsActive)
            {
                _model.SetState(SlotGameState.FreeSpins);
                if (_autoSpinCoroutine != null) StopCoroutine(_autoSpinCoroutine);
                _autoSpinCoroutine = StartCoroutine(AutoFreeSpinSequence());
            }
            else
            {
                _model.SetState(SlotGameState.Idle);
                if (uiView != null)
                {
                    uiView.SetControlsInteractable(true, true);
                }
            }
        }

        private IEnumerator AutoFreeSpinSequence()
        {
            // Dramatic pause between free spins
            yield return new WaitForSeconds(1.4f);
            HandleSpinRequest();
        }

        private void HandleBetPlus()
        {
            if (audioService != null) audioService.PlayButtonClick();
            _model.CycleBet(1, gameConfig.BetAmounts);
        }

        private void HandleBetMinus()
        {
            if (audioService != null) audioService.PlayButtonClick();
            _model.CycleBet(-1, gameConfig.BetAmounts);
        }

        private void HandleMaxBet()
        {
            if (audioService != null) audioService.PlayButtonClick();
            _model.SetMaxBet(gameConfig.BetAmounts);
        }

        private void HandleSoundToggle()
        {
            if (audioService != null)
            {
                audioService.ToggleMute();
                if (uiView != null)
                {
                    uiView.UpdateSoundButton(audioService.IsMuted);
                }
            }
        }

        private void HandleResetCredits()
        {
            if (audioService != null) audioService.PlayButtonClick();
            _model.ResetBalance(gameConfig.StartingBalance);
            if (uiView != null)
            {
                uiView.SetStatusMessage("CREDITS RELOADED! READY TO PLAY.");
                uiView.SetControlsInteractable(true, true);
            }
        }

        private Color GetSymbolColor(SymbolType? symbolType)
        {
            if (!symbolType.HasValue) return Color.yellow;
            for (int i = 0; i < symbols.Count; i++)
            {
                if (symbols[i].SymbolType == symbolType.Value)
                {
                    return symbols[i].ThemeColor;
                }
            }
            return Color.yellow;
        }
    }
}
