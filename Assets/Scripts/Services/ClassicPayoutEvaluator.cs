using System.Linq;
using SlotGame.Core;

namespace SlotGame.Services
{
    /// <summary>
    /// Implements classic casino 3-reel slot evaluation logic with Wild 7s and Free Spins bonus triggers.
    /// </summary>
    public class ClassicPayoutEvaluator : IPayoutEvaluator
    {
        public PayoutResult Evaluate(
            SymbolType[] paylineSymbols,
            int currentBet,
            bool isFreeSpin,
            PayoutTableSO table)
        {
            if (paylineSymbols == null || paylineSymbols.Length < 3 || table == null)
            {
                return PayoutResult.NoWin;
            }

            var s0 = paylineSymbols[0];
            var s1 = paylineSymbols[1];
            var s2 = paylineSymbols[2];

            // 1. Direct 3 of a kind match
            if (s0 == s1 && s1 == s2)
            {
                return EvaluateThreeOfAKind(s0, currentBet, isFreeSpin, table, isWildSubstituted: false);
            }

            // 2. Wild Seven substitution (if enabled in paytable)
            if (table.SevenIsWild)
            {
                var nonSevenSymbols = paylineSymbols.Where(s => s != SymbolType.Seven).ToList();
                int sevenCount = paylineSymbols.Count(s => s == SymbolType.Seven);

                // If we have 1 or 2 Sevens, and all non-seven symbols are identical, it forms a 3-of-a-kind!
                if (sevenCount > 0 && nonSevenSymbols.Count > 0)
                {
                    bool allSameNonSeven = nonSevenSymbols.All(s => s == nonSevenSymbols[0]);
                    if (allSameNonSeven)
                    {
                        SymbolType targetSymbol = nonSevenSymbols[0];
                        return EvaluateThreeOfAKind(targetSymbol, currentBet, isFreeSpin, table, isWildSubstituted: true);
                    }
                }
            }

            // 3. Cherry partial matches (Any 2 or Any 1 Cherry)
            int cherryCount = paylineSymbols.Count(s => s == SymbolType.Cherry);
            if (cherryCount == 2)
            {
                int mult = table.TwoCherriesMultiplier;
                if (isFreeSpin) mult *= table.FreeSpinsMultiplier;
                int payout = mult * currentBet;
                string desc = isFreeSpin ? $"2 Cherries! (Free Spin 2x) Win {payout}!" : $"2 Cherries! Win {payout}!";
                return new PayoutResult(true, payout, mult, WinTier.SmallWin, desc, false, false, 0, SymbolType.Cherry);
            }
            else if (cherryCount == 1)
            {
                int mult = table.OneCherryMultiplier;
                if (isFreeSpin) mult *= table.FreeSpinsMultiplier;
                int payout = mult * currentBet;
                string desc = isFreeSpin ? $"1 Cherry! (Free Spin 2x) Win {payout}!" : $"1 Cherry! Win {payout}!";
                return new PayoutResult(true, payout, mult, WinTier.SmallWin, desc, false, false, 0, SymbolType.Cherry);
            }

            return PayoutResult.NoWin;
        }

        private PayoutResult EvaluateThreeOfAKind(
            SymbolType symbol,
            int currentBet,
            bool isFreeSpin,
            PayoutTableSO table,
            bool isWildSubstituted)
        {
            int baseMult = 0;
            WinTier tier = WinTier.MediumWin;
            bool isJackpot = false;
            bool isFreeSpinsTriggered = false;
            int freeSpinsAwarded = 0;
            string prefix = isWildSubstituted ? "WILD WIN! 3x " : "3x ";

            switch (symbol)
            {
                case SymbolType.Seven:
                    baseMult = table.ThreeSevensMultiplier;
                    tier = WinTier.Jackpot;
                    isJackpot = true;
                    prefix = "JACKPOT! 3x SEVENS! ";
                    break;

                case SymbolType.Bell:
                    baseMult = table.ThreeBellsMultiplier;
                    tier = WinTier.BigWin;
                    isFreeSpinsTriggered = true;
                    freeSpinsAwarded = table.FreeSpinsOnThreeBells;
                    prefix = $"BELL BONUS! +{freeSpinsAwarded} FREE SPINS! ";
                    break;

                case SymbolType.Bar:
                    baseMult = table.ThreeBarsMultiplier;
                    tier = WinTier.MediumWin;
                    prefix += "BARS! ";
                    break;

                case SymbolType.Cherry:
                    baseMult = table.ThreeCherriesMultiplier;
                    tier = WinTier.MediumWin;
                    prefix += "CHERRIES! ";
                    break;
            }

            int finalMult = baseMult;
            if (isFreeSpin)
            {
                finalMult *= table.FreeSpinsMultiplier;
                prefix += $"(Free Spin {table.FreeSpinsMultiplier}x Multiplier!) ";
            }

            int totalPayout = finalMult * currentBet;
            string description = $"{prefix}Won {totalPayout} Credits ({finalMult}x)!";

            return new PayoutResult(
                true,
                totalPayout,
                finalMult,
                tier,
                description,
                isJackpot,
                isFreeSpinsTriggered,
                freeSpinsAwarded,
                symbol);
        }
    }
}
