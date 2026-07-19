using System.Collections.Generic;

namespace Combinations.Rules
{
    /// <summary>
    /// Пять Подряд: 5 карт последовательных рангов
    /// Схема: 3+2 ИЛИ 2+1+2
    /// </summary>
    public class FiveInARow : TemplateBasedRule
    {
        public override string Name => "Five in a row";

        protected override PartitionTemplate[] Templates => new[]
        {
            // Вариант 1: 3+2
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(3),
                new PartitionTemplate.BlockRequirement(2)
            ),
            // Вариант 2: 2+1+2
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2),
                new PartitionTemplate.BlockRequirement(1),
                new PartitionTemplate.BlockRequirement(2)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.IsSequence(cards, 5);
        }
    }
}
