using System.Collections.Generic;

namespace Combinations.Rules
{
    /// <summary>
    /// Тройка: 3 карты одного ранга
    /// Схема: 2+1
    /// </summary>
    public class ThreeOfAKind : TemplateBasedRule
    {
        public override string Name => "Тройка";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2),
                new PartitionTemplate.BlockRequirement(1)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.HasRankGroups(cards, new[] { 3 });
        }
    }
}
