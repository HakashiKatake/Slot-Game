using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Core;

namespace SlotGame.Reels
{
    /// <summary>
    /// Controls a single vertical slot reel: infinite scrolling, symbol wrapping,
    /// and smooth deceleration to land the target symbol dead-center.
    /// </summary>
    public class ReelController : MonoBehaviour
    {
        [SerializeField] private int reelIndex;
        [SerializeField] private List<SymbolCellView> cells = new();

        public int ReelIndex => reelIndex;
        public bool IsSpinning { get; private set; }
        public SymbolType CenterSymbol => _centerCell != null ? _centerCell.CurrentSymbol : SymbolType.Seven;

        public event Action<ReelController, int, SymbolType> OnReelStopped;

        private SlotGameConfigSO _config;
        private List<SymbolDataSO> _symbols;
        private SymbolCellView _targetCell;
        private SymbolCellView _centerCell;
        private SymbolDataSO _targetData;
        private bool _stopRequested;
        private float _cellHeight = 80f;

        public void Initialize(int index, List<SymbolDataSO> symbols, SlotGameConfigSO config)
        {
            reelIndex = index;
            _symbols = symbols;
            _config = config;
            _cellHeight = config != null ? config.SymbolCellHeight : 80f;

            if (cells.Count == 0)
                cells.AddRange(GetComponentsInChildren<SymbolCellView>());

            // Position cells vertically: ... -80, 0 (center), +80 ...
            int mid = cells.Count / 2;
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].RectTransform.anchoredPosition = new Vector2(0f, (i - mid) * _cellHeight);
                if (i == mid) _centerCell = cells[i];
                if (_symbols?.Count > 0)
                    cells[i].SetSymbol(_symbols[(i + reelIndex) % _symbols.Count]);
            }
        }

        public void Spin()
        {
            if (IsSpinning) return;
            _stopRequested = false;
            _targetCell = null;
            _targetData = null;
            IsSpinning = true;
            StartCoroutine(SpinRoutine());
        }

        public void StopAt(SymbolType symbol, SymbolDataSO data)
        {
            _targetData = data;
            _stopRequested = true;
        }

        public void HighlightWin(Color color, float duration) => _centerCell?.PlayWinHighlight(color, duration);
        public void StopHighlight() => cells.ForEach(c => c.StopHighlight());

        // ── Spin Animation ────────────────────────────────────────

        private IEnumerator SpinRoutine()
        {
            float speed = _config != null ? _config.SpinSpeed : 1600f;
            float wrapLimit = _cellHeight * 2.5f;

            // 1. Full speed spin until stop is requested and a target cell wraps into position
            while (!_stopRequested || _targetCell == null)
            {
                ScrollCells(speed * Time.deltaTime, wrapLimit);
                yield return null;
            }

            // 2. Smooth deceleration: target cell glides from its current position to y = 0
            float startY = _targetCell.RectTransform.anchoredPosition.y;
            float stopDuration = _config != null ? _config.StopDuration : 0.35f;
            float elapsed = 0f;

            while (elapsed < stopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / stopDuration);
                float easeOut = 1f - (1f - t) * (1f - t) * (1f - t); // Cubic ease out
                float desiredY = Mathf.Lerp(startY, 0f, easeOut);
                ShiftCells(desiredY - _targetCell.RectTransform.anchoredPosition.y);
                yield return null;
            }

            // 3. Align all cells symmetrically around center cell (y = 0)
            AlignGrid();
            _centerCell = _targetCell;
            IsSpinning = false;

            OnReelStopped?.Invoke(this, reelIndex, _centerCell.CurrentSymbol);
        }

        private void ScrollCells(float distance, float wrapLimit)
        {
            float topY = float.MinValue;
            for (int i = 0; i < cells.Count; i++)
            {
                float y = cells[i].RectTransform.anchoredPosition.y;
                if (y > topY) topY = y;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                var pos = cells[i].RectTransform.anchoredPosition;
                pos.y -= distance;

                if (pos.y < -wrapLimit)
                {
                    pos.y = topY + _cellHeight;
                    topY = pos.y;

                    if (_stopRequested && _targetCell == null && _targetData != null)
                    {
                        _targetCell = cells[i];
                        cells[i].SetSymbol(_targetData);
                    }
                    else if (_symbols?.Count > 0)
                    {
                        cells[i].SetSymbol(_symbols[UnityEngine.Random.Range(0, _symbols.Count)]);
                    }
                }

                cells[i].RectTransform.anchoredPosition = pos;
            }
        }

        private void ShiftCells(float deltaY)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                var pos = cells[i].RectTransform.anchoredPosition;
                pos.y += deltaY;
                cells[i].RectTransform.anchoredPosition = pos;
            }
        }

        private void AlignGrid()
        {
            if (_targetCell == null) return;
            _targetCell.RectTransform.anchoredPosition = Vector2.zero;

            int targetIndex = cells.IndexOf(_targetCell);
            int count = cells.Count;
            int mid = count / 2;

            for (int i = 0; i < count; i++)
            {
                int offset = i - targetIndex;
                while (offset > mid) offset -= count;
                while (offset < -mid) offset += count;
                cells[i].RectTransform.anchoredPosition = new Vector2(0f, offset * _cellHeight);
            }
        }
    }
}
