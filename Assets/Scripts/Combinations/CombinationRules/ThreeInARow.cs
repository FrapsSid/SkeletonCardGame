using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Три Подряд: 3 карты последовательных рангов
    /// Схема: 2+1
    /// </summary>
    public class ThreeInARow : TemplateBasedRule
    {
        public override string Name => "Three in a row";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2, IsPartOfSequence),
                new PartitionTemplate.BlockRequirement(1, IsPartOfSequence)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.IsSequence(cards, 3);
        }

        private bool IsPartOfSequence(List<CardData> cards)
        {
            // Любые карты подходят, форма уже проверена
            return true;
        }
    }
}
