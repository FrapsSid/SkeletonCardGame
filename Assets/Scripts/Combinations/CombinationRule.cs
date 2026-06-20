using System;
using System.Collections.Generic;
using System.Linq;

public class CombinationRule
{
    public CombinationRuleType Type { get; }
    public int ParamN { get; }
    public int ParamValue { get; }

    // Автогенерация UI-описания правила
    public string Description => GetRuleDescription();

    // Автогенерация UI-названия правила
    public string ShortName => GetRuleShortName();

    public CombinationRule(CombinationRuleType type, int paramN = 0, int paramValue = 0)
    {
        Type = type;
        ParamN = paramN;
        ParamValue = paramValue;
    }


    public bool Check(List<CardData> cards)
    {
        if (cards == null) return false;

        switch (Type)
        {
            case CombinationRuleType.ExactCardCount:
                return cards.Count == ParamN;

            case CombinationRuleType.SameSuit:
                if (cards.Count < ParamN) return false;
                return cards.GroupBy(c => c.Suit).Any(g => g.Count() >= ParamN);

            case CombinationRuleType.SameRank:
                if (cards.Count < ParamN) return false;
                return cards.GroupBy(c => c.Value).Any(g => g.Count() >= ParamN);

            case CombinationRuleType.Sequence:
                if (cards.Count < ParamN) return false;
                return ContainsContinuousSequence(cards, ParamN);

            case CombinationRuleType.SequenceSameSuit:
                if (cards.Count < ParamN) return false;
                return cards.GroupBy(c => c.Suit)
                            .Any(g => ContainsContinuousSequence(g.ToList(), ParamN));

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

    private string GetRuleDescription()
    {
        string cardWord = GetCardWordEnding(ParamN);

        switch (Type)
        {
            case CombinationRuleType.SameRank:
                return $"{GetNumberString(ParamN, true)} {cardWord} одного номинала";

            case CombinationRuleType.Sequence:
                return $"{GetNumberString(ParamN, true)} {cardWord} по порядку";

            case CombinationRuleType.SequenceSameSuit:
                return $"{GetNumberString(ParamN, true)} {cardWord} одной масти по порядку";

            case CombinationRuleType.SameSuit:
                return $"{GetNumberString(ParamN, true)} {cardWord} одной масти";

            case CombinationRuleType.ExactCardCount:
                return $"Ровно {ParamN} {cardWord} в комбинации";

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
                return $"Содержит карту номинала {GetRankName(ParamValue)}";

            default:
                return $"{Type} (N:{ParamN}, V:{ParamValue})";
        }
    }

    private string GetRuleShortName()
    {
        switch (Type)
        {
            case CombinationRuleType.SameRank:
                return GetPokerRankName(ParamN);

            case CombinationRuleType.Sequence:
                return $"Стрит ({ParamN})";

            case CombinationRuleType.SequenceSameSuit:
                return $"Стрит-Флеш ({ParamN})";

            case CombinationRuleType.SameSuit:
                return $"Флеш ({ParamN})";

            case CombinationRuleType.ExactCardCount:
                return $"Размер: {ParamN}";

            case CombinationRuleType.SumEquals:
                return $"={ParamValue}";

            case CombinationRuleType.SumGreaterThan:
                return $">{ParamValue}";

            case CombinationRuleType.SumLessThan:
                return $"<{ParamValue}";

            case CombinationRuleType.AllDifferentSuits:
                return "Разн. Масти";

            case CombinationRuleType.AllDifferentRanks:
                return "Разн. Номиналы";

            case CombinationRuleType.ContainsRank:
                return $"+{GetRankName(ParamValue)}";

            default:
                return $"{Type}";
        }
    }

    private string GetPokerRankName(int count)
    {
        return count switch
        {
            2 => "Пара",
            3 => "Тройка",
            4 => "Каре",
            _ => $"Сет ({count})"
        };
    }

    private bool ContainsContinuousSequence(List<CardData> source, int requiredLength)
    {
        if (requiredLength <= 1) return true;

        // Сортируем по возрастанию и убираем дубликаты номиналов для корректного шага
        var sortedValues = source.Select(c => (int)c.Value).Distinct().OrderBy(v => v).ToList();
        if (sortedValues.Count < requiredLength) return false;

        int currentRun = 1;
        for (int i = 0; i < sortedValues.Count - 1; i++)
        {
            // Если следующая карта идет строго по порядку (+1 к номиналу)
            if (sortedValues[i + 1] - sortedValues[i] == 1)
            {
                currentRun++;
                if (currentRun >= requiredLength) return true;
            }
            else
            {
                currentRun = 1; // Цепочка прервалась, сбрасываем счетчик
            }
        }

        return false;
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

    // Вспомогательный метод для склонения числительных
    private string GetNumberString(int number, bool capitalize = false)
    {
        string result = number switch
        {
            2 => "две",
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

    private string GetCardWordEnding(int number)
    {
        // Исключение для чисел, оканчивающихся на 11-14 (11 карт, 12 карт...)
        int mod100 = number % 100;
        if (mod100 >= 11 && mod100 <= 14)
        {
            return "карт";
        }

        int mod10 = number % 10;
        return mod10 switch
        {
            1 => "карта",          // 1 карта, 21 карта
            >= 2 and <= 4 => "карты", // 2 карты, 3 карты, 4 карты
            _ => "карт"            // 0 карт, 5 карт, 6 карт, 30 карт...
        };
    }

    private string GetRankName(int value)
    {
        return value switch
        {
            11 => "J",
            12 => "Q",
            13 => "K",
            14 => "A",
            _ => value.ToString()
        };
    }

    #endregion
}
