using System.Collections.Generic;

namespace Combinations
{
    /// <summary>
    /// Базовый класс для правила комбинации
    /// </summary>
    public abstract class CombinationRule
    {
        protected int _requiredCardCount;

        public abstract string Name { get; }

        /// <summary>
        /// Количество карт, необходимое для этой комбинации
        /// </summary>
        public int RequiredCardCount => _requiredCardCount;

        /// <summary>
        /// Проверяет, образуют ли карты данную комбинацию
        /// </summary>
        public abstract bool Check(List<CardWithPool> cards);

        /// <summary>
        /// Проверяет форму комбинации (без учета источников)
        /// </summary>
        protected abstract bool CheckForm(List<CardData> cards);
    }
}
