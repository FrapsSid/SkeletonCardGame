public abstract class BaseAIDecisionStrategy
{
    /// <summary>
    /// Активная фаза (Торговля): Вызывается, когда нужно ответить на ставку или сделать свою.
    /// Допустимые действия: Fold, CheckCall (Колл/Чек), Raise.
    /// </summary>
    public abstract AIResponsePackage ChooseBettingAction(GameStateSnapshot snapshot);

    /// <summary>
    /// Пассивная фаза (Ход): Вызывается, когда игрок взаимодействует с картами.
    /// Допустимые действия: CheckCall (Пропустить), DrawCard, ChangeCombination.
    /// </summary>
    public abstract AIResponsePackage ChooseCardAction(GameStateSnapshot snapshot);
}
