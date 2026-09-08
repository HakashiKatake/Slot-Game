using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SlotGame.Core;

namespace SlotGame.Reels
{
    /// <summary>
    /// Displays a single symbol sprite within a reel column.
    /// Handles visual updates and win celebration animations.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SymbolCellView : MonoBehaviour
    {
        [SerializeField] private Image symbolImage;
        [SerializeField] private RectTransform rectTransform;

        public SymbolType CurrentSymbol { get; private set; }
        public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());

        private Coroutine _highlightCoroutine;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (symbolImage == null)
                symbolImage = GetComponentInChildren<Image>();

            _baseScale = RectTransform.localScale;
        }

        /// <summary>
        /// Sets the active symbol and updates the display sprite.
        /// </summary>
        public void SetSymbol(SymbolDataSO data)
        {
            if (data == null) return;

            CurrentSymbol = data.SymbolType;
            if (symbolImage != null)
            {
                symbolImage.sprite = data.SymbolSprite;
                symbolImage.enabled = true;
                symbolImage.color = Color.white;
            }
        }

        /// <summary>
        /// Plays a pulsing color and scale animation when this symbol is part of a win.
        /// </summary>
        public void PlayWinHighlight(Color color, float duration = 1.2f)
        {
            StopHighlight();
            _highlightCoroutine = StartCoroutine(HighlightRoutine(color, duration));
        }

        /// <summary>
        /// Resets the cell scale and color back to standard idle state.
        /// </summary>
        public void StopHighlight()
        {
            if (_highlightCoroutine != null)
            {
                StopCoroutine(_highlightCoroutine);
                _highlightCoroutine = null;
            }

            if (symbolImage != null)
                symbolImage.color = Color.white;

            if (RectTransform != null)
                RectTransform.localScale = _baseScale;
        }

        private IEnumerator HighlightRoutine(Color color, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float pulse = Mathf.PingPong(elapsed * 5f, 1f);

                if (RectTransform != null)
                    RectTransform.localScale = _baseScale * (1f + 0.15f * pulse);

                if (symbolImage != null)
                    symbolImage.color = Color.Lerp(Color.white, color, pulse);

                yield return null;
            }

            StopHighlight();
        }
    }
}
