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
    protected override int CalculateRaisePrice(float risk, List<StakeAsset> availableAssets)
    {
        if (availableAssets == null) return 0;

        if (risk < shouldRaiseThreshold) return 0;

        List<StakeAsset> allowedParts = availableAssets.Where(asset => asset != null && asset.bodyPart.Type != BodyPartType.Soul).ToList();

        int totalValueWithoutSoul = allowedParts
            .Sum(part => part.stakeValue);

        int calculatedPrice = Mathf.RoundToInt(totalValueWithoutSoul * (risk- shouldRaiseThreshold));

        return Mathf.Max(0, calculatedPrice);
    }

    public override List<StakeAsset> SelectBodypartsForTradeAction(float risk, List<StakeAsset> availableAssets, int price = 0)
    {
        if (risk < shouldFoldThreshold || availableAssets == null) return null;

        int requiredPrice = price == 0 ? CalculateRaisePrice(risk, availableAssets) : price;

        if (requiredPrice == 0) return null;

        List<StakeAsset> partsToStake = new List<StakeAsset>();

        int currentAccumulatedPrice = 0;

        foreach (var part in availableAssets)
        {
            if (currentAccumulatedPrice >= requiredPrice) break;

            partsToStake.Add(part);
            currentAccumulatedPrice += part.stakeValue;
        }

        return partsToStake.Count > 0 ? partsToStake : null;
    }
}
