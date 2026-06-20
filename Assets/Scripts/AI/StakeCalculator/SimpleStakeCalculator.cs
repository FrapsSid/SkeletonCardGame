using System.Collections.Generic;
using UnityEngine;

public class SimpleStakeCalculator : BaseStakeCalculator
{
    protected override int CalculateTradeActionPrice(float risk, SkeletonBody skeletonBody)
    {
        throw new System.NotImplementedException();
    }

    public override List<BodyPart> SelectBodypartsForTradeAction(float risk, SkeletonBody skeletonBody, int price = 0)
    {
        throw new System.NotImplementedException();
    }

    protected override List<BodyPart> SelectOrderedBodypartsByPrice(float price, SkeletonBody skeletonBody)
    {
        throw new System.NotImplementedException();
    }
}