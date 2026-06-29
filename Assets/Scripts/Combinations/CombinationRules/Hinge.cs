using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Шарнир: 6 карт на 5 последовательных рангах, где центральный ранг повторен
    /// Форма: r, r+1, r+2, r+2, r+3, r+4
    /// Схема: 2+4 (пара центрального ранга - источник 1; 4 внешние карты - источник 2)
    /// </summary>
    public class Hinge : CombinationRule
    {
        public override string Name => "Шарнир";

        public override bool Check(List<CardWithPool> cards)
        {
            var cardDataList = cards.Select(c => c.Card).ToList();
            
            if (!CheckForm(cardDataList))
                return false;

            // Находим центральный ранг (который повторяется)
            var ranks = cardDataList.Select(c => (int)c.Value).OrderBy(r => r).ToList();
            int centerRank = ranks[2]; // Должен быть r+2

            // Разбиваем карты на пару центра и внешние
            var centerPair = new List<CardWithPool>();
            var outerCards = new List<CardWithPool>();

            foreach (var card in cards)
            {
                if ((int)card.Value == centerRank)
                    centerPair.Add(card);
                else
                    outerCards.Add(card);
            }

            if (centerPair.Count != 2 || outerCards.Count != 4)
                return false;

            // Проверяем, что пара из одного источника
            if (centerPair[0].Pool != centerPair[1].Pool)
                return false;

            // Проверяем, что внешние карты из одного источника
            var outerPool = outerCards[0].Pool;
            if (!outerCards.All(c => c.Pool == outerPool))
                return false;

            // Источники пары и внешних карт могут совпадать или быть разными
            return true;
        }

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 6) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();

            // Проверяем паттерн: r, r+1, r+2, r+2, r+3, r+4
            if (ranks[0] + 1 != ranks[1]) return false; // r -> r+1
            if (ranks[1] + 1 != ranks[2]) return false; // r+1 -> r+2
            if (ranks[2] != ranks[3]) return false; // r+2 == r+2 (пара)
            if (ranks[3] + 1 != ranks[4]) return false; // r+2 -> r+3
            if (ranks[4] + 1 != ranks[5]) return false; // r+3 -> r+4

            return true;
        }
    }
}
