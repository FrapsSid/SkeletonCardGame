using UnityEngine;
using System.Collections.Generic;

public abstract class BaseStakeCalculator
{
    public abstract List<StakeAsset> SelectBodypartsForTradeAction(float risk, List<StakeAsset> availableAssets, int price = 0);
    protected abstract int CalculateRaisePrice(float risk, List<StakeAsset> availableAssets);

    protected abstract List<StakeAsset> OrderBodyparts(List<StakeAsset> availableAssets);
}