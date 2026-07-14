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
                new ThreeInARow(),      // Три Подряд
                new TwoPair(),          // Две Пары
                new TwoPairSplit(),     // Две Пары С Расколом
                new FourInARow(),       // Четыре Подряд
                new Pincers(),          // Клещи
                new ThreeOfAKind()      // Тройка
            };
            EasyRules = easy.AsReadOnly();

            // === СРЕДНИЙ УРОВЕНЬ ===
            var medium = new List<CombinationRule>
            {
                new SuitedLadder3(),    // Мастная Лестница-3
                new SuitCross(),        // Перекрест Мастей
                new DoubleStep(),       // Двойная Ступень
                new FiveInARow(),       // Пять Подряд
                new FullHouse(),        // Фулл
                new BrokenSeal(),       // Сломанная Печать
                new DoubleFork(),       // Двойная Вилка
                new SkewTower(),        // Косая Башня
                new DrawBridge(),       // Разводной Мост
                new SuitedLadder4()     // Мастная Лестница-4
            };
            MediumRules = medium.AsReadOnly();

            // === ТЯЖЕЛЫЙ УРОВЕНЬ ===
            var hard = new List<CombinationRule>
            {
                new Hinge(),            // Шарнир
                new TwoMasks(),         // Две Маски
                new RedBlackCastle(),   // Красно-Черный Замок
                new TripleStep(),       // Тройная Ступень
                new Reflection(),       // Отражение
                new StraightFlush()     // Стрит-Флеш
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
