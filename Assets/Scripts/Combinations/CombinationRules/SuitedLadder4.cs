using System.Collections.Generic;

namespace Combinations.Rules
{
    /// <summary>
    /// Мастная Лестница-4: 4 карты подряд одной масти
    /// Схема: 2+2
    /// </summary>
    public class SuitedLadder4 : TemplateBasedRule
    {
        public override string Name => "Мастная Лестница-4";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2),
                new PartitionTemplate.BlockRequirement(2)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.IsSuitedSequence(cards, 4);
        }
    }
}
