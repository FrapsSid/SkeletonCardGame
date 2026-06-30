using System.Collections.Generic;

namespace Combinations.Rules
{
    /// <summary>
    /// Мастная Лестница-3: 3 карты подряд одной масти
    /// Схема: 2+1
    /// </summary>
    public class SuitedLadder3 : TemplateBasedRule
    {
        public override string Name => "Мастная Лестница-3";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2),
                new PartitionTemplate.BlockRequirement(1)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.IsSuitedSequence(cards, 3);
        }
    }
}
