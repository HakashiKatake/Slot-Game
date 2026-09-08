using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using SlotGame.Core;
using SlotGame.Reels;
using SlotGame.Audio;
using SlotGame.Services;

namespace SlotGame.Controllers
{
    /// <summary>
    /// Main controller for the slot machine game.
    /// Connects user input (lever & quick bet buttons) to reel spinning,
    /// evaluates outcomes via payout tables, and updates HUD / popup displays.
    /// </summary>
    public class SlotMachinePresenter : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SlotGameConfigSO gameConfig;
        [SerializeField] private PayoutTableSO payoutTable;
        [SerializeField] private List<SymbolDataSO> symbols = new();

        [Header("Settings")]
        [SerializeField] private int startingBalance = 1000;
        [SerializeField] private int defaultBet = 50;

        [Header("Reels")]
        [SerializeField] private ReelsManager reelsManager;

        [Header("Lever")]
        [SerializeField] private Button leverButton;
        [SerializeField] private Image leverImage;
        [SerializeField] private Sprite leverUpSprite;
        [SerializeField] private Sprite leverDownSprite;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI winText;

        [Header("Quick Bet Menu")]
        [SerializeField] private GameObject quickBetPanel;
        [SerializeField] private Button quickBet10Btn;
        [SerializeField] private Button quickBet50Btn;
        [SerializeField] private Button quickBet100Btn;
        [SerializeField] private Button quickBetExitBtn;

        [Header("Rules Popup")]
        [SerializeField] private GameObject rulesPopupPanel;
        [SerializeField] private Button rulesCloseBtn;

        [Header("Win Popup")]
        [SerializeField] private GameObject winPopupPanel;
        [SerializeField] private TextMeshProUGUI winPopupTitle;
        [SerializeField] private TextMeshProUGUI winPopupAmount;
        [SerializeField] private TextMeshProUGUI winPopupDesc;
        [SerializeField] private Button winPopupCollectBtn;

        [Header("Audio")]
        [SerializeField] private SlotAudioService audioService;

        // State
        private int _balance;
        private int _lastBet;
        private int _currentBet;
        private int _freeSpinsRemaining;
        private bool _isSpinning;
        private bool _isLeverPulling;
        private SlotRngService _rng;
        private PayoutEvaluator _evaluator;

        public int Balance => _balance;
        public int LastBet => _lastBet;
        public bool IsSpinning => _isSpinning;

        private void Awake()
        {
            Application.runInBackground = true;
            _balance = startingBalance;
            _lastBet = defaultBet;
            _rng = new SlotRngService();
            _evaluator = new PayoutEvaluator();

            // Setup button clicks
            if (leverButton != null)
                leverButton.onClick.AddListener(OnLeverClicked);

            if (quickBet10Btn != null)
                quickBet10Btn.onClick.AddListener(() => OnQuickBetClicked(10));

            if (quickBet50Btn != null)
                quickBet50Btn.onClick.AddListener(() => OnQuickBetClicked(50));

            if (quickBet100Btn != null)
                quickBet100Btn.onClick.AddListener(() => OnQuickBetClicked(100));

            if (quickBetExitBtn != null)
                quickBetExitBtn.onClick.AddListener(ToggleQuickBetMenu);

            if (rulesCloseBtn != null)
                rulesCloseBtn.onClick.AddListener(() => SetPopupActive(rulesPopupPanel, false));

            if (winPopupCollectBtn != null)
                winPopupCollectBtn.onClick.AddListener(() => SetPopupActive(winPopupPanel, false));

            if (reelsManager != null)
                reelsManager.OnAllReelsStopped += OnAllReelsStopped;
        }

        private void Start()
        {
            if (reelsManager != null && gameConfig != null && payoutTable != null)
                reelsManager.Initialize(symbols, gameConfig, payoutTable);

            UpdateBalanceUI();
            SetStatus("PULL LEVER OR SELECT BET!");

            if (winText != null) winText.gameObject.SetActive(false);
            if (quickBetPanel != null) quickBetPanel.SetActive(true);
            if (rulesPopupPanel != null) rulesPopupPanel.SetActive(true);
            if (winPopupPanel != null) winPopupPanel.SetActive(false);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (!_isSpinning && kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                OnLeverClicked();
        }

        private void OnDestroy()
        {
            if (reelsManager != null)
                reelsManager.OnAllReelsStopped -= OnAllReelsStopped;
        }

        // ── Actions ───────────────────────────────────────────────

        /// <summary>
        /// Pulls the lever and bets the last betted amount (default 50G).
        /// </summary>
        public void OnLeverClicked()
        {
            if (_isSpinning) return;
            if (rulesPopupPanel != null && rulesPopupPanel.activeSelf) return;

            StartSpin(_lastBet);
        }

        /// <summary>
        /// Selects a bet from the Quick Bet menu, updates last bet, and spins.
        /// </summary>
        public void OnQuickBetClicked(int betAmount)
        {
            if (_isSpinning) return;
            if (rulesPopupPanel != null && rulesPopupPanel.activeSelf) return;

            _lastBet = betAmount;
            StartSpin(_lastBet);
        }

        public void ToggleQuickBetMenu()
        {
            if (quickBetPanel != null)
                quickBetPanel.SetActive(!quickBetPanel.activeSelf);
        }

        public void CloseRulesPopup() => SetPopupActive(rulesPopupPanel, false);
        public void CloseWinPopup() => SetPopupActive(winPopupPanel, false);

        // ── Spin Flow ─────────────────────────────────────────────

        private void StartSpin(int bet)
        {
            if (_isSpinning) return;

            bool isFreeSpin = _freeSpinsRemaining > 0;
            if (!isFreeSpin && _balance < bet)
            {
                SetStatus("OUT OF CREDITS! CLICK RELOAD.");
                return;
            }

            _isSpinning = true;
            _currentBet = bet;

            if (isFreeSpin)
                _freeSpinsRemaining--;
            else
            {
                _balance -= _currentBet;
                UpdateBalanceUI();
            }

            StartCoroutine(LeverPullRoutine());
            if (audioService != null)
            {
                audioService.PlayLeverPull();
                audioService.PlaySpinLoop();
            }

            if (winText != null) winText.gameObject.SetActive(false);

            SetStatus(isFreeSpin
                ? $"FREE SPIN ({_freeSpinsRemaining} LEFT) - 2X MULTIPLIER!"
                : "SPINNING... GOOD LUCK!");

            if (reelsManager != null)
            {
                var outcome = _rng.GenerateSpinResult(reelsManager.Reels.Count, symbols);
                reelsManager.SpinAll(outcome);
            }
        }

        private void OnAllReelsStopped(SymbolType[] outcome)
        {
            if (audioService != null)
                audioService.StopSpinLoop();

            bool wasFreeSpin = _freeSpinsRemaining > 0;
            var result = _evaluator.Evaluate(outcome, _currentBet, wasFreeSpin, payoutTable);

            if (result.IsWin)
            {
                _balance += result.PayoutCredits;
                UpdateBalanceUI();
                SetStatus(result.WinDescription);

                if (winText != null)
                {
                    winText.text = $"+{result.PayoutCredits:N0}";
                    winText.gameObject.SetActive(true);
                }

                if (reelsManager != null)
                {
                    float duration = gameConfig != null ? gameConfig.WinHighlightDuration : 1.2f;
                    reelsManager.HighlightWinningLine(GetSymbolColor(result.WinningSymbol), duration);
                }

                if (audioService != null)
                    audioService.PlayWin(result.Tier);

                if (result.IsJackpot || result.IsFreeSpinsTriggered || result.Tier == WinTier.BigWin)
                {
                    ShowWinPopup(result);
                    if (result.IsFreeSpinsTriggered)
                        _freeSpinsRemaining += result.FreeSpinsAwarded;
                }
            }
            else
            {
                SetStatus(wasFreeSpin ? "NO WIN - ROLLING AGAIN..." : "NO WIN. TRY AGAIN!");
            }

            _isSpinning = false;

            if (_freeSpinsRemaining > 0)
                StartCoroutine(AutoFreeSpinRoutine());
        }

        private IEnumerator AutoFreeSpinRoutine()
        {
            float delay = gameConfig != null ? gameConfig.FreeSpinDelay : 1.0f;
            yield return new WaitForSeconds(delay);
            StartSpin(_currentBet);
        }

        private IEnumerator LeverPullRoutine()
        {
            if (_isLeverPulling) yield break;
            _isLeverPulling = true;

            if (leverImage != null && leverDownSprite != null)
                leverImage.sprite = leverDownSprite;

            yield return new WaitForSeconds(0.14f);

            if (leverImage != null && leverUpSprite != null)
                leverImage.sprite = leverUpSprite;

            yield return new WaitForSeconds(0.08f);
            _isLeverPulling = false;
        }

        // ── Helpers ───────────────────────────────────────────────

        private void UpdateBalanceUI()
        {
            if (balanceText != null)
                balanceText.text = $"{_balance:N0}G";
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void SetPopupActive(GameObject popup, bool active)
        {
            if (popup != null) popup.SetActive(active);
        }

        private void ShowWinPopup(PayoutResult result)
        {
            if (winPopupPanel == null) return;

            string title = result.IsJackpot ? "JACKPOT!" : result.IsFreeSpinsTriggered ? "BELL BONUS!" : "BIG WIN!";
            string desc = result.IsFreeSpinsTriggered
                ? $"3 BELLS! {result.FreeSpinsAwarded} FREE SPINS WITH 2X PAYOUTS!"
                : result.WinDescription;

            if (winPopupTitle != null) winPopupTitle.text = title;
            if (winPopupAmount != null) winPopupAmount.text = $"+{result.PayoutCredits:N0} CREDITS";
            if (winPopupDesc != null) winPopupDesc.text = desc;

            winPopupPanel.SetActive(true);
        }

        private Color GetSymbolColor(SymbolType? type)
        {
            if (!type.HasValue) return Color.yellow;
            foreach (var s in symbols)
                if (s != null && s.SymbolType == type.Value) return s.ThemeColor;
            return Color.yellow;
        }
    }
}
