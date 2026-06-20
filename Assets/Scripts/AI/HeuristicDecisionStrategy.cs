using System;
using System.Collections.Generic;
using UnityEngine;

public class HeuristicDecisionStrategy : BaseAIDecisionStrategy
{
    // --- Пороги риска для защиты от Антикомбинации ---
    private const float RISK_THRESHOLD_EASY_MET = 0.25f;    // Собрана легкая: умеренная осторожность
    private const float RISK_THRESHOLD_MEDIUM_MET = 0.15f;  // Собрана средняя: высокий риск потерять 2 очка
    private const float RISK_THRESHOLD_HARD_MET = 0.00f;    // Собрана сложная: любой риск запрещен
    private const float RISK_THRESHOLD_NOTHING_MET = 0.45f; // Ничего не собрано: повышенный трешхолд ради спасения игры

    private const int RISK_SAMPLE_SIZE = 10;                // Размер выборки карт для симуляции

    // --- Пороги экономической агрессии (Максимальный % роста ставки от банка) ---
    private const float BET_GROWTH_THRESHOLD_NOTHING = 0.30f; // 30% при пустой руке
    private const float BET_GROWTH_THRESHOLD_EASY = 0.60f;    // 60% при легкой комбинации
    private const float BET_GROWTH_THRESHOLD_MEDIUM = 1.50f;  // 150% при средней комбинации
    private const float BET_GROWTH_THRESHOLD_HARD = float.MaxValue; // Коллим всё при сложной

    // --- ФАЗА ТОРГОВЛИ (АКТИВНАЯ) ---
    public override AIResponsePackage ChooseBettingAction(GameStateSnapshot snapshot)
    {
        // 0. Предохранитель: если скелет недееспособен — автоматический фолд
        if (snapshot.OwnBody != null && snapshot.OwnBody.IsIncapacitated)
        {
            return new AIResponsePackage(AIActionType.Fold);
        }

        // --- ЧАСТЬ 1: ПРОВЕРКА НА ФОЛД ---
        if (ShouldFold(snapshot))
        {
            return new AIResponsePackage(AIActionType.Fold);
        }

        // --- ЧАСТЬ 2: ВЫБОР МЕЖДУ ЧЕКОМ/КОЛЛОМ И РЕЙЗОМ ---
        CombinationDifficulty? realDifficulty = EvaluateHand(snapshot.OwnHand, snapshot.TableCards, snapshot.RoundCombinations);

        // Универсально рассчитываем сумму возможного повышения на основе текущей силы руки
        int raiseAmount = CalculateRaiseAmount(snapshot, realDifficulty);

        // Если расчетная сумма рейза больше нуля — объявляем агрессивный Рейз
        if (raiseAmount > 0)
        {
            // Передаем полученную сумму raiseAmount в метод выбора костей для ставки
            List<BodyPart> partsToStake = SelectBodyPartsForStake(snapshot, realDifficulty, raiseAmount);

            return new AIResponsePackage(AIActionType.Raise, raiseAmount, partsToStake);
        }

        // Если raiseAmount равен 0, то повышать ставку нет смысла — просто коллируем чужую или чекаем
        return new AIResponsePackage(AIActionType.CheckCall);
    }

    // --- ПАССИВНАЯ ФАЗА (ХОД С КАРТАМИ) ---
    public override AIResponsePackage ChooseCardAction(GameStateSnapshot snapshot)
    {
        // 0. Предохранитель: если скелет обездвижен — пассивно пропускаем ход
        if (snapshot.OwnBody != null && snapshot.OwnBody.IsIncapacitated)
        {
            return new AIResponsePackage(AIActionType.CheckCall);
        }

        CombinationDifficulty? realDifficulty = EvaluateHand(snapshot.OwnHand, snapshot.TableCards, snapshot.RoundCombinations);

        if(realDifficulty.HasValue && realDifficulty.Value == CombinationDifficulty.Anti) 
        {
            return new AIResponsePackage(AIActionType.Fold);
        }

        float antiRisk = CalculateAntiRisk(snapshot.OwnHand, snapshot.TableCards, snapshot);

        // --- УСЛОВИЕ 1: Заявленной комбинации нет ---
        if (!snapshot.OwnTarget.HasValue)
        {
            CombinationDifficulty difficultyToDeclare = realDifficulty ?? CombinationDifficulty.Easy;

            // Заявляем реальную собранную комбинацию, если она есть. Если она null — заявляем легкую (Easy)
            DeclaredCombinationTier targetToDeclare = difficultyToDeclare switch
            {
                CombinationDifficulty.Easy => DeclaredCombinationTier.Easy,
                CombinationDifficulty.Medium => DeclaredCombinationTier.Medium,
                CombinationDifficulty.Hard => DeclaredCombinationTier.Hard,
                _ => DeclaredCombinationTier.Easy
            };

            return new AIResponsePackage(AIActionType.ChangeCombination, 0, null, targetToDeclare);
        }

        DeclaredCombinationTier currentTarget = snapshot.OwnTarget.Value;

        // --- УСЛОВИЕ 2: Реальная комбинация НИЖЕ или РАВНА заявленной, либо равна NULL ---
        // (При условии, что если она есть, она не является блокирующей Anti-комбинацией)
        if (!realDifficulty.HasValue || (realDifficulty.Value <= DeclaredCombinationTierExtensions.ToCombinationDifficulty(currentTarget)))
        {
            // Получаем динамический порог риска для текущей руки
            float dynamicRiskThreshold = GetRiskThresholdForTier(realDifficulty);

            if (antiRisk < dynamicRiskThreshold)
            {
                return new AIResponsePackage(AIActionType.DrawCard);
            }

            // Если риск превышен или оторваны руки — фиксируем очки и пассивно пропускаем ход
            return new AIResponsePackage(AIActionType.CheckCall);
        }

        // --- УСЛОВИЕ 3: Реальная комбинация ВЫШЕ заявленной ---
        // (Сработает, только если realDifficulty.HasValue и realDifficulty.Value > currentTarget)
        if (realDifficulty.HasValue && realDifficulty.Value > DeclaredCombinationTierExtensions.ToCombinationDifficulty(currentTarget))
        {
            DeclaredCombinationTier targetToDeclare = realDifficulty.Value switch
            {
                CombinationDifficulty.Easy => DeclaredCombinationTier.Easy,
                CombinationDifficulty.Medium => DeclaredCombinationTier.Medium,
                CombinationDifficulty.Hard => DeclaredCombinationTier.Hard,
                _ => DeclaredCombinationTier.Hard
            };

            // Мгновенно переключаем заявку сразу на уровень реально собранной (понижать нельзя, только вверх)
            return new AIResponsePackage(AIActionType.ChangeCombination, 0, null, targetToDeclare);
        }

        // Дефолтный защитный пропуск хода
        return new AIResponsePackage(AIActionType.CheckCall);

    }


    // --- МАТЕМАТИЧЕСКИЕ И ОЦЕНОЧНЫЕ МЕТОДЫ ---

    /// <summary>
    /// Оценивает силу карт конкретного игрока в связке со столом на основе комбинаций раунда.
    /// Проверяет совпадения поочередно, начиная со Сложной комбинации.
    /// </summary>
    private CombinationDifficulty? EvaluateHand(List<CardData> hand, List<CardData> table, RoundCombinationSet roundCombinations)
    {
        if (roundCombinations == null) return null;

        List<CardData> fullCardPool = new List<CardData>(hand.Count + table.Count);
        fullCardPool.AddRange(hand);
        fullCardPool.AddRange(table);

        // 0. ПРИОРИТЕТ №0: Проверяем Антикомбинацию (0)
        Combination anti = roundCombinations.GetCombination(CombinationDifficulty.Anti);
        if (anti != null && anti.FindMatch(fullCardPool) != null)
        {
            return CombinationDifficulty.Anti;
        }

        // 1. ПРИОРИТЕТ №1: Проверяем Сложную комбинацию (3)
        Combination hard = roundCombinations.GetCombination(CombinationDifficulty.Hard);
        if (hard != null && hard.FindMatch(fullCardPool) != null)
        {
            return CombinationDifficulty.Hard;
        }

        // 2. ПРИОРИТЕТ №2: Проверяем Среднюю комбинацию (2)
        Combination medium = roundCombinations.GetCombination(CombinationDifficulty.Medium);
        if (medium != null && medium.FindMatch(fullCardPool) != null)
        {
            return CombinationDifficulty.Medium;
        }

        // 3. ПРИОРИТЕТ №3: Проверяем Простую/Легкую комбинацию (1)
        Combination easy = roundCombinations.GetCombination(CombinationDifficulty.Easy);
        if (easy != null && easy.FindMatch(fullCardPool) != null)
        {
            return CombinationDifficulty.Easy;
        }

        // 5. Если не совпало вообще ничего — возвращаем null (мусор на руке)
        return null;
    }

    /// <summary>
    /// Математически точный расчет риска получить Антикомбинацию.
    /// Выдергивает 10 случайных карт прямо из колоды снимка snapshot._AIDeck.
    /// </summary>
    private float CalculateAntiRisk(List<CardData> currentHand, List<CardData> tableCards, GameStateSnapshot snapshot)
    {
        // Предохранитель: если колода в снимке пуста или не задана — риск равен 0
        if (snapshot._AIDeck == null || snapshot._AIDeck.IsEmpty) return 0.0f;

        // Находим размер выборки (если в колоде осталось меньше 10 карт, тестируем только доступный остаток)
        int sampleSize = Math.Min(RISK_SAMPLE_SIZE, snapshot._AIDeck.CardsCount);
        int antiOutsCount = 0;

        // Списки для хранения вытащенных тестовых карт и временного пула руки
        List<CardData> sampledCards = new List<CardData>(sampleSize);
        List<CardData> temporaryHand = new List<CardData>(currentHand.Count + 1);

        // ШАГ 1: Поочередно достаем случайные карты из колоды снимка
        // (Ваш AIDeck.DrawCard() внутри себя уже использует randInt для случайного выбора)
        for (int i = 0; i < sampleSize; i++)
        {
            CardData drawnCard = snapshot._AIDeck.DrawCard();
            if (drawnCard != null)
            {
                sampledCards.Add(drawnCard);
            }
        }

        // ШАГ 2: Проверяем только эти вытащенные карты через покерный оценщик
        for (int i = 0; i < sampledCards.Count; i++)
        {
            temporaryHand.Clear();
            temporaryHand.AddRange(currentHand);
            temporaryHand.Add(sampledCards[i]);

            Combination anti = snapshot.RoundCombinations.GetCombination(CombinationDifficulty.Anti);

            if (anti.FindMatch(temporaryHand) != null)
            {
                antiOutsCount++;
            }
        }

        // ШАГ 3: Обязательно возвращаем карты обратно в колоду снимка, 
        // чтобы не разрушать состояние данных для последующих проверок или MCTS
        ReturnCardsToDeck(snapshot._AIDeck, sampledCards);

        // Если из-за пустоты колоды ничего не вытащили, возвращаем 0
        if (sampledCards.Count == 0) return 0.0f;

        // Возвращаем финальный статистический процент риска
        return (float)antiOutsCount / sampledCards.Count;
    }

    /// <summary>
    /// Возвращает тестовые карты обратно в виртуальную колоду ИИ.
    /// </summary>
    private void ReturnCardsToDeck(AIDeck deck, List<CardData> cardsToReturn)
    {
        if (deck == null || cardsToReturn == null) return;

        foreach (var card in cardsToReturn)
        {
            deck.ReturnCard(card); 
        }
    }

    /// <summary>
    /// Возвращает порог допустимого роста ставки в зависимости от текущей силы руки.
    /// </summary>
    private float GetEconomicThresholdForTier(CombinationDifficulty? difficulty)
    {
        if (!difficulty.HasValue)
        {
            return BET_GROWTH_THRESHOLD_NOTHING;
        }

        return difficulty.Value switch
        {
            CombinationDifficulty.Hard => BET_GROWTH_THRESHOLD_HARD,
            CombinationDifficulty.Medium => BET_GROWTH_THRESHOLD_MEDIUM,
            CombinationDifficulty.Easy => BET_GROWTH_THRESHOLD_EASY,
            _ => BET_GROWTH_THRESHOLD_NOTHING
        };
    }

    /// <summary>
    /// Возвращает порог допустимого риска Антикомбинации в зависимости от текущей силы руки.
    /// </summary>
    private float GetRiskThresholdForTier(CombinationDifficulty? difficulty)
    {
        if (!difficulty.HasValue)
        {
            return RISK_THRESHOLD_NOTHING_MET;
        }

        return difficulty.Value switch
        {
            CombinationDifficulty.Hard => RISK_THRESHOLD_HARD_MET,
            CombinationDifficulty.Medium => RISK_THRESHOLD_MEDIUM_MET,
            CombinationDifficulty.Easy => RISK_THRESHOLD_EASY_MET,
            _ => RISK_THRESHOLD_NOTHING_MET
        };
    }

    /// <summary>
    /// Функция проверки необходимости сбросить карты (Фолд).
    /// Защищает ИИ от разрушительного риска Антикомбинации и неадекватных экономических ставок врага.
    /// </summary>
    private bool ShouldFold(GameStateSnapshot snapshot)
    {
        bool isFacingBet = snapshot.CurrentParticipationPrice > snapshot.OwnCommittedValue;
        if (!isFacingBet) return false;

        CombinationDifficulty? realDifficulty = EvaluateHand(snapshot.OwnHand, snapshot.TableCards, snapshot.RoundCombinations);

        // --- КРИТЕРИЙ 1: Экономическая проверка на разумность чужей ставки ---
        int amountToCall = snapshot.CurrentParticipationPrice - snapshot.OwnCommittedValue;
        int potBeforeEnemyBet = Mathf.Max(1, snapshot.PotSize - amountToCall);
        float betGrowthPercentage = (float)amountToCall / potBeforeEnemyBet;

        float economicThreshold = GetEconomicThresholdForTier(realDifficulty);

        if (betGrowthPercentage > economicThreshold)
        {
            return true;
        }

        // --- КРИТЕРИЙ 2: Математическая проверка на риск разрушения руки ---
        float antiRisk = CalculateAntiRisk(snapshot.OwnHand, snapshot.TableCards, snapshot);
        float riskThreshold = GetRiskThresholdForTier(realDifficulty);

        if (antiRisk > riskThreshold)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Изолированная функция для расчета числового размера повышения ставки (Рейза).
    /// </summary>
    private int CalculateRaiseAmount(GameStateSnapshot snapshot, CombinationDifficulty? difficulty)
    {
        // Внутри вы можете проверять: if (difficulty == CombinationDifficulty.Hard) { return ... }
        // Если рейз не нужен, метод должен возвращать 0.
        return 0;
    }

    /// <summary>
    /// Изолированная функция для выбора конкретных частей тела костей скелета, которые ставятся на кон.
    /// </summary>
    private List<BodyPart> SelectBodyPartsForStake(GameStateSnapshot snapshot, CombinationDifficulty? difficulty, int raiseAmount)
    {
        // Теперь здесь доступен raiseAmount для более точной балансировки жертвенных костей
        return new List<BodyPart>();
    }
}

