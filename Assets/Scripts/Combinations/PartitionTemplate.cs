using System.Collections.Generic;
using System.Linq;

namespace Combinations
{
    /// <summary>
    /// Шаблон разбиения карт на блоки с условиями на каждый блок
    /// </summary>
    public class PartitionTemplate
    {
        /// <summary>
        /// Описание одного блока в шаблоне
        /// </summary>
        public class BlockRequirement
        {
            public int Size { get; }
            public BlockPredicate Predicate { get; }

            public BlockRequirement(int size, BlockPredicate predicate = null)
            {
                Size = size;
                Predicate = predicate ?? (_ => true); // по умолчанию - любые карты
            }
        }

        public List<BlockRequirement> Blocks { get; }

        public PartitionTemplate(params BlockRequirement[] blocks)
        {
            Blocks = new List<BlockRequirement>(blocks);
        }

        /// <summary>
        /// Пытается разбить карты на блоки согласно шаблону
        /// </summary>
        public bool TryMatch(List<CardWithPool> cards, out List<CardBlock> resultBlocks)
        {
            resultBlocks = new List<CardBlock>();

            // Группируем карты по пулам
            var cardsByPool = new Dictionary<CardPool, List<CardWithPool>>();
            foreach (var card in cards)
            {
                if (!cardsByPool.ContainsKey(card.Pool))
                    cardsByPool[card.Pool] = new List<CardWithPool>();
                cardsByPool[card.Pool].Add(card);
            }

            // Пытаемся найти разбиение на блоки
            return TryPartitionRecursive(cards, cardsByPool, 0, new List<CardBlock>(), resultBlocks);
        }

        private bool TryPartitionRecursive(
            List<CardWithPool> remainingCards,
            Dictionary<CardPool, List<CardWithPool>> cardsByPool,
            int blockIndex,
            List<CardBlock> currentBlocks,
            List<CardBlock> resultBlocks)
        {
            // Если все блоки найдены
            if (blockIndex >= Blocks.Count)
            {
                if (remainingCards.Count == 0)
                {
                    resultBlocks.Clear();
                    resultBlocks.AddRange(currentBlocks);
                    return true;
                }
                return false;
            }

            var requirement = Blocks[blockIndex];

            // Пытаемся собрать блок из каждого пула
            foreach (var pool in cardsByPool.Keys)
            {
                var poolCards = cardsByPool[pool];

                // Пытаемся выбрать нужное количество карт из этого пула
                if (TrySelectCardsFromPool(poolCards, remainingCards, requirement, out var selectedCards))
                {
                    // Создаем блок
                    var block = new CardBlock(selectedCards, pool);
                    currentBlocks.Add(block);

                    // Убираем выбранные карты из оставшихся
                    var newRemaining = new List<CardWithPool>(remainingCards);
                    foreach (var card in selectedCards)
                        newRemaining.Remove(card);

                    // Рекурсивно ищем следующий блок
                    if (TryPartitionRecursive(newRemaining, cardsByPool, blockIndex + 1, currentBlocks, resultBlocks))
                        return true;

                    // Откатываем
                    currentBlocks.RemoveAt(currentBlocks.Count - 1);
                }
            }

            return false;
        }

        private bool TrySelectCardsFromPool(
            List<CardWithPool> poolCards,
            List<CardWithPool> availableCards,
            BlockRequirement requirement,
            out List<CardWithPool> selectedCards)
        {
            selectedCards = new List<CardWithPool>();

            // Находим карты из этого пула среди доступных
            var candidates = new List<CardWithPool>();
            foreach (var card in poolCards)
            {
                if (availableCards.Contains(card))
                    candidates.Add(card);
            }

            if (candidates.Count < requirement.Size)
                return false;

            // Перебираем комбинации
            return TrySelectCombination(candidates, requirement.Size, requirement.Predicate, selectedCards);
        }

        private bool TrySelectCombination(
            List<CardWithPool> candidates,
            int size,
            BlockPredicate predicate,
            List<CardWithPool> result)
        {
            return TrySelectCombinationRecursive(candidates, size, predicate, 0, new List<CardWithPool>(), result);
        }

        private bool TrySelectCombinationRecursive(
            List<CardWithPool> candidates,
            int size,
            BlockPredicate predicate,
            int startIndex,
            List<CardWithPool> current,
            List<CardWithPool> result)
        {
            if (current.Count == size)
            {
                // Конвертируем CardWithPool в CardData для предиката
                var cardDataList = current.Select(c => c.Card).ToList();
                if (predicate(cardDataList))
                {
                    result.Clear();
                    result.AddRange(current);
                    return true;
                }
                return false;
            }

            for (int i = startIndex; i < candidates.Count; i++)
            {
                current.Add(candidates[i]);
                if (TrySelectCombinationRecursive(candidates, size, predicate, i + 1, current, result))
                    return true;
                current.RemoveAt(current.Count - 1);
            }

            return false;
        }
    }
}
