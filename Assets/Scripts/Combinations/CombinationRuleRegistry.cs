using System.Collections.Generic;
using System.Linq;
using Combinations.Rules;

namespace Combinations
{

    /// <summary>
    /// Статический реестр всех комбинаций, разделенных по уровням сложности
    /// </summary>
    public static class CombinationRuleRegistry
    {
        public static readonly IReadOnlyList<CombinationRule> EasyRules;

        public static readonly IReadOnlyList<CombinationRule> MediumRules;

        public static readonly IReadOnlyList<CombinationRule> HardRules;

        public static readonly IReadOnlyList<CombinationRule> AntiRules;

        public static readonly IReadOnlyList<CombinationRule> AllRules;

        static CombinationRuleRegistry()
        {
            // === ЛЕГКИЙ УРОВЕНЬ ===
            var easy = new List<CombinationRule>
            {
                // Последовательности
                new ThreeInARow(),      // Три Подряд
                new FourInARow(),       // Четыре Подряд
                new FiveInARow(),       // Пять Подряд

                // Мастные последовательности
                new SuitedLadder3(),    // Мастная Лестница-3
                new SuitedLadder4(),    // Мастная Лестница-4
                new StraightFlush(),    // Стрит-Флеш

                // Простые группы рангов
                new ThreeOfAKind(),     // Тройка
                new TwoPair(),          // Две Пары
                new FullHouse()         // Фулл
            };
            EasyRules = easy.AsReadOnly();

            // === СРЕДНИЙ УРОВЕНЬ ===
            var medium = new List<CombinationRule>
            {
                // Ступенчатые пары
                new DoubleStep(),       // Двойная Ступень
                new TripleStep(),       // Тройная Ступень

                // Сложные формы с последовательностями
                new DrawBridge(),       // Разводной Мост
                new BrokenSeal(),       // Сломанная Печать
                new SkewTower(),        // Косая Башня
                new DoubleFork(),       // Двойная Вилка

                // Цветовые комбинации
                new RedBlackCastle(),   // Красно-Черный Замок
                new TwoMasks()          // Две Маски
            };
            MediumRules = medium.AsReadOnly();

            // === ТЯЖЕЛЫЙ УРОВЕНЬ ===
            var hard = new List<CombinationRule>
            {
                new Hinge(),            // Шарнир
                new Pincers(),          // Клещи
                new TwoPairSplit(),     // Две Пары С Расколом
                new SuitCross(),        // Перекрест Мастей
                new Reflection()        // Отражение
            };
            HardRules = hard.AsReadOnly();

            // === АНТИ-КОМБИНАЦИИ ===
            AntiRules = MediumRules;

            // === ВСЕ КОМБИНАЦИИ ===
            var all = new List<CombinationRule>();
            all.AddRange(EasyRules);
            all.AddRange(MediumRules);
            all.AddRange(HardRules);
            AllRules = all.AsReadOnly();
        }

        /// <summary>
        /// Получить правила по уровню сложности
        /// </summary>
        public static IReadOnlyList<CombinationRule> GetRules(CombinationDifficulty difficulty)
        {
            return difficulty switch
            {
                CombinationDifficulty.Easy => EasyRules,
                CombinationDifficulty.Medium => MediumRules,
                CombinationDifficulty.Hard => HardRules,
                CombinationDifficulty.Anti => AntiRules,
                _ => AllRules
            };
        }
    }
}
