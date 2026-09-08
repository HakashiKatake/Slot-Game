using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Core;

namespace SlotGame.Reels
{
    /// <summary>
    /// Coordinates all reel columns in the slot machine.
    /// Manages staggered stop sequencing, suspense delays on potential jackpots,
    /// and dispatches reel stop and completion events.
    /// </summary>
    public class ReelsManager : MonoBehaviour
    {
        [Header("Reel Columns (Left to Right)")]
        [SerializeField] private List<ReelController> reels = new List<ReelController>();

        public IReadOnlyList<ReelController> Reels => reels;
        public bool IsAnyReelSpinning
        {
            get
            {
                for (int i = 0; i < reels.Count; i++)
                {
                    if (reels[i].State != ReelState.Idle) return true;
                }
                return false;
            }
        }

        public event Action<int, SymbolType> OnIndividualReelStopped;
        public event Action<SymbolType[]> OnAllReelsStopped;

        private SlotGameConfigSO _config;
        private Dictionary<SymbolType, SymbolDataSO> _symbolLookup;
        private SymbolType[] _currentTargets;
        private int _stoppedReelCount;
        private Coroutine _staggerCoroutine;

        public void Initialize(
            List<SymbolDataSO> symbols,
            SlotGameConfigSO config)
        {
            _config = config;
            _symbolLookup = new Dictionary<SymbolType, SymbolDataSO>();
            for (int i = 0; i < symbols.Count; i++)
            {
                _symbolLookup[symbols[i].SymbolType] = symbols[i];
            }

            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].Initialize(i, symbols, config);
                reels[i].OnReelStopped += HandleReelStopped;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].OnReelStopped -= HandleReelStopped;
                }
            }
        }

        public void SpinAll(SymbolType[] targets)
        {
            if (IsAnyReelSpinning || targets == null || targets.Length != reels.Count) return;

            StopHighlights();
            _currentTargets = targets;
            _stoppedReelCount = 0;

            // Start all reels spinning
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].Spin();
            }

            if (_staggerCoroutine != null)
            {
                StopCoroutine(_staggerCoroutine);
            }
            _staggerCoroutine = StartCoroutine(StaggeredStopRoutine());
        }

        private IEnumerator StaggeredStopRoutine()
        {
            float baseDuration = _config != null ? _config.BaseSpinDuration : 1.0f;
            float stagger = _config != null ? _config.ReelStaggerDelay : 0.35f;
            float suspense = _config != null ? _config.SuspenseDelay : 0.7f;

            // 1. Wait for base spin duration
            yield return new WaitForSeconds(baseDuration);

            // 2. Stop Reel 0
            StopReel(0);

            // 3. Wait stagger delay, then stop Reel 1
            yield return new WaitForSeconds(stagger);
            StopReel(1);

            // 4. Check for suspense on Reel 2: If reel 0 and reel 1 match Seven or Bell
            float lastReelDelay = stagger;
            if (_currentTargets.Length >= 2 && _currentTargets[0] == _currentTargets[1])
            {
                if (_currentTargets[0] == SymbolType.Seven || _currentTargets[0] == SymbolType.Bell)
                {
                    lastReelDelay += suspense;
                }
            }

            yield return new WaitForSeconds(lastReelDelay);
            StopReel(2);
        }

        private void StopReel(int index)
        {
            if (index < 0 || index >= reels.Count) return;

            var targetType = _currentTargets[index];
            _symbolLookup.TryGetValue(targetType, out var data);
            reels[index].StopAt(targetType, data);
        }

        private void HandleReelStopped(ReelController reel, int index, SymbolType landedSymbol)
        {
            OnIndividualReelStopped?.Invoke(index, landedSymbol);
            _stoppedReelCount++;

            if (_stoppedReelCount >= reels.Count)
            {
                var finalOutcome = new SymbolType[reels.Count];
                for (int i = 0; i < reels.Count; i++)
                {
                    finalOutcome[i] = reels[i].CenterSymbol;
                }
                OnAllReelsStopped?.Invoke(finalOutcome);
            }
        }

        public void HighlightWinningLine(Color color, float duration)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].HighlightWin(color, duration);
            }
        }

        public void StopHighlights()
        {
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].StopHighlight();
            }
        }
    }
}
