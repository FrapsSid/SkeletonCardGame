using System.Collections.Generic;
using System.Linq;

namespace Combinations
{
    /// <summary>
    /// Базовый класс для комбинаций, основанных на шаблонах разбиения
    /// </summary>
    public abstract class TemplateBasedRule : CombinationRule
    {
        protected abstract PartitionTemplate[] Templates { get; }

        public override bool Check(List<CardWithPool> cards)
        {
            // Извлекаем CardData для проверки формы
            var cardDataList = cards.Select(c => c.Card).ToList();

            // Сначала проверяем форму
            if (!CheckForm(cardDataList))
                return false;

            // Затем пытаемся найти подходящее разбиение
            foreach (var template in Templates)
            {
                if (template.TryMatch(cards, out _))
                    return true;
            }

            return false;
        }
    }
}
