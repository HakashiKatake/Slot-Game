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

            if (leverDownSprite != null && leverImage != null)
            {
                leverImage.sprite = leverDownSprite;
            }

            // Down state duration
            yield return new WaitForSeconds(0.14f);

            if (leverUpSprite != null && leverImage != null)
            {
                leverImage.sprite = leverUpSprite;
            }

            yield return new WaitForSeconds(0.08f);
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
