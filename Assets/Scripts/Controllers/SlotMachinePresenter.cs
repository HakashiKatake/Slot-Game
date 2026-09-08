using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SlotGame.Core;
using SlotGame.Reels;
using SlotGame.UI;
using SlotGame.Audio;
using SlotGame.Services;

namespace SlotGame.Controllers
{
    /// <summary>
    /// MVP Presenter — orchestrates game flow between model, reels, UI, and audio.
    /// </summary>
    public class SlotMachinePresenter : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SlotGameConfigSO gameConfig;
        [SerializeField] private PayoutTableSO payoutTable;
        [SerializeField] private List<SymbolDataSO> symbols = new();

        [Header("Scene References")]
        [SerializeField] private ReelsManager reelsManager;
        [SerializeField] private SlotMachineView machineView;
        [SerializeField] private SlotUIView uiView;
        [SerializeField] private SlotAudioService audioService;

        private SlotGameModel _model;
        private SlotRngService _rng;
        private PayoutEvaluator _evaluator;
        private bool _isSpinning;
        private Coroutine _autoSpinCoroutine;

        private void Start()
        {
            LoadSymbolsIfEmpty();
            InitGame();
        }

        private void LoadSymbolsIfEmpty()
        {
#if UNITY_EDITOR
            if (symbols == null || symbols.Count == 0)
            {
                symbols = new List<SymbolDataSO>();
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:SymbolDataSO"))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var s = UnityEditor.AssetDatabase.LoadAssetAtPath<SymbolDataSO>(path);
                    if (s != null) symbols.Add(s);
                }
            }
#endif
            symbols?.RemoveAll(s => s == null);
        }

        private void InitGame()
        {
            if (gameConfig == null) { Debug.LogError("[Presenter] gameConfig missing!"); return; }
            if (payoutTable == null) { Debug.LogError("[Presenter] payoutTable missing!"); return; }

            _model = new SlotGameModel(gameConfig.StartingBalance, gameConfig.BetAmounts, gameConfig.DefaultBetIndex);
            _rng = new SlotRngService();
            _evaluator = new PayoutEvaluator();

            reelsManager.Initialize(symbols, gameConfig, payoutTable);
            uiView?.Initialize(gameConfig);

            // Model → UI
            _model.OnBalanceChanged += b => uiView?.UpdateBalance(b);
            _model.OnBetChanged += b => uiView?.UpdateBet(b);
            _model.OnWinChanged += w => uiView?.UpdateWin(w);
            _model.OnFreeSpinsChanged += fs => uiView?.UpdateFreeSpins(fs);

            // UI → Presenter
            if (uiView != null)
            {
                uiView.OnSpinClicked += RequestSpin;
                uiView.OnBetPlusClicked += () => { audioService?.PlayButtonClick(); _model.CycleBet(1); };
                uiView.OnBetMinusClicked += () => { audioService?.PlayButtonClick(); _model.CycleBet(-1); };
                uiView.OnMaxBetClicked += () => { audioService?.PlayButtonClick(); _model.SetMaxBet(); };
                uiView.OnSoundToggled += () => { audioService?.ToggleMute(); uiView.UpdateSoundButton(audioService.IsMuted); };
                uiView.OnResetCreditsClicked += ResetCredits;
                uiView.OnQuickBetSelected += QuickBet;
            }
            if (machineView != null) machineView.OnLeverClicked += RequestSpin;

            // Reel events
            reelsManager.OnIndividualReelStopped += (idx, _) => audioService?.PlayReelStop(idx);
            reelsManager.OnAllReelsStopped += OnAllReelsStopped;

            // Initial UI
            uiView?.UpdateBalance(_model.Balance);
            uiView?.UpdateBet(_model.CurrentBet);
            uiView?.UpdateWin(0, false);
            uiView?.UpdateFreeSpins(0);
            uiView?.SetStatusMessage("PULL LEVER OR CLICK SPIN!");
            uiView?.SetControlsInteractable(true, true);
            if (audioService != null) uiView?.UpdateSoundButton(audioService.IsMuted);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) && !_isSpinning)
                RequestSpin();
        }

        private void OnDestroy()
        {
            if (reelsManager != null) reelsManager.OnAllReelsStopped -= OnAllReelsStopped;
            if (machineView != null) machineView.OnLeverClicked -= RequestSpin;
            if (uiView != null)
            {
                uiView.OnSpinClicked -= RequestSpin;
                uiView.OnQuickBetSelected -= QuickBet;
                uiView.OnResetCreditsClicked -= ResetCredits;
            }
        }

        private void QuickBet(int bet)
        {
            if (_isSpinning) return;
            _model.SetBetDirect(bet);
            RequestSpin();
        }

        private void RequestSpin()
        {
            if (_isSpinning) return;
            if (!_model.IsFreeSpinsActive && _model.Balance < _model.CurrentBet)
            {
                uiView?.SetStatusMessage("OUT OF CREDITS! CLICK RELOAD TO PLAY.");
                return;
            }

            _isSpinning = true;
            _model.SetState(SlotGameState.Spinning);
            _model.TryDeductBet();

            audioService?.PlayLeverPull();
            audioService?.PlaySpinLoop();
            machineView?.TriggerLeverPull();

            uiView?.UpdateWin(0, false);
            uiView?.SetControlsInteractable(false, false);
            string msg = _model.IsFreeSpinsActive
                ? $"FREE SPIN ({_model.FreeSpinsRemaining} LEFT) — 2x MULTIPLIER!"
                : "SPINNING... GOOD LUCK!";
            uiView?.SetStatusMessage(msg);

            var targets = _rng.GenerateSpinResult(reelsManager.Reels.Count, symbols);
            reelsManager.SpinAll(targets);
        }

        private void OnAllReelsStopped(SymbolType[] outcome)
        {
            audioService?.StopSpinLoop();
            _model.SetState(SlotGameState.Evaluating);

            var result = _evaluator.Evaluate(outcome, _model.CurrentBet, _model.IsFreeSpinsActive, payoutTable);

            if (!result.IsWin)
            {
                uiView?.SetStatusMessage(_model.IsFreeSpinsActive ? "NO WIN — ROLLING AGAIN..." : "NO WIN. TRY AGAIN!");
            }
            else
            {
                _model.AddPayout(result.PayoutCredits);
                uiView?.SetStatusMessage(result.WinDescription);
                uiView?.UpdateWin(result.PayoutCredits);

                // Highlight winning cells
                Color highlight = GetSymbolColor(result.WinningSymbol);
                reelsManager.HighlightWinningLine(highlight, gameConfig.WinHighlightDuration);
                audioService?.PlayWin(result.Tier);

                // Popup for big wins / jackpot / free spins
                if (result.IsJackpot || result.IsFreeSpinsTriggered || result.Tier == WinTier.BigWin)
                {
                    string title = result.IsJackpot ? "JACKPOT!" : result.IsFreeSpinsTriggered ? "BELL BONUS!" : "BIG WIN!";
                    string desc = result.IsFreeSpinsTriggered
                        ? $"3 BELLS! YOU WON {result.FreeSpinsAwarded} FREE SPINS WITH 2X PAYOUTS!"
                        : result.WinDescription;
                    uiView?.ShowWinPopup(title, result.PayoutCredits, desc);

                    if (result.IsFreeSpinsTriggered)
                        _model.AddFreeSpins(result.FreeSpinsAwarded);
                }
            }

            _isSpinning = false;

            if (_model.IsFreeSpinsActive)
            {
                _model.SetState(SlotGameState.FreeSpins);
                if (_autoSpinCoroutine != null) StopCoroutine(_autoSpinCoroutine);
                _autoSpinCoroutine = StartCoroutine(AutoFreeSpin());
            }
            else
            {
                _model.SetState(SlotGameState.Idle);
                uiView?.SetControlsInteractable(true, true);
            }
        }

        private IEnumerator AutoFreeSpin()
        {
            yield return new WaitForSeconds(gameConfig.FreeSpinDelay);
            RequestSpin();
        }

        private void ResetCredits()
        {
            audioService?.PlayButtonClick();
            _model.ResetBalance(gameConfig.StartingBalance);
            uiView?.SetStatusMessage("CREDITS RELOADED! READY TO PLAY.");
            uiView?.SetControlsInteractable(true, true);
        }

        private Color GetSymbolColor(SymbolType? symbolType)
        {
            if (!symbolType.HasValue) return Color.yellow;
            foreach (var s in symbols)
                if (s.SymbolType == symbolType.Value) return s.ThemeColor;
            return Color.yellow;
        }
    }
}
