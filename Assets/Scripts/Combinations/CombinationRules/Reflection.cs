using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Отражение: две одинаковые трехкарточные линии рангов с шагом 2
    /// Форма: r, r+2, r+4 и еще r, r+2, r+4
    /// Схема: 3+3 (одна линия - источник 1; вторая линия - источник 2)
    /// </summary>
    public class Reflection : CombinationRule
    {
        public override string Name => "Отражение";

        public override bool Check(List<CardWithPool> cards)
        {
            var cardDataList = cards.Select(c => c.Card).ToList();
            
            if (!CheckForm(cardDataList))
                return false;

            // Получаем уникальные ранги (должно быть ровно 3)
            var uniqueRanks = cards.Select(c => c.Value).Distinct().OrderBy(r => r).ToList();
            if (uniqueRanks.Count != 3)
                return false;

            // Группируем карты по пулам
            var poolGroups = cards.GroupBy(c => c.Pool).ToList();

            // Должно быть ровно 2 пула
            if (poolGroups.Count != 2)
                return false;

            // Каждый пул должен содержать по 3 карты
            if (!poolGroups.All(g => g.Count() == 3))
                return false;

            // Каждый пул должен содержать одну карту каждого из трех рангов
            foreach (var pool in poolGroups)
            {
                var poolRanks = pool.Select(c => c.Value).OrderBy(r => r).ToList();
                if (!poolRanks.SequenceEqual(uniqueRanks))
                    return false;
            }

            return true;
        }

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 6) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();

            // Должно быть 3 уникальных ранга, каждый встречается дважды
            var uniqueRanks = ranks.Distinct().ToList();
            if (uniqueRanks.Count != 3)
                return false;

            // Каждый ранг должен встречаться ровно дважды
            foreach (var rank in uniqueRanks)
            {
                if (ranks.Count(r => r == rank) != 2)
                    return false;
            }

            // Ранги должны идти с шагом 2: r, r+2, r+4
            if (uniqueRanks[1] - uniqueRanks[0] != 2)
                return false;
            if (uniqueRanks[2] - uniqueRanks[1] != 2)
                return false;

            return true;
        }
    }
}
