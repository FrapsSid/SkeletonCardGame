using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Перекрест Мастей: две ранговые пары в двух мастях
    /// Форма: A♠, A♥, B♠, B♥
    /// Схема: A♠ и B♥ - источник 1; A♥ и B♠ - источник 2 (диагональный обмен)
    /// </summary>
    public class SuitCross : CombinationRule
    {
        public override string Name => "Перекрест Мастей";

        public override bool Check(List<CardWithPool> cards)
        {
            var cardDataList = cards.Select(c => c.Card).ToList();
            
            if (!CheckForm(cardDataList))
                return false;

            // Получаем два ранга и две масти
            var rankGroups = cards.GroupBy(c => c.Value).OrderBy(g => g.Key).ToList();
            var suitGroups = cards.GroupBy(c => c.Suit).OrderBy(g => g.Key).ToList();

            if (rankGroups.Count != 2 || suitGroups.Count != 2)
                return false;

            var rankA = rankGroups[0].Key;
            var rankB = rankGroups[1].Key;
            var suit1 = suitGroups[0].Key;
            var suit2 = suitGroups[1].Key;

            // Находим карты по позициям
            var cardA1 = cards.FirstOrDefault(c => c.Value == rankA && c.Suit == suit1); // A♠
            var cardA2 = cards.FirstOrDefault(c => c.Value == rankA && c.Suit == suit2); // A♥
            var cardB1 = cards.FirstOrDefault(c => c.Value == rankB && c.Suit == suit1); // B♠
            var cardB2 = cards.FirstOrDefault(c => c.Value == rankB && c.Suit == suit2); // B♥

            if (cardA1.Equals(default(CardWithPool)) || cardA2.Equals(default(CardWithPool)) ||
                cardB1.Equals(default(CardWithPool)) || cardB2.Equals(default(CardWithPool)))
                return false;

            // Проверяем диагональное разбиение: A♠ и B♥ из одного источника, A♥ и B♠ из другого
            var diagonal1Pool = cardA1.Pool;
            var diagonal2Pool = cardA2.Pool;

            // Вариант 1: A♠ и B♥ - один источник, A♥ и B♠ - другой
            if (cardB2.Pool == diagonal1Pool && cardB1.Pool == diagonal2Pool && diagonal1Pool != diagonal2Pool)
                return true;

            // Вариант 2: A♥ и B♠ - один источник, A♠ и B♥ - другой (обратная диагональ)
            if (cardB1.Pool == diagonal1Pool && cardB2.Pool == diagonal2Pool && diagonal1Pool != diagonal2Pool)
                return true;

            return false;
        }

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 4) return false;

            // Должно быть ровно 2 разных ранга
            var rankCount = cards.GroupBy(c => c.Value).Count();
            if (rankCount != 2) return false;

            // Должно быть ровно 2 разных масти
            var suitCount = cards.GroupBy(c => c.Suit).Count();
            if (suitCount != 2) return false;

            // Каждый ранг должен встречаться дважды
            var rankGroups = cards.GroupBy(c => c.Value).ToList();
            if (!rankGroups.All(g => g.Count() == 2))
                return false;

            // Каждая масть должна встречаться дважды
            var suitGroups = cards.GroupBy(c => c.Suit).ToList();
            if (!suitGroups.All(g => g.Count() == 2))
                return false;

            return true;
        }
    }
}
