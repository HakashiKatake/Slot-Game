using System.Linq;
using SlotGame.Core;

namespace SlotGame.Services
{
    /// <summary>
    /// Evaluates a 3-reel payline against the payout table.
    /// Classic rules: 3-of-a-kind wins, Sevens act as wilds, Cherries pay partial.
    /// </summary>
    public class PayoutEvaluator
    {
        public PayoutResult Evaluate(SymbolType[] reels, int bet, bool isFreeSpin, PayoutTableSO table)
        {
            if (reels == null || reels.Length < 3 || table == null)
                return PayoutResult.NoWin;

            var s0 = reels[0]; var s1 = reels[1]; var s2 = reels[2];

            // 3-of-a-kind
            if (s0 == s1 && s1 == s2)
                return EvaluateTriple(s0, bet, isFreeSpin, table, wild: false);

            // Wild Seven substitution
            if (table.SevenIsWild)
            {
                var nonSeven = reels.Where(s => s != SymbolType.Seven).ToList();
                int sevens = reels.Count(s => s == SymbolType.Seven);
                if (sevens > 0 && nonSeven.Count > 0 && nonSeven.All(s => s == nonSeven[0]))
                    return EvaluateTriple(nonSeven[0], bet, isFreeSpin, table, wild: true);
            }

            // Cherry partials
            int cherries = reels.Count(s => s == SymbolType.Cherry);
            if (cherries == 2)
            {
                int mult = table.TwoCherriesMultiplier * (isFreeSpin ? table.FreeSpinsMultiplier : 1);
                return new PayoutResult(true, mult * bet, mult, WinTier.SmallWin,
                    $"2 Cherries! Win {mult * bet}!", false, false, 0, SymbolType.Cherry);
            }
            if (cherries == 1)
            {
                int mult = table.OneCherryMultiplier * (isFreeSpin ? table.FreeSpinsMultiplier : 1);
                return new PayoutResult(true, mult * bet, mult, WinTier.SmallWin,
                    $"1 Cherry! Win {mult * bet}!", false, false, 0, SymbolType.Cherry);
            }

            return PayoutResult.NoWin;
        }

        private PayoutResult EvaluateTriple(SymbolType symbol, int bet, bool isFreeSpin, PayoutTableSO table, bool wild)
        {
            int baseMult; WinTier tier; bool jackpot = false; bool freeSpins = false; int fsCount = 0;
            string prefix = wild ? "WILD WIN! 3x " : "3x ";

            switch (symbol)
            {
                case SymbolType.Seven:
                    baseMult = table.ThreeSevensMultiplier; tier = WinTier.Jackpot;
                    jackpot = true; prefix = "JACKPOT! 3x SEVENS! "; break;
                case SymbolType.Bell:
                    baseMult = table.ThreeBellsMultiplier; tier = WinTier.BigWin;
                    freeSpins = true; fsCount = table.FreeSpinsOnThreeBells;
                    prefix = $"BELL BONUS! +{fsCount} FREE SPINS! "; break;
                case SymbolType.Bar:
                    baseMult = table.ThreeBarsMultiplier; tier = WinTier.MediumWin;
                    prefix += "BARS! "; break;
                default: // Cherry
                    baseMult = table.ThreeCherriesMultiplier; tier = WinTier.MediumWin;
                    prefix += "CHERRIES! "; break;
            }

            int mult = baseMult * (isFreeSpin ? table.FreeSpinsMultiplier : 1);
            int payout = mult * bet;
            string desc = $"{prefix}Won {payout} Credits ({mult}x)!";
            return new PayoutResult(true, payout, mult, tier, desc, jackpot, freeSpins, fsCount, symbol);
        }
    }
}
