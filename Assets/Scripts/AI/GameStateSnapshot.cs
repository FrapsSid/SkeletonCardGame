using System.Collections.Generic;

public struct GameStateSnapshot
{
    public int CurrentIteration;                     // Текущий ход
    public RoundCombinationSet RoundCombinations;    // Доступные комбинации раунда
    public List<CardData> TableCards;                // Открытые карты на столе

    public AIDeck _AIDeck;

    // Текущий игрок, принимающий решение (Own)
    public SkeletonBody OwnBody;                     // Ссылка на компонент скелета игрока
    public List<CardData> OwnHand;
    public DeclaredCombinationTier? OwnTarget;
    public int OwnCommittedValue;                    // Сколько вложил в банк лично

    // Тиммейт текущего игрока
    public List<CardData> AllyHand;
    public DeclaredCombinationTier? AllyTarget;

    // Противники 
    public List<CardData> Enemy1Hand;
    public DeclaredCombinationTier? Enemy1Target;
    public List<CardData> Enemy2Hand;
    public DeclaredCombinationTier? Enemy2Target;

    // Глобальная экономика стола
    public int CurrentParticipationPrice;            // Текущая ставка стола, которую надо закрыть
    public int PotSize;                              // Размер текущего банка
}