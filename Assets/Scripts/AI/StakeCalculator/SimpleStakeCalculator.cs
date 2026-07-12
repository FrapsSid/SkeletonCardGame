using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SimpleStakeCalculator : BaseStakeCalculator
{
    [Header("Risk Thresholds")]
    [Tooltip("Если риск ниже этого значения, бот сбросит карты")]
    public float shouldFoldThreshold = 0.2f;

    [Tooltip("Если риск ниже этого значения, бот не будет повышать")]
    public float shouldRaiseThreshold = 0.4f;
    protected override int CalculateTradeActionPrice(float risk, SkeletonBody skeletonBody)
    {
        if (skeletonBody == null) return 0;

        if (risk < shouldRaiseThreshold) return 0;

        List<BodyPart> allowedParts = SelectOrderedBodypartsByPrice(0, skeletonBody);

        int totalValueWithoutSoul = allowedParts
            .Where(part => part.Type != BodyPartType.Soul)
            .Sum(part => BodyPartExtensions.GetBodyPartCost(part));

        int calculatedPrice = Mathf.RoundToInt(totalValueWithoutSoul * (risk- shouldRaiseThreshold));

        return Mathf.Max(0, calculatedPrice);
    }

    public override List<BodyPart> SelectBodypartsForTradeAction(float risk, SkeletonBody skeletonBody, int price = 0)
    {
        if (risk < shouldFoldThreshold || skeletonBody == null) return null;

        int requiredPrice = price == 0 ? CalculateTradeActionPrice(risk, skeletonBody) : price;

        if (requiredPrice == 0) return null;

        List<BodyPart> availableParts = SelectOrderedBodypartsByPrice(requiredPrice, skeletonBody);

        List<BodyPart> partsToStake = new List<BodyPart>();
        int currentAccumulatedPrice = 0;

        foreach (var part in availableParts)
        {
            if (currentAccumulatedPrice >= requiredPrice) break;

            partsToStake.Add(part);
            currentAccumulatedPrice += BodyPartExtensions.GetBodyPartCost(part);
        }

        return partsToStake.Count > 0 ? partsToStake : null;
    }

    protected override List<BodyPart> SelectOrderedBodypartsByPrice(float price, SkeletonBody skeletonBody)
    {
        if (skeletonBody == null) return new List<BodyPart>();

        return skeletonBody.GetAttachedParts()
            .OrderBy(part => BodyPartExtensions.GetBodyPartCost(part))
            .ToList();
    }
}
