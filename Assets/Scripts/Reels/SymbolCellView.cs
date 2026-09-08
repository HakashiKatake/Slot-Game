using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SlotGame.Core;

namespace SlotGame.Reels
{
    /// <summary>
    /// Represents an individual symbol item inside a reel column.
    /// Manages the visual display, sprite assignment, and win highlight animation.
    /// </summary>
    public class SymbolCellView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image symbolImage;
        [SerializeField] private RectTransform rectTransform;

        public SymbolType CurrentSymbol { get; private set; }
        public RectTransform RectTransform => rectTransform != null ? rectTransform : (rectTransform = GetComponent<RectTransform>());

        private Coroutine _highlightCoroutine;
        private Vector3 _originalScale = Vector3.one;

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            if (symbolImage == null)
            {
                symbolImage = GetComponentInChildren<Image>();
            }
            _originalScale = RectTransform.localScale;
        }

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

        public void SetSymbolDirect(SymbolType type, Sprite sprite)
        {
            CurrentSymbol = type;
            if (symbolImage != null)
            {
                symbolImage.sprite = sprite;
                symbolImage.enabled = true;
                symbolImage.color = Color.white;
            }
        }

        public void PlayWinHighlight(Color highlightColor, float duration = 1.2f)
        {
            StopHighlight();
            _highlightCoroutine = StartCoroutine(HighlightRoutine(highlightColor, duration));
        }

        public void StopHighlight()
        {
            if (_highlightCoroutine != null)
            {
                StopCoroutine(_highlightCoroutine);
                _highlightCoroutine = null;
            }
            if (symbolImage != null)
            {
                symbolImage.color = Color.white;
            }
            if (RectTransform != null)
            {
                RectTransform.localScale = _originalScale;
            }
        }

        private IEnumerator HighlightRoutine(Color highlightColor, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Pulsing bounce scale and color flash
                float pulse = Mathf.PingPong(elapsed * 6f, 1f);
                if (RectTransform != null)
                {
                    RectTransform.localScale = _originalScale * (1f + 0.18f * pulse);
                }
                if (symbolImage != null)
                {
                    symbolImage.color = Color.Lerp(Color.white, highlightColor, pulse);
                }
                yield return null;
            }

            StopHighlight();
        }
    }
}
