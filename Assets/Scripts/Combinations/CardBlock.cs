using System.Collections.Generic;

namespace Combinations
{
    /// <summary>
    /// Блок карт из одного источника (пула)
    /// </summary>
    public class CardBlock
    {
        public List<CardWithPool> Cards { get; }
        public CardPool Pool { get; }

        public CardBlock(List<CardWithPool> cards, CardPool pool)
        {
            Cards = cards;
            Pool = pool;
        }

        public int Size => Cards.Count;
    }

    public delegate bool BlockPredicate(List<CardData> cards);
}