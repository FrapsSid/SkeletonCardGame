using System.Collections.Generic;
using UnityEngine;
using Combinations;

public class TeamCardPool
{
    // Текущий кэшированный пул данных карт команды
    private List<CardWithPool> _currentPool = new List<CardWithPool>();

    public List<CardWithPool> CurrentPool => _currentPool;

    //Собирает карты из рук всех скелетов команды и карт на столе в один плоский список
    public List<CardWithPool> BuildPool(Team team, List<CardData> tableCards)
    {
        _currentPool.Clear();

        if (team == null || team.Skeletons == null)
        {
            Debug.LogWarning("[TeamCardPool] Team or Skeletons list is null!");
            return _currentPool;
        }

        // Добавляем карты из рук игроков
        for (int i = 0; i < team.Skeletons.Count; i++)
        {
            var skeleton = team.Skeletons[i];
            if (skeleton?.Hand == null)
                continue;

            // Определяем пул в зависимости от индекса игрока
            CardPool pool;
            if (i == 0)
            {
                pool = CardPool.Player1Hand;
            }
            else if (i == 1)
            {
                pool = CardPool.Player2Hand;
            }
            else
            {
                // У нас только 2 пула для игроков
                Debug.LogWarning($"[TeamCardPool] Player index {i} exceeds available pools (max 2). Skipping.");
                continue;
            }

            // Получаем карты из руки (предполагаем, что GetCards() возвращает List<CardData>)
            var handCards = skeleton.Hand.GetCards();
            if (handCards != null)
            {
                foreach (var cardData in handCards)
                {
                    _currentPool.Add(new CardWithPool(cardData, pool));
                }
            }
        }

        // Добавляем карты со стола
        if (tableCards != null)
        {
            foreach (var cardData in tableCards)
            {
                _currentPool.Add(new CardWithPool(cardData, CardPool.Table));
            }
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
