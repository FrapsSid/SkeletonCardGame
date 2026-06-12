using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Типы атомарных правил для валидации карточных комбинаций.
/// </summary>
public enum CombinationRuleType
{
    /// <summary> Требует N карт строго одной масти (компонент Флеша). </summary>
    SameSuit,

    /// <summary> Требует N карт одного номинала (Пара, Тройка, Каре). </summary>
    SameRank,

    /// <summary> Требует последовательность из N карт по порядку любых мастей (компонент Стрита). </summary>
    Sequence,

    /// <summary> Требует последовательность из N карт по порядку строго одной масти (компонент Стрит-Флеша). </summary>
    SequenceSameSuit,

    /// <summary> Требует, чтобы общая сумма номиналов карт была точно равна X. </summary>
    SumEquals,

    /// <summary> Требует, чтобы общая сумма номиналов карт была строго больше X. </summary>
    SumGreaterThan,

    /// <summary> Требует, чтобы общая сумма номиналов карт была строго меньше X. </summary>
    SumLessThan,

    /// <summary> Требует, чтобы у всех проверяемых карт были уникальные масти (без совпадений). </summary>
    AllDifferentSuits,

    /// <summary> Требует, чтобы у всех проверяемых карт были уникальные номиналы (без совпадений). </summary>
    AllDifferentRanks,

    /// <summary> Требует наличия в наборе хотя бы одной карты с конкретным номиналом. </summary>
    ContainsRank,

    /// <summary> Фиксирует точное количество участников комбинации (ровно N карт). </summary>
    ExactCardCount
}


public class CombinationRule
{
    public CombinationRuleType Type { get; }
    public int ParamN { get; }
    public int ParamValue { get; }

    public CombinationRule(CombinationRuleType type, int paramN = 0, int paramValue = 0)
    {
        Type = type;
        ParamN = paramN;
        ParamValue = paramValue;
    }

    // Автогенерация UI-описания правила со склонением числительных
    public string Description => GetRuleDescription();

    private string GetRuleDescription()
    {
        switch (Type)
        {
            case CombinationRuleType.SameRank:
                return $"{GetRankComboName(ParamN)} ({GetNumberString(ParamN, false)} карты одного номинала)";

            case CombinationRuleType.Sequence:
                return $"{GetNumberString(ParamN, true, true)} карты по порядку";

            case CombinationRuleType.SequenceSameSuit:
                return $"{GetNumberString(ParamN, true, true)} карты одной масти по порядку";

            case CombinationRuleType.SameSuit:
                return $"{GetNumberString(ParamN, true, true)} карты одной масти";

            case CombinationRuleType.ExactCardCount:
                return $"Ровно {ParamN} карт в комбинации";

            case CombinationRuleType.SumEquals:
                return $"Сумма номиналов равна {ParamValue}";

            case CombinationRuleType.SumGreaterThan:
                return $"Сумма номиналов больше {ParamValue}";

            case CombinationRuleType.SumLessThan:
                return $"Сумма номиналов меньше {ParamValue}";

            case CombinationRuleType.AllDifferentSuits:
                return "Все карты разных мастей";

            case CombinationRuleType.AllDifferentRanks:
                return "Все карты разных номиналов";

            case CombinationRuleType.ContainsRank:
                return $"Содержит карту номинала {(CardValue)ParamValue}";

            default:
                return $"{Type} (N:{ParamN}, V:{ParamValue})";
        }
    }

    // Быстрая O(N) проверка: удовлетворяет ли готовый НАБОР карт этому правилу целиком
    public bool Check(List<CardData> cards)
    {
        if (cards == null) return false;

        switch (Type)
        {
            case CombinationRuleType.ExactCardCount:
                return cards.Count == ParamN;

            case CombinationRuleType.SameSuit:
                if (cards.Count == 0) return false;
                return cards.All(c => c.Suit == cards[0].Suit);

            case CombinationRuleType.SameRank:
                if (cards.Count == 0) return false;
                return cards.All(c => c.Value == cards[0].Value);

            case CombinationRuleType.Sequence:
                return IsContinuousSequence(cards);

            case CombinationRuleType.SequenceSameSuit:
                if (cards.Count == 0) return false;
                return cards.All(c => c.Suit == cards[0].Suit) && IsContinuousSequence(cards);

            case CombinationRuleType.SumEquals:
                return cards.Sum(c => (int)c.Value) == ParamValue;

            case CombinationRuleType.SumGreaterThan:
                return cards.Sum(c => (int)c.Value) > ParamValue;

            case CombinationRuleType.SumLessThan:
                return cards.Sum(c => (int)c.Value) < ParamValue;

            case CombinationRuleType.AllDifferentSuits:
                return cards.Select(c => c.Suit).Distinct().Count() == cards.Count;

            case CombinationRuleType.AllDifferentRanks:
                return cards.Select(c => c.Value).Distinct().Count() == cards.Count;

            case CombinationRuleType.ContainsRank:
                return cards.Any(c => (int)c.Value == ParamValue);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // Поиск в ПУЛЕ карт базового подмножества, на основе которого Combination строит пересечения
    public List<CardData> FindMatchingSubset(List<CardData> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        switch (Type)
        {
            case CombinationRuleType.ExactCardCount:
                return pool.Count >= ParamN ? pool.Take(ParamN).ToList() : null;

            case CombinationRuleType.SameSuit:
                var suitGroup = pool.GroupBy(c => c.Suit).FirstOrDefault(g => g.Count() >= ParamN);
                return suitGroup?.Take(ParamN).ToList();

            case CombinationRuleType.SameRank:
                var rankGroup = pool.GroupBy(c => c.Value).FirstOrDefault(g => g.Count() >= ParamN);
                return rankGroup?.Take(ParamN).ToList();

            case CombinationRuleType.Sequence:
                return FindSequenceSubset(pool, sameSuit: false);

            case CombinationRuleType.SequenceSameSuit:
                return FindSequenceSubset(pool, sameSuit: true);

            case CombinationRuleType.SumEquals:
                List<CardData> exactSumResult = new List<CardData>();
                if (FindSumSubsetRecursive(pool, ParamValue, 0, exactSumResult)) return exactSumResult;
                return null;

            case CombinationRuleType.SumGreaterThan:
                // Если весь набор карт удовлетворяет условию Check, возвращаем его. Иначе — null.
                return Check(pool) ? pool : null;

            case CombinationRuleType.SumLessThan:
                var smallestCard = pool.OrderBy(c => (int)c.Value).First();
                return (int)smallestCard.Value < ParamValue ? new List<CardData> { smallestCard } : null;

            case CombinationRuleType.AllDifferentSuits:
                var diffSuits = pool.GroupBy(c => c.Suit).Select(g => g.First()).Take(ParamN).ToList();
                return diffSuits.Count == ParamN ? diffSuits : null;

            case CombinationRuleType.AllDifferentRanks:
                var diffRanks = pool.GroupBy(c => c.Value).Select(g => g.First()).Take(ParamN).ToList();
                return diffRanks.Count == ParamN ? diffRanks : null;

            case CombinationRuleType.ContainsRank:
                var matchingCard = pool.FirstOrDefault(c => (int)c.Value == ParamValue);
                return matchingCard != null ? new List<CardData> { matchingCard } : null;

            default:
                return null;
        }
    }

    #region Helpers

    private bool IsContinuousSequence(List<CardData> cards)
    {
        if (cards.Count < 2) return true;
        var sorted = cards.Select(c => (int)c.Value).OrderBy(v => v).ToList();

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            if (sorted[i + 1] - sorted[i] != 1) return false;
        }
        return true;
    }

    private List<CardData> FindSequenceSubset(List<CardData> pool, bool sameSuit)
    {
        if (pool.Count < ParamN) return null;

        if (sameSuit)
        {
            foreach (var group in pool.GroupBy(c => c.Suit))
            {
                var result = GetSequenceFromList(group.ToList());
                if (result != null) return result;
            }
            return null;
        }

        return GetSequenceFromList(pool);
    }

    private List<CardData> GetSequenceFromList(List<CardData> source)
    {
        var sorted = source.OrderBy(c => (int)c.Value).ToList();

        for (int i = 0; i <= sorted.Count - ParamN; i++)
        {
            var sequenceCandidate = new List<CardData> { sorted[i] };
            for (int j = i + 1; j < sorted.Count; j++)
            {
                int lastVal = (int)sequenceCandidate.Last().Value;
                int currentVal = (int)sorted[j].Value;

                if (currentVal == lastVal + 1)
                {
                    sequenceCandidate.Add(sorted[j]);
                    if (sequenceCandidate.Count == ParamN) return sequenceCandidate;
                }
                else if (currentVal > lastVal + 1)
                {
                    break;
                }
            }
        }
        return null;
    }

    private bool FindSumSubsetRecursive(List<CardData> pool, int targetSum, int startIndex, List<CardData> currentSubset)
    {
        if (targetSum == 0) return true;
        if (targetSum < 0 || startIndex >= pool.Count) return false;

        for (int i = startIndex; i < pool.Count; i++)
        {
            CardData card = pool[i];
            currentSubset.Add(card);

            if (FindSumSubsetRecursive(pool, targetSum - (int)card.Value, i + 1, currentSubset))
            {
                return true;
            }

            currentSubset.RemoveAt(currentSubset.Count - 1);
        }

        return false;
    }

    // Возвращает классические названия покерных комбинаций для одинаковых номиналов
    private string GetRankComboName(int count)
    {
        return count switch
        {
            2 => "Пара",
            3 => "Тройка",
            4 => "Каре",
            _ => $"Сет из {count} карт"
        };
    }

    // Вспомогательный метод для склонения числительных
    // isFeminine - женский род (одна, две), capitalize - с заглавной буквы
    private string GetNumberString(int number, bool isFeminine, bool capitalize = false)
    {
        string result = number switch
        {
            2 => isFeminine ? "две" : "два",
            3 => "три",
            4 => "четыре",
            5 => "пять",
            6 => "шесть",
            7 => "семь",
            8 => "восемь",
            9 => "девять",
            10 => "десять",
            _ => number.ToString()
        };

        if (capitalize && result.Length > 0)
        {
            return char.ToUpper(result[0]) + result.Substring(1);
        }

        return result;
    }

    #endregion
}
