using System.Collections.Generic;

namespace Combinations.Rules
{
    /// <summary>
    /// Стрит-Флеш: 5 карт подряд одной масти
    /// Схема: 3+2
    /// </summary>
    public class StraightFlush : TemplateBasedRule
    {
        public override string Name => "Стрит-Флеш";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(3),
                new PartitionTemplate.BlockRequirement(2)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.IsSuitedSequence(cards, 5);
        }
    }
}
