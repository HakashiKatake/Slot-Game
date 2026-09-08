using UnityEngine;

namespace SlotGame.Core
{
    /// <summary>
    /// Global game settings, timing parameters, and betting configuration.
    /// ScriptableObject separating game tuning from runtime code.
    /// </summary>
    [CreateAssetMenu(fileName = "SlotGameConfig", menuName = "SlotGame/Game Config")]
    public class SlotGameConfigSO : ScriptableObject
    {
        [Header("Economy & Betting")]
        [SerializeField] private int startingBalance = 1000;
        [SerializeField] private int[] betAmounts = new int[] { 10, 20, 50, 100 };
        [SerializeField] private int defaultBetIndex = 0;

        [Header("Reel Animation & Timings")]
        [Tooltip("Minimum spin time before the first reel begins deceleration (seconds)")]
        [SerializeField] private float baseSpinDuration = 1.0f;

        [Tooltip("Delay between each reel stopping in sequence (seconds)")]
        [SerializeField] private float reelStaggerDelay = 0.35f;

        [Tooltip("Additional dramatic pause on reel 3 if reels 1 & 2 have matched high symbols")]
        [SerializeField] private float suspenseDelay = 0.7f;

        [Tooltip("Reel scroll speed during full spin (units/sec)")]
        [SerializeField] private float spinSpeed = 2200f;

        [Tooltip("Distance in units between symbol cell centers")]
        [SerializeField] private float symbolCellHeight = 140f;

        [Tooltip("Anticipation nudge distance upward before rolling down")]
        [SerializeField] private float anticipationDistance = 28f;

        [Tooltip("Duration of the anticipation pull-back (seconds)")]
        [SerializeField] private float anticipationDuration = 0.18f;

        [Tooltip("Overshoot bounce distance downward when stopping before snapping back")]
        [SerializeField] private float bounceOvershootDistance = 22f;

        [Tooltip("Duration of the overshoot bounce settlement (seconds)")]
        [SerializeField] private float bounceDuration = 0.28f;

        public int StartingBalance => startingBalance;
        public int[] BetAmounts => betAmounts;
        public int DefaultBetIndex => defaultBetIndex;
        public float BaseSpinDuration => baseSpinDuration;
        public float ReelStaggerDelay => reelStaggerDelay;
        public float SuspenseDelay => suspenseDelay;
        public float SpinSpeed => spinSpeed;
        public float SymbolCellHeight => symbolCellHeight;
        public float AnticipationDistance => anticipationDistance;
        public float AnticipationDuration => anticipationDuration;
        public float BounceOvershootDistance => bounceOvershootDistance;
        public float BounceDuration => bounceDuration;

        public void InitializeDefaults()
        {
            startingBalance = 1000;
            betAmounts = new int[] { 10, 20, 50, 100 };
            defaultBetIndex = 0;
            baseSpinDuration = 1.0f;
            reelStaggerDelay = 0.35f;
            suspenseDelay = 0.7f;
            spinSpeed = 2200f;
            symbolCellHeight = 140f;
            anticipationDistance = 28f;
            anticipationDuration = 0.18f;
            bounceOvershootDistance = 22f;
            bounceDuration = 0.28f;
        }
    }
}
