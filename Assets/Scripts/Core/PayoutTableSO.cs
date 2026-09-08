using System.Collections.Generic;
using UnityEngine;

namespace SlotGame.Core
{
    /// <summary>
    /// Configures the win evaluation rules, payout multipliers, and bonus conditions.
    /// Easily editable in the Unity Inspector without modifying code (Open/Closed Principle).
    /// </summary>
    [CreateAssetMenu(fileName = "PayoutTable", menuName = "SlotGame/Payout Table")]
    public class PayoutTableSO : ScriptableObject
    {
        [Header("Three of a Kind Multipliers")]
        [Tooltip("Multiplier for 3 Sevens (Jackpot)")]
        [SerializeField] private int threeSevensMultiplier = 100;

        [Tooltip("Multiplier for 3 Bells")]
        [SerializeField] private int threeBellsMultiplier = 50;

        [Tooltip("Multiplier for 3 Bars")]
        [SerializeField] private int threeBarsMultiplier = 25;

        [Tooltip("Multiplier for 3 Cherries")]
        [SerializeField] private int threeCherriesMultiplier = 15;

        [Header("Partial / Any Match Multipliers")]
        [Tooltip("Multiplier for any 2 Cherries")]
        [SerializeField] private int twoCherriesMultiplier = 5;

        [Tooltip("Multiplier for any 1 Cherry")]
        [SerializeField] private int oneCherryMultiplier = 2;

        [Header("Bonus Features")]
        [Tooltip("Whether 7 acts as a Wild symbol matching any other symbol")]
        [SerializeField] private bool sevenIsWild = true;

        [Tooltip("Number of Free Spins awarded when 3 Bells hit")]
        [SerializeField] private int freeSpinsOnThreeBells = 5;

        [Tooltip("Win multiplier applied during Free Spins")]
        [SerializeField] private int freeSpinsMultiplier = 2;

        public int ThreeSevensMultiplier => threeSevensMultiplier;
        public int ThreeBellsMultiplier => threeBellsMultiplier;
        public int ThreeBarsMultiplier => threeBarsMultiplier;
        public int ThreeCherriesMultiplier => threeCherriesMultiplier;
        public int TwoCherriesMultiplier => twoCherriesMultiplier;
        public int OneCherryMultiplier => oneCherryMultiplier;
        public bool SevenIsWild => sevenIsWild;
        public int FreeSpinsOnThreeBells => freeSpinsOnThreeBells;
        public int FreeSpinsMultiplier => freeSpinsMultiplier;

        public void InitializeDefaults()
        {
            threeSevensMultiplier = 100;
            threeBellsMultiplier = 50;
            threeBarsMultiplier = 25;
            threeCherriesMultiplier = 15;
            twoCherriesMultiplier = 5;
            oneCherryMultiplier = 2;
            sevenIsWild = true;
            freeSpinsOnThreeBells = 5;
            freeSpinsMultiplier = 2;
        }
    }
}
