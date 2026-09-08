using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Core;

namespace SlotGame.Reels
{
    /// <summary>
    /// Coordinates all reel columns in the slot machine.
    /// Manages staggered stops, anticipation/suspense delays, and winning payline highlights.
    /// </summary>
    public class ReelsManager : MonoBehaviour
    {
        [SerializeField] private List<ReelController> reels = new();

        public IReadOnlyList<ReelController> Reels => reels;
        public bool IsSpinning
        {
            get
            {
                foreach (var r in reels)
                    if (r != null && r.IsSpinning) return true;
                return false;
            }
        }

        public event Action<int, SymbolType> OnIndividualReelStopped;
        public event Action<SymbolType[]> OnAllReelsStopped;

        private SlotGameConfigSO _config;
        private PayoutTableSO _payoutTable;
        private Dictionary<SymbolType, SymbolDataSO> _symbolMap;
        private SymbolType[] _targetSymbols;
        private int _stoppedCount;

        public void Initialize(List<SymbolDataSO> symbols, SlotGameConfigSO config, PayoutTableSO payoutTable)
        {
            _config = config;
            _payoutTable = payoutTable;
            _symbolMap = new Dictionary<SymbolType, SymbolDataSO>();

            if (symbols != null)
            {
                foreach (var s in symbols)
                    if (s != null) _symbolMap[s.SymbolType] = s;
            }

            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] == null) continue;
                reels[i].Initialize(i, symbols, config);
                reels[i].OnReelStopped += HandleReelStopped;
            }
        }

        private void OnDestroy()
        {
            foreach (var r in reels)
                if (r != null) r.OnReelStopped -= HandleReelStopped;
        }

        /// <summary>
        /// Spins all reels and schedules their staggered stops based on the target outcome.
        /// </summary>
        public void SpinAll(SymbolType[] targets)
        {
            if (IsSpinning || targets == null || targets.Length != reels.Count) return;

            StopHighlights();
            _targetSymbols = targets;
            _stoppedCount = 0;

            foreach (var r in reels)
                r.Spin();

            StartCoroutine(StaggeredStopRoutine());
        }

        public void HighlightWinningLine(Color color, float duration)
        {
            foreach (var r in reels)
                r.HighlightWin(color, duration);
        }

        public void StopHighlights()
        {
            foreach (var r in reels)
                r.StopHighlight();
        }

        // ── Staggered Stopping Sequence ───────────────────────────

        private IEnumerator StaggeredStopRoutine()
        {
            float baseDuration = _config != null ? _config.BaseSpinDuration : 1.0f;
            float staggerDelay = _config != null ? _config.ReelStaggerDelay : 0.35f;
            float suspenseDelay = _config != null ? _config.SuspenseDelay : 0.7f;

            // Stop Reel 0
            yield return new WaitForSeconds(baseDuration);
            StopReel(0);

            // Stop Reel 1
            yield return new WaitForSeconds(staggerDelay);
            StopReel(1);

            // If Reel 0 and Reel 1 matched on a high-value symbol, add suspense before Reel 2 stops!
            bool triggersSuspense = _targetSymbols.Length >= 3
                && _targetSymbols[0] == _targetSymbols[1]
                && _payoutTable != null
                && _payoutTable.ShouldTriggerSuspense(_targetSymbols[0]);

            yield return new WaitForSeconds(staggerDelay + (triggersSuspense ? suspenseDelay : 0f));
            StopReel(2);
        }

        private void StopReel(int index)
        {
            if (index < 0 || index >= reels.Count) return;

            SymbolType target = _targetSymbols[index];
            _symbolMap.TryGetValue(target, out var data);
            reels[index].StopAt(target, data);
        }

        private void HandleReelStopped(ReelController reel, int index, SymbolType symbol)
        {
            OnIndividualReelStopped?.Invoke(index, symbol);
            _stoppedCount++;

            if (_stoppedCount >= reels.Count)
            {
                var finalOutcome = new SymbolType[reels.Count];
                for (int i = 0; i < reels.Count; i++)
                    finalOutcome[i] = reels[i].CenterSymbol;

                OnAllReelsStopped?.Invoke(finalOutcome);
            }
        }
    }
}
