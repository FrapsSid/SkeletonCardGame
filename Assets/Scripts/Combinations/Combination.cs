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
    private static readonly List<CardData> _executionBuffer = new List<CardData>(10);

    public string DisplayName => _displayName;
    public string Description => _description;
    public List<CombinationRule> Rules => _rules;
    public int RequiredCardCount => _requiredCardCount;

    public Combination(string displayName, List<CombinationRule> rules, int requiredCardCount)
    {
        _displayName = displayName;
        _requiredCardCount = requiredCardCount;

        // Оптимизация: Сортируем правила от легких к тяжелым для быстрого отсечения в циклах
        _rules = rules ?? new List<CombinationRule>();
        _rules = _rules.OrderBy(GetRulePriority).ToList();

        // Автогенерация UI-описания: Исключаем системный ExactCardCount, соединяем остальные через " + "
        var visibleRules = _rules.Where(r => r.Type != CombinationRuleType.ExactCardCount).ToList();

        if (visibleRules.Count > 0)
        {
            _description = string.Join(" + ", visibleRules.Select(r => r.Description));
        }
        else
        {
            _description = $"Любые {_requiredCardCount} карт";
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
        }

        // Базовый случай: Набрали нужное число карт, проводим финальную проверку тяжелых правил (последовательности и др.)
        if (currentCount == _requiredCardCount)
        {
            for (int r = 0; r < _rules.Count; r++)
            {
                if (!_rules[r].Check(currentSubset)) return false;
            }
            return true;
        }

        // Оптимизация: Если оставшихся в пуле карт физически не хватит до размера комбинации — выходим
        int cardsNeeded = _requiredCardCount - currentCount;
        if (pool.Count - startIndex < cardsNeeded) return false;

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
}
