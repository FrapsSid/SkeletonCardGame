using System.Collections.Generic;

namespace Combinations
{
    public static class CardPoolBuilder
    {
        public static List<CardWithPool> BuildCardPool(List<CardData> tableCards, List<CardData> player1Hand, List<CardData> player2Hand = null)
        {
            var pool = new List<CardWithPool>();

            if (player1Hand != null)
            {
                foreach (var cardData in player1Hand)
                {
                    pool.Add(new CardWithPool(cardData, CardPool.Player1Hand));
                }
            }

            if (player2Hand != null)
            {
                foreach (var cardData in player2Hand)
                {
                    pool.Add(new CardWithPool(cardData, CardPool.Player2Hand));
                }
            }

            if (tableCards != null)
            {
                foreach (var cardData in tableCards)
                {
                    pool.Add(new CardWithPool(cardData, CardPool.Table));
                }
            }

            return pool;
        }
    }
}

