using System.Collections.Generic;

namespace Combinations
{
    public class Combination
    {
        private readonly CombinationRule _rule;

        public string DisplayName => _rule.Name;
        //public string Description => _rule.Description;
        public CombinationRule Rule => _rule;
        public int RequiredCardCount => _rule.RequiredCardCount;

        public Combination(CombinationRule combinationRule)
        {
            _rule = combinationRule;
        }

        public bool IsSatisfied(List<CardWithPool> cards)
        {
            if (cards == null || cards.Count < RequiredCardCount) return false;

            return FindMatch(cards) != null;
        }

        /// <summary>
        /// Ищет первую подходящую комбинацию карт в списке
        /// </summary>
        /// <param name="cards">Полный список карт</param>
        /// <returns>Первое найденное подмножество карт, удовлетворяющее правилу, или null</returns>
        public List<CardWithPool> FindMatch(List<CardWithPool> cards)
        {
            if (cards == null || cards.Count < RequiredCardCount)
                return null;

            var result = new List<CardWithPool>();
            if (FindMatchRecursive(cards, 0, new List<CardWithPool>(), result))
                return result;

            return null;
        }

        /// <summary>
        /// Рекурсивно собирает подсписки карт длиной RequiredCardCount и проверяет правило
        /// </summary>
        private bool FindMatchRecursive(
            List<CardWithPool> allCards,
            int startIndex,
            List<CardWithPool> currentSubset,
            List<CardWithPool> result)
        {
            // Если набрали нужное количество - проверяем правило
            if (currentSubset.Count == RequiredCardCount)
            {
                if (_rule.Check(currentSubset))
                {
                    result.Clear();
                    result.AddRange(currentSubset);
                    return true;
                }
                return false;
            }

            // Перебираем карты начиная с startIndex
            for (int i = startIndex; i < allCards.Count; i++)
            {
                currentSubset.Add(allCards[i]);

                if (FindMatchRecursive(allCards, i + 1, currentSubset, result))
                    return true;

                currentSubset.RemoveAt(currentSubset.Count - 1);
            }

            return false;
        }
    }
}
