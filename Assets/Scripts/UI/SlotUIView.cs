using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SlotGame.Core;

namespace SlotGame.UI
{
    /// <summary>
    /// Manages the slot machine HUD, interactive buttons, counter rollups, and win popups.
    /// Follows MVP passive view pattern: exposes clean update methods and user action events.
    /// </summary>
    public class SlotUIView : MonoBehaviour
    {
        [Header("HUD Text Displays")]
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private TextMeshProUGUI winText;
        [SerializeField] private TextMeshProUGUI statusBannerText;

        [Header("Free Spins Indicator")]
        [SerializeField] private GameObject freeSpinsBanner;
        [SerializeField] private TextMeshProUGUI freeSpinsText;

        [Header("Primary Control Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button betMinusButton;
        [SerializeField] private Button betPlusButton;
        [SerializeField] private Button maxBetButton;

        [Header("Secondary / Utility Buttons")]
        [SerializeField] private Button paytableToggleButton;
        [SerializeField] private Button soundToggleButton;
        [SerializeField] private TextMeshProUGUI soundToggleText;
        [SerializeField] private Button resetCreditsButton;

        [Header("Win Celebration Popup")]
        [SerializeField] private GameObject winPopupPanel;
        [SerializeField] private TextMeshProUGUI winPopupTitle;
        [SerializeField] private TextMeshProUGUI winPopupAmount;
        [SerializeField] private TextMeshProUGUI winPopupDesc;
        [SerializeField] private Button winPopupCollectButton;

        [Header("Paytable Dialog")]
        [SerializeField] private GameObject paytablePanel;
        [SerializeField] private Button paytableCloseButton;

        public event Action OnSpinClicked;
        public event Action OnBetMinusClicked;
        public event Action OnBetPlusClicked;
        public event Action OnMaxBetClicked;
        public event Action OnPaytableToggled;
        public event Action OnSoundToggled;
        public event Action OnResetCreditsClicked;
        public event Action OnWinPopupClosed;

        private Coroutine _winTallyCoroutine;

        private void Awake()
        {
            if (spinButton != null) spinButton.onClick.AddListener(() => OnSpinClicked?.Invoke());
            if (betMinusButton != null) betMinusButton.onClick.AddListener(() => OnBetMinusClicked?.Invoke());
            if (betPlusButton != null) betPlusButton.onClick.AddListener(() => OnBetPlusClicked?.Invoke());
            if (maxBetButton != null) maxBetButton.onClick.AddListener(() => OnMaxBetClicked?.Invoke());
            if (paytableToggleButton != null) paytableToggleButton.onClick.AddListener(() => OnPaytableToggled?.Invoke());
            if (soundToggleButton != null) soundToggleButton.onClick.AddListener(() => OnSoundToggled?.Invoke());
            if (resetCreditsButton != null) resetCreditsButton.onClick.AddListener(() => OnResetCreditsClicked?.Invoke());
            if (winPopupCollectButton != null) winPopupCollectButton.onClick.AddListener(CloseWinPopup);
            if (paytableCloseButton != null) paytableCloseButton.onClick.AddListener(ClosePaytable);

            if (winPopupPanel != null) winPopupPanel.SetActive(false);
            if (paytablePanel != null) paytablePanel.SetActive(false);
            if (freeSpinsBanner != null) freeSpinsBanner.SetActive(false);
        }

        public void UpdateBalance(int balance)
        {
            if (balanceText != null)
            {
                balanceText.text = $"{balance:N0}";
            }
        }

        public void UpdateBet(int bet)
        {
            if (betText != null)
            {
                betText.text = $"{bet:N0}";
            }
        }

        public void UpdateWin(int win, bool animated = true)
        {
            if (winText == null) return;

            if (!animated || win == 0)
            {
                if (_winTallyCoroutine != null) StopCoroutine(_winTallyCoroutine);
                winText.text = $"{win:N0}";
                return;
            }

            if (_winTallyCoroutine != null) StopCoroutine(_winTallyCoroutine);
            _winTallyCoroutine = StartCoroutine(TallyScoreRoutine(win));
        }

        private IEnumerator TallyScoreRoutine(int targetWin)
        {
            float duration = 0.6f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                int current = (int)Mathf.Lerp(0, targetWin, elapsed / duration);
                winText.text = $"{current:N0}";
                yield return null;
            }
            winText.text = $"{targetWin:N0}";
        }

        public void SetStatusMessage(string message)
        {
            if (statusBannerText != null)
            {
                statusBannerText.text = message;
            }
        }

        public void SetControlsInteractable(bool canSpin, bool canChangeBet)
        {
            if (spinButton != null) spinButton.interactable = canSpin;
            if (betMinusButton != null) betMinusButton.interactable = canChangeBet;
            if (betPlusButton != null) betPlusButton.interactable = canChangeBet;
            if (maxBetButton != null) maxBetButton.interactable = canChangeBet;
        }

        public void UpdateFreeSpins(int remainingSpins)
        {
            if (freeSpinsBanner != null)
            {
                freeSpinsBanner.SetActive(remainingSpins > 0);
            }
            if (freeSpinsText != null)
            {
                freeSpinsText.text = $"FREE SPINS: {remainingSpins} (2x WIN MULTIPLIER!)";
            }
        }

        public void ShowWinPopup(string title, int winAmount, string description)
        {
            if (winPopupPanel == null) return;

            if (winPopupTitle != null) winPopupTitle.text = title;
            if (winPopupAmount != null) winPopupAmount.text = $"+{winAmount:N0} CREDITS";
            if (winPopupDesc != null) winPopupDesc.text = description;

            winPopupPanel.SetActive(true);
        }

        public void CloseWinPopup()
        {
            if (winPopupPanel != null)
            {
                winPopupPanel.SetActive(false);
            }
            OnWinPopupClosed?.Invoke();
        }

        public void TogglePaytable()
        {
            if (paytablePanel != null)
            {
                paytablePanel.SetActive(!paytablePanel.activeSelf);
            }
        }

        public void ClosePaytable()
        {
            if (paytablePanel != null)
            {
                paytablePanel.SetActive(false);
            }
        }

        public void UpdateSoundButton(bool isMuted)
        {
            if (soundToggleText != null)
            {
                soundToggleText.text = isMuted ? "SOUND: OFF" : "SOUND: ON";
            }
        }
    }
}
