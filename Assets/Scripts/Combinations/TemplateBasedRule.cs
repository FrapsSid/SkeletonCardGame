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

        protected TemplateBasedRule()
        {
            // Вычисляем требуемое количество карт из первого шаблона
            // (все шаблоны должны иметь одинаковое общее количество карт)
            if (Templates != null && Templates.Length > 0)
            {
                _requiredCardCount = Templates[0].Blocks.Sum(b => b.Size);
            }
        }

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
