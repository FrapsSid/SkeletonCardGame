using System.Collections.Generic;
using UnityEngine;

public class TeamCardPool
{
    // Текущий кэшированный пул данных карт команды
    private List<CardData> _currentPool = new List<CardData>();

    public List<CardData> CurrentPool => _currentPool;

    //Собирает карты из рук всех скелетов команды и карт на столе в один плоский список
    public List<CardData> BuildPool(Team team, List<CardData> tableCards)
    {
        _currentPool.Clear();

        if (team == null || team.Skeletons == null)
        {
            Debug.LogWarning("[TeamCardPool] Team or Skeletons list is null!");
            return _currentPool;
        }

        foreach (var skeleton in team.Skeletons)
        {
            if (skeleton?.Hand != null)
            {
                _currentPool.AddRange(skeleton.Hand.GetCards());
            }
        }

        if (tableCards != null)
        {
            _currentPool.AddRange(tableCards);
        }

        return _currentPool;
    }

    //Проверяет, собрана ли конкретная комбинация на основе текущего пула
    public bool CheckCombination(Combination combo)
    {
        if (combo == null) return false;

        return combo.IsSatisfied(_currentPool);
    }

    //Проверяет массив комбинаций и возвращает словарь статусов (собрана/нет)
    public Dictionary<Combination, bool> CheckAllCombinations(List<Combination> combos)
    {
        var results = new Dictionary<Combination, bool>();

        if (combos == null) return results;

        foreach (var combo in combos)
        {
            if (combo == null) continue;

            bool isCollected = combo.IsSatisfied(_currentPool);
            results.Add(combo, isCollected);
        }

        return results;
    }
}
