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
        [SerializeField] private float spinSpeed = 1600f;

        [Tooltip("Distance in units between symbol cell centers")]
        [SerializeField] private float symbolCellHeight = 70f;

        [Tooltip("Anticipation nudge distance upward before rolling down")]
        [SerializeField] private float anticipationDistance = 16f;

        [Tooltip("Duration of the anticipation pull-back (seconds)")]
        [SerializeField] private float anticipationDuration = 0.18f;

        [Tooltip("Overshoot bounce distance downward when stopping before snapping back")]
        [SerializeField] private float bounceOvershootDistance = 14f;

        [Tooltip("Duration of the overshoot bounce settlement (seconds)")]
        [SerializeField] private float bounceDuration = 0.26f;

        [Header("Advanced Animation Tuning")]
        [Tooltip("Deceleration phase duration when reel is stopping (seconds)")]
        [SerializeField] private float stopDuration = 0.32f;

        [Tooltip("Acceleration rate multiplier (higher = faster acceleration)")]
        [SerializeField] private float accelerationMultiplier = 5f;

        [Tooltip("Cell wrapping threshold multiplier (cellHeight * this value)")]
        [SerializeField] private float wrapThresholdMultiplier = 2.5f;

        [Tooltip("Win highlight flash duration (seconds)")]
        [SerializeField] private float winHighlightDuration = 1.8f;

        [Tooltip("Win tally animation duration (seconds)")]
        [SerializeField] private float winTallyDuration = 0.6f;

        [Tooltip("Auto free spin delay between spins (seconds)")]
        [SerializeField] private float freeSpinDelay = 1.4f;

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
        public float StopDuration => stopDuration;
        public float AccelerationMultiplier => accelerationMultiplier;
        public float WrapThresholdMultiplier => wrapThresholdMultiplier;
        public float WinHighlightDuration => winHighlightDuration;
        public float WinTallyDuration => winTallyDuration;
        public float FreeSpinDelay => freeSpinDelay;

        public void InitializeDefaults()
        {
            startingBalance = 1000;
            betAmounts = new int[] { 10, 20, 50, 100 };
            defaultBetIndex = 0;
            baseSpinDuration = 1.0f;
            reelStaggerDelay = 0.35f;
            suspenseDelay = 0.7f;
            spinSpeed = 1600f;
            symbolCellHeight = 80f;
            anticipationDistance = 16f;
            anticipationDuration = 0.18f;
            bounceOvershootDistance = 14f;
            bounceDuration = 0.26f;
            stopDuration = 0.32f;
            accelerationMultiplier = 5f;
            wrapThresholdMultiplier = 2.5f;
            winHighlightDuration = 1.8f;
            winTallyDuration = 0.6f;
            freeSpinDelay = 1.4f;
        }
    }
}
