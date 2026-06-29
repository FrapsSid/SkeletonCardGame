using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Две Пары С Расколом: 2 разных ранга, по 2 карты каждого
    /// Схема: каждый источник дает по одной карте каждого ранга (перекрестное разбиение)
    /// Пример: 7 и Q - источник 1; еще 7 и еще Q - источник 2
    /// </summary>
    public class TwoPairSplit : CombinationRule
    {
        public override string Name => "Две Пары С Расколом";

        public override bool Check(List<CardWithPool> cards)
        {
            var cardDataList = cards.Select(c => c.Card).ToList();
            
            if (!CheckForm(cardDataList))
                return false;

            // Получаем два ранга
            var rankGroups = cards.GroupBy(c => c.Value).ToList();
            if (rankGroups.Count != 2)
                return false;

            var rank1 = rankGroups[0].Key;
            var rank2 = rankGroups[1].Key;

            // Группируем по пулам
            var poolGroups = cards.GroupBy(c => c.Pool).ToList();
            
            // Должно быть ровно 2 пула
            if (poolGroups.Count != 2)
                return false;

            // Каждый пул должен содержать по одной карте каждого ранга
            foreach (var pool in poolGroups)
            {
                if (pool.Count() != 2)
                    return false;

                var poolRanks = pool.Select(c => c.Value).OrderBy(r => r).ToList();
                var expectedRanks = new[] { rank1, rank2 }.OrderBy(r => r).ToList();

                if (!poolRanks.SequenceEqual(expectedRanks))
                    return false;
            }

            return true;
        }

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.IsTwoPairs(cards);
        }
    }
}
