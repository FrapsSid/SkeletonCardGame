using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Клещи: пара одного ранга зажата двумя соседними рангами
    /// Форма: r-1, r, r, r+1
    /// Схема: 2+2 (пара r-r - источник 1; фланги r-1 и r+1 - источник 2)
    /// </summary>
    public class Pincers : CombinationRule
    {
        public override string Name => "Клещи";

        public override bool Check(List<CardWithPool> cards)
        {
            var cardDataList = cards.Select(c => c.Card).ToList();
            
            if (!CheckForm(cardDataList))
                return false;

            var ranks = cardDataList.Select(c => (int)c.Value).OrderBy(r => r).ToList();
            int centerRank = ranks[1]; // r (центральная пара)

            // Разбиваем на центральную пару и фланги
            var centerPair = new List<CardWithPool>();
            var flanks = new List<CardWithPool>();

            foreach (var card in cards)
            {
                if ((int)card.Value == centerRank)
                    centerPair.Add(card);
                else
                    flanks.Add(card);
            }

            if (centerPair.Count != 2 || flanks.Count != 2)
                return false;

            // Проверяем, что пара из одного источника
            if (centerPair[0].Pool != centerPair[1].Pool)
                return false;

            // Проверяем, что фланги из одного источника
            if (flanks[0].Pool != flanks[1].Pool)
                return false;

            return true;
        }

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 4) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();

            // Проверяем паттерн: r-1, r, r, r+1
            if (ranks[0] + 1 != ranks[1]) return false; // r-1 -> r
            if (ranks[1] != ranks[2]) return false; // r == r (пара)
            if (ranks[2] + 1 != ranks[3]) return false; // r -> r+1

            return true;
        }
    }
}
