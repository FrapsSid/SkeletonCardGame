using System.Collections.Generic;

namespace Combinations
{
    /// <summary>
    /// Базовый класс для правила комбинации
    /// </summary>
    public abstract class CombinationRule
    {
        public abstract string Name { get; }

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
