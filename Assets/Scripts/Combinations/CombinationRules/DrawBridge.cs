using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Разводной Мост: две мини-лестницы по 3 карты с пропущенным рангом между ними
    /// Пример: 4-5-6 и 8-9-10 (пропущена 7)
    /// Схема: 3+3
    /// </summary>
    public class DrawBridge : TemplateBasedRule
    {
        public override string Name => "Draw Bridge";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(3, IsLowerSequence),
                new PartitionTemplate.BlockRequirement(3, IsUpperSequence)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            return FormChecker.IsBridgeForm(cards);
        }

        private bool IsLowerSequence(List<CardData> cards)
        {
            if (cards.Count != 3) return false;
            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();
            return ranks[1] - ranks[0] == 1 && ranks[2] - ranks[1] == 1;
        }

        private bool IsUpperSequence(List<CardData> cards)
        {
            if (cards.Count != 3) return false;
            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();
            return ranks[1] - ranks[0] == 1 && ranks[2] - ranks[1] == 1;
        }
    }
}
