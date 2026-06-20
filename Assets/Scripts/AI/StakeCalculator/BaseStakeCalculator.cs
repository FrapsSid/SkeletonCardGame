using UnityEngine;
using System.Collections.Generic;

public abstract class BaseStakeCalculator
{
    public abstract List<BodyPart> SelectBodypartsForTradeAction(float risk, SkeletonBody skeletonBody, int price = 0);

    protected abstract List<BodyPart> SelectOrderedBodypartsByPrice(float price, SkeletonBody skeletonBody);
    protected abstract int CalculateTradeActionPrice(float risk, SkeletonBody skeletonBody);
}