using System;
using System.Collections.Generic;
using System.Linq;

public class Combination
{
    private readonly string _displayName;
    private readonly string _description;
    private readonly List<CombinationRule> _rules;
    private readonly int _requiredCardCount;

    // GC Optimization: Переиспользуемый буфер для исключения аллокаций в куче (Heap Allocation) во время рекурсии
    private static readonly List<CardData> _executionBuffer = new List<CardData>(32);

    public string DisplayName => _displayName;
    public string Description => _description;
    public List<CombinationRule> Rules => _rules;
    public int RequiredCardCount => _requiredCardCount;

    public Combination(List<CombinationRule> rules)
    {
        //Сортируем правила от легких к тяжелым для быстрого отсечения в циклах
        _rules = rules ?? new List<CombinationRule>();
        _rules = _rules.OrderBy(GetRulePriority).ToList();

        //Ищем максимальный ParamN среди правил, где он задан (> 0)
        var rulesWithParamN = _rules.Where(r => r.ParamN > 0).ToList();
        _requiredCardCount = rulesWithParamN.Count > 0
            ? rulesWithParamN.Max(r => r.ParamN)
            : 1;

        // АВТОГЕНЕРАЦИЯ НАЗВАНИЯ
        if (_rules.Count > 0)
        {
            _displayName = string.Join(" + ", _rules.Select(r => r.ShortName));
        }
        else
        {
            _displayName = $"Набор ({_requiredCardCount})";
        }

        // АВТОГЕНЕРАЦИЯ UI-ОПИСАНИЯ
        if (_rules.Count > 0)
        {
            _description = string.Join(" + ", _rules.Select(r => r.Description));
        }
        else
        {
            string countString = _requiredCardCount == 1 ? "Любая" : "Любые";
            _description = $"{countString} {GetNumberString(_requiredCardCount, true)} {GetCardWordEnding(_requiredCardCount)}";
        }
    }


    public string GetDisplayText()
    {
        return $"{_displayName}\n({_description})";
    }

    public bool IsSatisfied(List<CardData> cardPool)
    {
        if (cardPool == null || cardPool.Count < _requiredCardCount) return false;

        return FindMatch(cardPool) != null;
    }

    public List<CardData> FindMatch(List<CardData> cardPool)
    {
        if (cardPool == null || cardPool.Count < _requiredCardCount) return null;

        _executionBuffer.Clear();

        // Запуск Backtracking-перебора. Память выделяется только при успешном возврате результата наружу.
        if (FindValidSubsetRecursive(cardPool, 0, _executionBuffer))
        {
            return new List<CardData>(_executionBuffer);
        }

        return null;
    }

    #region Оптимизация и Перебор

    // Оценка вычислительной сложности правил: O(1) и O(N) операции идут первыми, сортировки и группировки — последними
    private int GetRulePriority(CombinationRule rule)
    {
        switch (rule.Type)
        {
            case CombinationRuleType.ExactCardCount: return 0;
            case CombinationRuleType.ContainsRank: return 1;
            case CombinationRuleType.SumEquals: return 2;
            case CombinationRuleType.SumGreaterThan: return 3;
            case CombinationRuleType.SumLessThan: return 4;

            case CombinationRuleType.SameRank: return 5;
            case CombinationRuleType.SameSuit: return 6;
            case CombinationRuleType.AllDifferentRanks: return 7;
            case CombinationRuleType.AllDifferentSuits: return 8;

            case CombinationRuleType.Sequence: return 9;
            case CombinationRuleType.SequenceSameSuit: return 10;

            default: return 100;
        }
    }

    // Алгоритм поиска с возвратом (Backtracking)
    private bool FindValidSubsetRecursive(List<CardData> pool, int startIndex, List<CardData> currentSubset)
    {
        int currentCount = currentSubset.Count;

        // Pruning (Ранний выход): Проверяем накопительные правила до полной сборки сета. Если лимит уже нарушен — обрубаем ветку.
        for (int r = 0; r < _rules.Count; r++)
        {
            var rule = _rules[r];

            if (rule.Type == CombinationRuleType.SumLessThan && !rule.Check(currentSubset))
                return false;

            if ((rule.Type == CombinationRuleType.AllDifferentRanks || rule.Type == CombinationRuleType.AllDifferentSuits)
                && !rule.Check(currentSubset))
                return false;

            if (rule.Type == CombinationRuleType.ExactCardCount)
            {
                if (currentCount > rule.ParamN)
                    return false;

                if (rule.Check(currentSubset))
                {
                    bool allPassed = true;
                    for (int r1 = 0; r1 < _rules.Count; r1++)
                    {
                        if (r1 == r) continue;

                        if (!_rules[r1].Check(currentSubset))
                        {
                            allPassed = false;
                            break;
                        }
                    }
                    if (allPassed) return true;
                    }

                    return false;
                }
        }

        // Базовый случай: Набрали нужное число карт, проводим финальную проверку
        if (currentCount >= _requiredCardCount)
        {
            bool allRulesPassed = true;
            for (int r = 0; r < _rules.Count; r++)
            {
                if (!_rules[r].Check(currentSubset))
                {
                    allRulesPassed = false;
                    break;
                }
            }
            if (allRulesPassed) return true;
        }

        // Оптимизация: Если оставшихся в пуле карт физически не хватит до размера комбинации — выходим
        int cardsNeeded = _requiredCardCount - currentCount;
        if (cardsNeeded > 0 && (pool.Count - startIndex < cardsNeeded))
            return false;

        // Рекурсивное построение комбинаций. startIndex гарантирует движение только вперед (исключает дубликаты перестановок)
        for (int i = startIndex; i < pool.Count; i++)
        {
            currentSubset.Add(pool[i]);

            if (FindValidSubsetRecursive(pool, i + 1, currentSubset))
            {
                return true;
            }

            currentSubset.RemoveAt(currentSubset.Count - 1); // Откат (Backtrack)
        }

        return false;
    }

    #endregion

    #region Helpers

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

    #endregion
}
