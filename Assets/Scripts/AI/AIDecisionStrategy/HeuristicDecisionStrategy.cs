using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeuristicDecisionStrategy : BaseAIDecisionStrategy
{
    // --- Пороги риска для защиты от Антикомбинации ---
    private const float ANTIRISK_THRESHOLD_EASY_MET = 0.25f;
    private const float ANTIRISK_THRESHOLD_MEDIUM_MET = 0.15f;
    private const float ANTIRISK_THRESHOLD_HARD_MET = 0.00f;
    private const float ANTIRISK_THRESHOLD_NOTHING_MET = 0.5f;

    private const int RISK_SAMPLE_SIZE = 10;                // Размер выборки карт для симуляции на проверку Антикомбинации

    // --- Значения допустимого риска ИИ в торговой фазе взависимости от собранной комбинации ---
    private const float RISK_EASY_MET = 0.75f;
    private const float RISK_MEDIUM_MET = 0.95f;
    private const float RISK_HARD_MET = 1.25f;
    private const float RISK_NOTHING_MET = 0.45f;

    // --- ФАЗА ТОРГОВЛИ ---
    public override AIResponsePackage ChooseBettingAction(GameStateSnapshot snapshot)
    {
        if (snapshot.OwnBody != null && snapshot.OwnBody.IsIncapacitated)
        {
            return new AIResponsePackage(AIActionType.Fold);
        }

        // подсчет собранной комбинации
        CombinationDifficulty? realDifficulty = EvaluateHand(snapshot.OwnHand, snapshot.TableCards, snapshot.RoundCombinations);

        // подсчет допустимого риска
        float risk = CalculateRiskForTradeAction(snapshot, realDifficulty);

        // выбор между колом и фолдом
        if (snapshot.CurrentParticipationPrice > 0)
        {
            List<BodyPart> partsToCall = stakeCalculator.SelectBodypartsForTradeAction(risk, snapshot.OwnBody, snapshot.CurrentParticipationPrice);

            if (partsToCall != null)
            {
                int totalCost = partsToCall.Sum(part => BodyPartExtensions.GetBodyPartCost(part));

                return new AIResponsePackage(AIActionType.CheckCall, totalCost, partsToCall, null);
            }
            else
            {
                return new AIResponsePackage(AIActionType.Fold);
            }
        }
        else
        // выбор между чеком и рейзом
        {
            List<BodyPart> partsToRaise = stakeCalculator.SelectBodypartsForTradeAction(risk, snapshot.OwnBody);

            if (partsToRaise != null)
            {
                int totalCost = partsToRaise.Sum(part => BodyPartExtensions.GetBodyPartCost(part));
                return new AIResponsePackage(AIActionType.Raise, totalCost, partsToRaise, null);
            }
            else
            {
                return new AIResponsePackage(AIActionType.CheckCall);
            }
        }
    }

    // --- ХОД С КАРТАМИ ---
    public override AIResponsePackage ChooseCardAction(GameStateSnapshot snapshot)
    {
        if (snapshot.OwnBody != null && snapshot.OwnBody.IsIncapacitated)
        {
            return new AIResponsePackage(AIActionType.Pass);
        }

        // подсчет собранной комбинации
        CombinationDifficulty? realDifficulty = EvaluateHand(snapshot.OwnHand, snapshot.TableCards, snapshot.RoundCombinations);

        // если собрали антикомбинацию - фолд
        if (realDifficulty.HasValue && realDifficulty.Value == CombinationDifficulty.Anti)
        {
            return new AIResponsePackage(AIActionType.Fold);
        }

        // подсчет вероятности антикомбинации при взятии карты
        float antiRisk = CalculateAntiRisk(snapshot.OwnHand, snapshot.TableCards, snapshot);

        // --- УСЛОВИЕ 1: Заявленной комбинации нет ---
        if (!snapshot.OwnTarget.HasValue)
        {
            CombinationDifficulty difficultyToDeclare = realDifficulty ?? CombinationDifficulty.Easy;

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
        if (!realDifficulty.HasValue || (realDifficulty.Value <= DeclaredCombinationTierExtensions.ToCombinationDifficulty(currentTarget)))
        {
            // Получаем динамический порог риска для текущей руки
            float dynamicRiskThreshold = GetAntiRiskThresholdForTier(realDifficulty);

            if (antiRisk < dynamicRiskThreshold)
            {
                return new AIResponsePackage(AIActionType.DrawCard);
            }

            // Если риск превышен — пропускаем ход
            return new AIResponsePackage(AIActionType.Pass);
        }

        // --- УСЛОВИЕ 3: Реальная комбинация ВЫШЕ заявленной ---
        if (realDifficulty.HasValue && realDifficulty.Value > DeclaredCombinationTierExtensions.ToCombinationDifficulty(currentTarget))
        {
            DeclaredCombinationTier targetToDeclare = realDifficulty.Value switch
            {
                CombinationDifficulty.Easy => DeclaredCombinationTier.Easy,
                CombinationDifficulty.Medium => DeclaredCombinationTier.Medium,
                CombinationDifficulty.Hard => DeclaredCombinationTier.Hard,
                _ => DeclaredCombinationTier.Hard
            };

            // Мгновенно переключаем заявку сразу на уровень реально собранной
            return new AIResponsePackage(AIActionType.ChangeCombination, 0, null, targetToDeclare);
        }

        // Дефолтный защитный пропуск хода
        return new AIResponsePackage(AIActionType.CheckCall);

    }


    // --- МАТЕМАТИЧЕСКИЕ И ОЦЕНОЧНЫЕ МЕТОДЫ ---

    /// Оценивает силу карт конкретного игрока в связке со столом на основе комбинаций раунда.
    /// Проверяет совпадения поочередно, начиная со Сложной комбинации.
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

    /// Расчет риска получить Антикомбинацию.
    /// Выдергивает 10 случайных карт прямо из колоды снимка snapshot._AIDeck.
    private float CalculateAntiRisk(List<CardData> currentHand, List<CardData> tableCards, GameStateSnapshot snapshot)
    {
        if (snapshot._AIDeck == null || snapshot._AIDeck.IsEmpty) return 0.0f;

        // Находим размер выборки
        int sampleSize = Math.Min(RISK_SAMPLE_SIZE, snapshot._AIDeck.CardsCount);
        int antiOutsCount = 0;

        List<CardData> sampledCards = new List<CardData>(sampleSize);
        List<CardData> temporaryHand = new List<CardData>(currentHand.Count + 1);

        // ШАГ 1: Поочередно достаем случайные карты из колоды снимка
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

        // ШАГ 3: Возвращаем карты обратно в колоду
        ReturnCardsToDeck(snapshot._AIDeck, sampledCards);

        // Если из-за пустоты колоды ничего не вытащили, возвращаем 0
        if (sampledCards.Count == 0) return 0.0f;

        // Возвращаем финальный статистический процент риска
        return (float)antiOutsCount / sampledCards.Count;
    }

    /// Возвращает тестовые карты обратно в виртуальную колоду ИИ.
    private void ReturnCardsToDeck(AIDeck deck, List<CardData> cardsToReturn)
    {
        if (deck == null || cardsToReturn == null) return;

        foreach (var card in cardsToReturn)
        {
            deck.ReturnCard(card);
        }
    }

    private float CalculateRiskForTradeAction(GameStateSnapshot snapshot, CombinationDifficulty? difficulty)
    {
        float risk = GetRiskForTier(difficulty);
        risk -= snapshot.CurrentIteration;
        risk = Mathf.Clamp01(risk);

        return risk;
    }

    /// Возвращает порог допустимого риска Антикомбинации в зависимости от текущей силы руки.
    private float GetAntiRiskThresholdForTier(CombinationDifficulty? difficulty)
    {
        if (!difficulty.HasValue)
        {
            return ANTIRISK_THRESHOLD_NOTHING_MET;
        }

        return difficulty.Value switch
        {
            CombinationDifficulty.Hard => ANTIRISK_THRESHOLD_HARD_MET,
            CombinationDifficulty.Medium => ANTIRISK_THRESHOLD_MEDIUM_MET,
            CombinationDifficulty.Easy => ANTIRISK_THRESHOLD_EASY_MET,
            _ => ANTIRISK_THRESHOLD_NOTHING_MET
        };
    }

    /// Возвращает допустимый риск в торговой фазе в зависимости от текущей силы руки.
    private float GetRiskForTier(CombinationDifficulty? difficulty)
    {
        if (!difficulty.HasValue)
        {
            return RISK_NOTHING_MET;
        }

        return difficulty.Value switch
        {
            CombinationDifficulty.Hard => RISK_HARD_MET,
            CombinationDifficulty.Medium => RISK_MEDIUM_MET,
            CombinationDifficulty.Easy => RISK_EASY_MET,
            _ => RISK_NOTHING_MET
        };
    }
}
