using System.Collections.Generic;
using System.Linq;

namespace Combinations
{
    /// <summary>
    /// Статические утилиты для проверки формы комбинации (без учета источников)
    /// </summary>
    public static class FormChecker
    {
        /// <summary>
        /// Проверяет, что карты образуют последовательность рангов
        /// </summary>
        public static bool IsSequence(List<CardData> cards, int expectedLength, int step = 1)
        {
            if (cards.Count != expectedLength) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();

            for (int i = 1; i < ranks.Count; i++)
            {
                if (ranks[i] - ranks[i - 1] != step)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Проверяет, что карты образуют последовательность рангов одной масти
        /// </summary>
        public static bool IsSuitedSequence(List<CardData> cards, int expectedLength, int step = 1)
        {
            if (!IsSequence(cards, expectedLength, step)) return false;
            return AllSameSuit(cards);
        }

        /// <summary>
        /// Проверяет, что все карты одной масти
        /// </summary>
        public static bool AllSameSuit(List<CardData> cards)
        {
            if (cards.Count == 0) return false;
            var firstSuit = cards[0].Suit;
            return cards.All(c => c.Suit == firstSuit);
        }

        /// <summary>
        /// Проверяет, что карты образуют группы рангов с заданными размерами
        /// Например, [3, 2] для фулла (тройка + пара)
        /// </summary>
        public static bool HasRankGroups(List<CardData> cards, int[] groupSizes)
        {
            if (cards.Count != groupSizes.Sum()) return false;

            var rankGroups = cards.GroupBy(c => c.Value)
                                 .Select(g => g.Count())
                                 .OrderByDescending(count => count)
                                 .ToList();

            var expectedGroups = groupSizes.OrderByDescending(s => s).ToList();

            if (rankGroups.Count != expectedGroups.Count) return false;

            for (int i = 0; i < rankGroups.Count; i++)
            {
                if (rankGroups[i] != expectedGroups[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Проверяет, что есть N карт одного ранга
        /// </summary>
        public static bool HasNOfKind(List<CardData> cards, int n)
        {
            return cards.GroupBy(c => c.Value).Any(g => g.Count() == n);
        }

        /// <summary>
        /// Проверяет, что карты образуют N пар последовательных рангов
        /// </summary>
        public static bool IsConsecutivePairs(List<CardData> cards, int pairCount)
        {
            if (cards.Count != pairCount * 2) return false;

            var rankGroups = cards.GroupBy(c => c.Value)
                                 .Where(g => g.Count() == 2)
                                 .Select(g => (int)g.Key)
                                 .OrderBy(r => r)
                                 .ToList();

            if (rankGroups.Count != pairCount) return false;

            // Проверяем, что ранги идут подряд
            for (int i = 1; i < rankGroups.Count; i++)
            {
                if (rankGroups[i] - rankGroups[i - 1] != 1)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Проверяет, что карты - это две пары (не обязательно последовательных рангов)
        /// </summary>
        public static bool IsTwoPairs(List<CardData> cards)
        {
            if (cards.Count != 4) return false;

            var rankGroups = cards.GroupBy(c => c.Value)
                                 .Select(g => g.Count())
                                 .OrderByDescending(count => count)
                                 .ToList();

            return rankGroups.Count == 2 && rankGroups[0] == 2 && rankGroups[1] == 2;
        }

        /// <summary>
        /// Проверяет форму "Разводной Мост": две последовательности по 3 карты с пропуском 1 ранга
        /// Пример: 4-5-6 и 8-9-10
        /// </summary>
        public static bool IsBridgeForm(List<CardData> cards)
        {
            if (cards.Count != 6) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();

            // Проверяем, что есть две последовательности по 3 карты
            // r, r+1, r+2, r+4, r+5, r+6
            if (ranks[1] - ranks[0] != 1) return false;
            if (ranks[2] - ranks[1] != 1) return false;
            if (ranks[3] - ranks[2] != 2) return false; // пропуск ровно 1 ранг
            if (ranks[4] - ranks[3] != 1) return false;
            if (ranks[5] - ranks[4] != 1) return false;

            return true;
        }
    }
}
