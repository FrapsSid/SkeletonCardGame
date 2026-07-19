using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Двойная Вилка: две пары вокруг одиночной средней карты
    /// Форма: r, r, r+1, r+2, r+2
    /// Схема: 2+3 (нижняя пара r-r - источник 1; средняя карта и верхняя пара r+1, r+2, r+2 - источник 2)
    /// </summary>
    public class DoubleFork : TemplateBasedRule
    {
        public override string Name => "Double Fork";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2, IsLowerPair),
                new PartitionTemplate.BlockRequirement(3, IsSinglePlusUpperPair)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 5) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();

            // Проверяем паттерн: r, r, r+1, r+2, r+2
            if (ranks[0] != ranks[1]) return false; // Первая пара
            if (ranks[1] + 1 != ranks[2]) return false; // r+1 (одиночная)
            if (ranks[2] + 1 != ranks[3]) return false; // r+2
            if (ranks[3] != ranks[4]) return false; // Вторая пара

            return true;
        }

        private bool IsLowerPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            return cards[0].Value == cards[1].Value;
        }

        private bool IsSinglePlusUpperPair(List<CardData> cards)
        {
            if (cards.Count != 3) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();
            
            // Должно быть: r+1, r+2, r+2 (одиночка + пара)
            return ranks[0] + 1 == ranks[1] && ranks[1] == ranks[2];
        }
    }
}
