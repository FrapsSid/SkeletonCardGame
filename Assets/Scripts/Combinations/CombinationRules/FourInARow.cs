using System.Collections.Generic;

namespace Combinations.Rules
{
    /// <summary>
    /// Четыре Подряд: 4 карты последовательных рангов
    /// Схема: 2+2
    /// </summary>
    public class FourInARow : TemplateBasedRule
    {
        public override string Name => "Four in a row";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2),
                new PartitionTemplate.BlockRequirement(2)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.IsSequence(cards, 4);
        }
    }
}
