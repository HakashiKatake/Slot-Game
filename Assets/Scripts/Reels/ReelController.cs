using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Core;

namespace SlotGame.Reels
{
    public enum ReelState
    {
        Idle,
        Anticipation,
        SpinningFast,
        Stopping,
        Bouncing
    }

    /// <summary>
    /// Controls a single physical reel column.
    /// Manages infinite symbol wrapping, smooth anticipation pull-back, high-speed spin,
    /// and realistic bounce-back deceleration settling precisely on the center payline.
    /// </summary>
    public class ReelController : MonoBehaviour
    {
        [Header("Reel Identification")]
        [SerializeField] private int reelIndex;

        [Header("Cell Views")]
        [Tooltip("Cells arranged vertically inside the reel. Typically 5 cells for seamless wrapping.")]
        [SerializeField] private List<SymbolCellView> cells = new List<SymbolCellView>();

        [Header("References")]
        [SerializeField] private RectTransform reelContainer;

        public int ReelIndex => reelIndex;
        public ReelState State { get; private set; } = ReelState.Idle;
        public SymbolType CenterSymbol => _centerCell != null ? _centerCell.CurrentSymbol : SymbolType.Seven;
        public SymbolCellView CenterCell => _centerCell;

        public event Action<ReelController, int, SymbolType> OnReelStopped;

        private List<SymbolDataSO> _availableSymbols;
        private SlotGameConfigSO _config;
        private Coroutine _spinCoroutine;
        private SymbolCellView _centerCell;

        private float _cellHeight = 140f;
        private float _halfHeightThreshold;
        private bool _stopRequested;
        private SymbolDataSO _targetSymbolData;
        private SymbolCellView _targetCell;

        public void Initialize(int index, List<SymbolDataSO> symbols, SlotGameConfigSO config)
        {
            reelIndex = index;
            _availableSymbols = symbols;
            _config = config;
            _cellHeight = config != null ? config.SymbolCellHeight : 140f;
            _halfHeightThreshold = _cellHeight * 2.5f;

            if (reelContainer == null)
            {
                reelContainer = GetComponent<RectTransform>();
            }

            SetupInitialLayout();
        }

        private void SetupInitialLayout()
        {
            if (cells == null || cells.Count == 0)
            {
                cells = new List<SymbolCellView>(GetComponentsInChildren<SymbolCellView>());
            }

            // Positions: [-2, -1, 0, 1, 2] * cellHeight
            int count = cells.Count;
            int mid = count / 2;

            for (int i = 0; i < count; i++)
            {
                int offsetFromMid = i - mid;
                float y = offsetFromMid * _cellHeight;
                cells[i].RectTransform.anchoredPosition = new Vector2(0f, y);

                if (i == mid)
                {
                    _centerCell = cells[i];
                }

                // Populate with diverse initial symbols
                if (_availableSymbols != null && _availableSymbols.Count > 0)
                {
                    var symbolData = _availableSymbols[(i + reelIndex) % _availableSymbols.Count];
                    cells[i].SetSymbol(symbolData);
                }
            }
        }

        public void Spin()
        {
            if (State != ReelState.Idle) return;

            _stopRequested = false;
            _targetCell = null;
            _targetSymbolData = null;

            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
            }
            _spinCoroutine = StartCoroutine(SpinRoutine());
        }

        public void StopAt(SymbolType target, SymbolDataSO targetData)
        {
            _targetSymbolData = targetData;
            _stopRequested = true;
        }

        private IEnumerator SpinRoutine()
        {
            // 1. Anticipation: Pull up slightly before rolling down
            State = ReelState.Anticipation;
            float antDist = _config != null ? _config.AnticipationDistance : 28f;
            float antDur = _config != null ? _config.AnticipationDuration : 0.18f;
            float elapsed = 0f;

            Vector2[] startPositions = new Vector2[cells.Count];
            for (int i = 0; i < cells.Count; i++)
            {
                startPositions[i] = cells[i].RectTransform.anchoredPosition;
            }

            while (elapsed < antDur)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / antDur);
                // Ease out sine
                float offset = Mathf.Sin(progress * Mathf.PI * 0.5f) * antDist;

                for (int i = 0; i < cells.Count; i++)
                {
                    cells[i].RectTransform.anchoredPosition = startPositions[i] + new Vector2(0f, offset);
                }
                yield return null;
            }

            // 2. High-speed continuous spin
            State = ReelState.SpinningFast;
            float maxSpeed = _config != null ? _config.SpinSpeed : 2200f;
            float currentSpeed = 0f;
            float accelRate = maxSpeed * 5f; // Reach max speed in ~0.2s

            while (!_stopRequested || _targetCell == null)
            {
                float dt = Time.deltaTime;
                currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, accelRate * dt);
                MoveAndWrapCells(currentSpeed * dt);
                yield return null;
            }

            // 3. Stopping phase: Decelerate target cell smoothly towards center (y = 0)
            State = ReelState.Stopping;
            float stopDuration = 0.32f;
            float stopElapsed = 0f;

            // Distance the target cell must travel to reach y = 0
            float initialTargetY = _targetCell.RectTransform.anchoredPosition.y;
            // Target cell will travel from initialTargetY to 0
            float totalTravelDistance = initialTargetY;

            while (stopElapsed < stopDuration)
            {
                stopElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(stopElapsed / stopDuration);
                // Ease out cubic
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);
                float currentTargetY = Mathf.Lerp(initialTargetY, 0f, easeOut);
                float deltaY = currentTargetY - _targetCell.RectTransform.anchoredPosition.y;

                ApplyDeltaToAllCells(deltaY);
                yield return null;
            }

            // 4. Juicy Bounce / Overshoot Phase
            State = ReelState.Bouncing;
            float bounceDist = _config != null ? _config.BounceOvershootDistance : 22f;
            float bounceDur = _config != null ? _config.BounceDuration : 0.28f;
            float bounceElapsed = 0f;

            while (bounceElapsed < bounceDur)
            {
                bounceElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(bounceElapsed / bounceDur);
                // Elastic damped sine: overshoot downwards then bounce back to 0
                float bounceOffset = -Mathf.Sin(t * Mathf.PI) * (1f - t) * bounceDist;
                float currentTargetY = bounceOffset;
                float deltaY = currentTargetY - _targetCell.RectTransform.anchoredPosition.y;

                ApplyDeltaToAllCells(deltaY);
                yield return null;
            }

            // 5. Final Snap & Settlement
            SnapCellsToGrid();
            _centerCell = _targetCell;
            State = ReelState.Idle;

            OnReelStopped?.Invoke(this, reelIndex, _centerCell.CurrentSymbol);
        }

        private void MoveAndWrapCells(float deltaY)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                var pos = cell.RectTransform.anchoredPosition;
                pos.y -= deltaY;

                // When cell falls below the bottom threshold, wrap it to the top
                if (pos.y < -_halfHeightThreshold)
                {
                    float highestY = GetHighestCellY();
                    pos.y = highestY + _cellHeight;

                    // If a stop was requested and we haven't designated a target cell yet,
                    // this cell wrapping into the top position becomes our landing target!
                    if (_stopRequested && _targetCell == null && _targetSymbolData != null)
                    {
                        _targetCell = cell;
                        cell.SetSymbol(_targetSymbolData);
                    }
                    else
                    {
                        // Randomize intermediate symbols for spinning blur
                        if (_availableSymbols != null && _availableSymbols.Count > 0)
                        {
                            int randIdx = UnityEngine.Random.Range(0, _availableSymbols.Count);
                            cell.SetSymbol(_availableSymbols[randIdx]);
                        }
                    }
                }

                cell.RectTransform.anchoredPosition = pos;
            }
        }

        private void ApplyDeltaToAllCells(float deltaY)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                var pos = cells[i].RectTransform.anchoredPosition;
                pos.y += deltaY;
                cells[i].RectTransform.anchoredPosition = pos;
            }
        }

        private float GetHighestCellY()
        {
            float maxY = float.MinValue;
            for (int i = 0; i < cells.Count; i++)
            {
                float y = cells[i].RectTransform.anchoredPosition.y;
                if (y > maxY) maxY = y;
            }
            return maxY;
        }

        private void SnapCellsToGrid()
        {
            if (_targetCell == null) return;

            // Target cell snaps exactly to y = 0
            _targetCell.RectTransform.anchoredPosition = new Vector2(0f, 0f);

            // Other cells snap relative to target cell
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] == _targetCell) continue;

                // Round to nearest multiple of _cellHeight
                float rawY = cells[i].RectTransform.anchoredPosition.y;
                float snappedY = Mathf.Round(rawY / _cellHeight) * _cellHeight;
                cells[i].RectTransform.anchoredPosition = new Vector2(0f, snappedY);
            }
        }

        public void HighlightWin(Color color, float duration)
        {
            if (_centerCell != null)
            {
                _centerCell.PlayWinHighlight(color, duration);
            }
        }

        public void StopHighlight()
        {
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].StopHighlight();
            }
        }
    }
}
