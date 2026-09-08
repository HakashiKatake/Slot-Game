using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SlotGame.UI
{
    /// <summary>
    /// Controls the physical arcade slot cabinet, interactive lever pull animation,
    /// and payline visual indicators.
    /// </summary>
    public class SlotMachineView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Cabinet Frame")]
        [SerializeField] private Image cabinetImage;
        [SerializeField] private Image middleBoxImage;

        [Header("Interactive Lever")]
        [SerializeField] private RectTransform leverTransform;
        [SerializeField] private Image leverImage;
        [SerializeField] private Sprite leverUpSprite;
        [SerializeField] private Sprite leverDownSprite;

        [Header("Payline Guide")]
        [SerializeField] private Image paylineGuide;

        public event Action OnLeverClicked;

        private Coroutine _leverAnimationCoroutine;
        private Vector2 _leverOriginalPos;
        private bool _isLeverAnimating;

        private void Awake()
        {
            if (leverTransform != null)
            {
                _leverOriginalPos = leverTransform.anchoredPosition;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // If the user clicks anywhere on the lever area
            if (!_isLeverAnimating)
            {
                OnLeverClicked?.Invoke();
            }
        }

        public void TriggerLeverPull()
        {
            if (!_isLeverAnimating)
            {
                if (_leverAnimationCoroutine != null)
                {
                    StopCoroutine(_leverAnimationCoroutine);
                }
                _leverAnimationCoroutine = StartCoroutine(LeverPullRoutine());
            }
        }

        private IEnumerator LeverPullRoutine()
        {
            _isLeverAnimating = true;

            // 1. Pull down: quick smooth drop
            float pullDownDuration = 0.12f;
            float elapsed = 0f;
            Vector2 downPos = _leverOriginalPos + new Vector2(0f, -80f);

            if (leverDownSprite != null && leverImage != null)
            {
                leverImage.sprite = leverDownSprite;
            }

            while (elapsed < pullDownDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / pullDownDuration);
                if (leverTransform != null)
                {
                    leverTransform.anchoredPosition = Vector2.Lerp(_leverOriginalPos, downPos, t);
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.06f);

            // 2. Spring back up: elastic rebound
            float springUpDuration = 0.22f;
            elapsed = 0f;

            if (leverUpSprite != null && leverImage != null)
            {
                leverImage.sprite = leverUpSprite;
            }

            while (elapsed < springUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / springUpDuration);
                // Overshoot bounce back to top
                float overshoot = Mathf.Sin(t * Mathf.PI * 1.5f) * (1f - t) * 15f;
                if (leverTransform != null)
                {
                    leverTransform.anchoredPosition = Vector2.Lerp(downPos, _leverOriginalPos, t) + new Vector2(0f, overshoot);
                }
                yield return null;
            }

            if (leverTransform != null)
            {
                leverTransform.anchoredPosition = _leverOriginalPos;
            }

            _isLeverAnimating = false;
        }

        public void SetPaylineActive(bool active)
        {
            if (paylineGuide != null)
            {
                paylineGuide.gameObject.SetActive(active);
            }
        }
    }
}
