public abstract class BaseAIDecisionStrategy
{
    protected BaseStakeCalculator stakeCalculator;

    /// Торговля: Вызывается, когда нужно ответить на ставку или сделать свою.
    /// Допустимые действия: Fold, CheckCall (Колл/Чек), Raise.
    public abstract AIResponsePackage ChooseBettingAction(GameStateSnapshot snapshot);

    /// Ход: Вызывается, когда игрок взаимодействует с картами.
    /// Допустимые действия: Pass, DrawCard, ChangeCombination.
    public abstract AIResponsePackage ChooseCardAction(GameStateSnapshot snapshot);
}