using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Сломанная Печать: пара одного ранга и 3 карты подряд сразу после нее
    /// Форма: r, r, r+1, r+2, r+3
    /// Схема: 2+3 (пара r-r - источник 1; продолжение r+1, r+2, r+3 - источник 2)
    /// </summary>
    public class BrokenSeal : TemplateBasedRule
    {
        public override string Name => "Сломанная Печать";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2, IsPair),
                new PartitionTemplate.BlockRequirement(3, IsSequence)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 5) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();

            // Проверяем паттерн: r, r, r+1, r+2, r+3
            if (ranks[0] != ranks[1]) return false; // Первые две должны быть парой
            if (ranks[1] + 1 != ranks[2]) return false; // Следующая должна быть r+1
            if (ranks[2] + 1 != ranks[3]) return false; // r+2
            if (ranks[3] + 1 != ranks[4]) return false; // r+3

            return true;
        }

        private bool IsPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            return cards[0].Value == cards[1].Value;
        }

        private bool IsSequence(List<CardData> cards)
        {
            if (cards.Count != 3) return false;
            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();
            return ranks[1] - ranks[0] == 1 && ranks[2] - ranks[1] == 1;
        }
    }
}
