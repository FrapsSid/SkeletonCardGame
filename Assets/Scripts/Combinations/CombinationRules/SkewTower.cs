using System.Collections.Generic;
using System.Linq;

namespace Combinations.Rules
{
    /// <summary>
    /// Косая Башня: 2 пары соседних рангов и одиночная карта следующего ранга
    /// Форма: r, r, r+1, r+1, r+2
    /// Схема: 2+3 (нижняя пара r-r - источник 1; верхняя пара и одиночка r+1, r+1, r+2 - источник 2)
    /// </summary>
    public class SkewTower : TemplateBasedRule
    {
        public override string Name => "Skew Tower";

        protected override PartitionTemplate[] Templates => new[]
        {
            new PartitionTemplate(
                new PartitionTemplate.BlockRequirement(2, IsLowerPair),
                new PartitionTemplate.BlockRequirement(3, IsUpperPairPlusSingle)
            )
        };

        protected override bool CheckForm(List<CardData> cards)
        {
            if (cards.Count != 5) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();

            // Проверяем паттерн: r, r, r+1, r+1, r+2
            if (ranks[0] != ranks[1]) return false; // Первая пара
            if (ranks[1] + 1 != ranks[2]) return false; // r+1
            if (ranks[2] != ranks[3]) return false; // Вторая пара
            if (ranks[3] + 1 != ranks[4]) return false; // r+2

            return true;
        }

        private bool IsLowerPair(List<CardData> cards)
        {
            if (cards.Count != 2) return false;
            return cards[0].Value == cards[1].Value;
        }

        private bool IsUpperPairPlusSingle(List<CardData> cards)
        {
            if (cards.Count != 3) return false;

            var ranks = cards.Select(c => (int)c.Value).OrderBy(r => r).ToList();
            
            // Должно быть: r+1, r+1, r+2
            return ranks[0] == ranks[1] && ranks[1] + 1 == ranks[2];
        }
    }
}
